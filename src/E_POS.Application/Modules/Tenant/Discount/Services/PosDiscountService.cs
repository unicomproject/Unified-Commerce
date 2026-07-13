using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;

namespace E_POS.Application.Modules.Tenant.Discount.Services;

public sealed class PosDiscountService : IPosDiscountService
{
    private readonly IPosDiscountRepository _repository;
    private readonly IPosCheckoutRepository _checkoutRepository;
    private readonly IDateTimeProvider _clock;

    public PosDiscountService(
        IPosDiscountRepository repository,
        IPosCheckoutRepository checkoutRepository,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _checkoutRepository = checkoutRepository;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosDiscountCatalogResponseDto>> ListAvailableAsync(
        TenantRequestContext context, PosDiscountCatalogQueryDto query, CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Discount.Apply))
            return Failure<PosDiscountCatalogResponseDto>("pos_discounts.permission_denied", "You do not have permission to view POS discounts.");
        if (query.DeviceId == Guid.Empty)
            return Failure<PosDiscountCatalogResponseDto>("pos_discounts.invalid_device_id", "Device id is required.");

        var result = await _repository.ListAvailableAsync(
            context.TenantId, context.UserId, query, _clock.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult<PosDiscountCatalogResponseDto>.Success(result.Catalog!)
            : Failure<PosDiscountCatalogResponseDto>(result.ErrorCode!, ContextErrorMessage(result.ErrorCode));
    }

    public async Task<ApplicationResult<PosDiscountValidationResponseDto>> ValidateAsync(
        TenantRequestContext context, PosDiscountValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Discount.Apply))
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.permission_denied", "You do not have permission to validate POS discounts.");

        var requestError = ValidateRequest(request, requireIdempotencyKey: false);
        if (requestError is not null)
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.invalid_request", requestError);

        return await ValidateCoreAsync(context, request, cancellationToken);
    }

    public async Task<ApplicationResult<PosDiscountApplyResponseDto>> ApplyAsync(
        TenantRequestContext context, PosDiscountValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Discount.Apply))
            return Failure<PosDiscountApplyResponseDto>("pos_discounts.permission_denied", "You do not have permission to apply POS discounts.");

        var requestError = ValidateRequest(request, requireIdempotencyKey: true);
        if (requestError is not null)
            return Failure<PosDiscountApplyResponseDto>("pos_discounts.invalid_request", requestError);

        var validationResult = await ValidateCoreAsync(context, request, cancellationToken);
        if (!validationResult.IsSuccess || validationResult.Value is null)
            return ApplicationResult<PosDiscountApplyResponseDto>.Failure(validationResult.Error);

        var validation = validationResult.Value;
        if (!validation.IsValid)
        {
            return ApplicationResult<PosDiscountApplyResponseDto>.Success(new(
                Guid.Empty, validation.DiscountId, false, "rejected", validation.Subtotal, 0,
                validation.Subtotal, false, _clock.UtcNow,
                validation.CartHash, validation.ValidationMessages));
        }

        var source = NormalizeSource(request.DiscountSource)!;
        var contextResult = source == "MANUAL"
            ? await _repository.ResolveManualContextAsync(
                context.TenantId, context.UserId, request.DeviceId,
                request.CalculationMethod!.Trim().ToUpperInvariant(),
                request.Scope!.Trim().ToUpperInvariant(), _clock.UtcNow, cancellationToken)
            : await _repository.ResolveContextAsync(
                context.TenantId, context.UserId, request.DeviceId, validation.DiscountId,
                new PosDiscountApplicabilityContext(
                    request.Scope!.Trim().ToUpperInvariant(), request.TargetVariantId,
                    VariantIdsForScope(request.Scope, request.TargetVariantId, request.Lines),
                    request.CustomerId, request.Lines.Sum(x => (decimal)x.Qty),
                    validation.Subtotal, validation.CurrencyCode),
                _clock.UtcNow, cancellationToken);
        if (!contextResult.IsSuccess)
            return Failure<PosDiscountApplyResponseDto>(contextResult.ErrorCode!, ContextErrorMessage(contextResult.ErrorCode));

        var policy = contextResult.Policy!;
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(15);
        var scope = NormalizeScope(request.Scope, policy.Scope)!;
        var snapshotJson = PosDiscountCartFingerprint.CreateSnapshotJson(
            request.DeviceId, request.SaleType, request.CustomerId, request.Lines,
            validation.Subtotal, validation.CurrencyCode);
        if (!string.Equals(PosDiscountCartFingerprint.Hash(snapshotJson), validation.CartHash, StringComparison.Ordinal))
            return Failure<PosDiscountApplyResponseDto>("pos_discounts.cart_changed", "The cart changed while the discount was being applied.");

        var create = await _repository.CreateApplicationAsync(new(
            Guid.NewGuid(), context.TenantId, policy.Id, policy.DiscountTypeId,
            contextResult.OutletId, contextResult.TillId, contextResult.TillSessionId,
            request.DeviceId, context.UserId, request.CustomerId, request.TargetVariantId,
            request.IdempotencyKey!.Trim(), source, scope, policy.Code, policy.Name,
            policy.CalculationMethod, validation.RequestedValue, validation.CashierLimit,
            validation.AbsoluteLimit, validation.Subtotal, validation.EligibleSubtotal,
            validation.DiscountAmount, validation.TotalAfterDiscount,
            validation.CurrencyCode, snapshotJson, validation.CartHash,
            request.Reason, validation.RequiresManagerApproval, policy.IsStackable,
            policy.StackingGroupCode, expiresAt, now), cancellationToken);

        if (!create.IsSuccess)
            return Failure<PosDiscountApplyResponseDto>(create.ErrorCode!, "The discount application conflicts with an existing request.");

        var pending = string.Equals(create.Status, "PENDING_APPROVAL", StringComparison.Ordinal);
        return ApplicationResult<PosDiscountApplyResponseDto>.Success(new(
            create.ApplicationId, policy.Id, !pending, create.Status.ToLowerInvariant(),
            validation.Subtotal, pending ? 0 : validation.DiscountAmount,
            pending ? validation.Subtotal : validation.TotalAfterDiscount,
            pending, create.ExpiresAt, validation.CartHash,
            pending ? ["Manager approval is required before checkout."] : []));
    }

    public async Task<ApplicationResult<PosDiscountDecisionResponseDto>> DecideAsync(
        TenantRequestContext context, Guid applicationId, PosDiscountDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Discount.Approve))
            return Failure<PosDiscountDecisionResponseDto>("pos_discounts.approval_permission_denied", "You do not have permission to approve POS discounts.");
        if (applicationId == Guid.Empty || request is null ||
            (request.Decision?.Trim().ToUpperInvariant() is not ("APPROVE" or "REJECT")))
            return Failure<PosDiscountDecisionResponseDto>("pos_discounts.invalid_decision", "Decision must be APPROVE or REJECT.");

        var result = await _repository.DecideAsync(
            context.TenantId, context.UserId, applicationId, request.Decision,
            request.Note, _clock.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult<PosDiscountDecisionResponseDto>.Success(result.Decision!)
            : Failure<PosDiscountDecisionResponseDto>(result.ErrorCode!, "The discount approval request could not be decided.");
    }

    public async Task<ApplicationResult<PosDiscountCancelResponseDto>> CancelAsync(
        TenantRequestContext context, Guid applicationId, PosDiscountCancelRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Discount.Apply))
            return Failure<PosDiscountCancelResponseDto>("pos_discounts.permission_denied",
                "You do not have permission to cancel POS discounts.");
        if (applicationId == Guid.Empty || request.DeviceId == Guid.Empty || request.Reason?.Length > 500)
            return Failure<PosDiscountCancelResponseDto>("pos_discounts.invalid_request",
                "Application id, device id, and a valid reason are required.");
        var result = await _repository.CancelAsync(
            context.TenantId, context.UserId, applicationId, request.DeviceId,
            request.Reason, _clock.UtcNow, cancellationToken);
        return result.IsSuccess
            ? ApplicationResult<PosDiscountCancelResponseDto>.Success(result.Cancellation!)
            : Failure<PosDiscountCancelResponseDto>(result.ErrorCode!,
                "Discount application could not be cancelled.");
    }

    private async Task<ApplicationResult<PosDiscountValidationResponseDto>> ValidateCoreAsync(
        TenantRequestContext context, PosDiscountValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var source = NormalizeSource(request.DiscountSource);
        var requestedScope = request.Scope?.Trim().ToUpperInvariant();
        var method = request.CalculationMethod?.Trim().ToUpperInvariant();
        if (source is null || requestedScope is not ("ORDER" or "LINE"))
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.invalid_request",
                "Discount source must be POLICY or MANUAL and scope must be ORDER or LINE.");
        if (source == "POLICY" && (!request.DiscountId.HasValue || request.DiscountId == Guid.Empty))
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.invalid_request", "Policy id is required.");
        if (source == "MANUAL" && method is not ("PERCENTAGE" or "FIXED_AMOUNT"))
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.invalid_request",
                "Manual discount calculation method must be PERCENTAGE or FIXED_AMOUNT.");
        var applicability = new PosDiscountApplicabilityContext(
            requestedScope, request.TargetVariantId,
            VariantIdsForScope(requestedScope, request.TargetVariantId, request.Lines),
            request.CustomerId,
            request.Lines.Sum(x => (decimal)x.Qty), 0m, null);
        var resolved = source == "POLICY"
            ? await _repository.ResolveContextAsync(
                context.TenantId, context.UserId, request.DeviceId, request.DiscountId!.Value,
                applicability, now, cancellationToken)
            : await _repository.ResolveManualContextAsync(
                context.TenantId, context.UserId, request.DeviceId, method!, requestedScope, now, cancellationToken);
        if (!resolved.IsSuccess)
            return Failure<PosDiscountValidationResponseDto>(resolved.ErrorCode!, ContextErrorMessage(resolved.ErrorCode));

        var policy = resolved.Policy!;
        var scope = source == "POLICY" ? policy.Scope : requestedScope;
        if (requestedScope != policy.Scope)
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.scope_mismatch",
                "Requested discount scope does not match the policy scope.");
        if (scope == "ORDER" && request.TargetVariantId.HasValue)
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.scope_mismatch",
                "Order discounts cannot target a cart line.");
        if (policy.CalculationMethod is not ("PERCENTAGE" or "FIXED_AMOUNT"))
            return Failure<PosDiscountValidationResponseDto>("pos_discounts.unsupported_method", "This discount calculation method is not supported by POS manual application.");

        var requestedValue = source == "POLICY" ? policy.PredefinedValue : request.RequestedValue ?? 0m;
        var cashierLimit = policy.CalculationMethod == "PERCENTAGE"
            ? resolved.Authority!.MaxPercentage
            : resolved.Authority!.MaxFixedAmount;
        var absoluteLimit = policy.AbsoluteValueLimit;

        var fullCart = await CalculateCartAsync(context, request, request.Lines, cancellationToken);
        if (!fullCart.IsSuccess || fullCart.Summary is null)
            return Failure<PosDiscountValidationResponseDto>(fullCart.ErrorCode ?? "pos_discounts.cart_invalid", "The cart could not be validated.");

        var eligibleLines = request.Lines;
        if (scope == "LINE")
        {
            if (!request.TargetVariantId.HasValue || request.TargetVariantId == Guid.Empty)
                return Failure<PosDiscountValidationResponseDto>("pos_discounts.target_required", "A target variant is required for a line discount.");
            eligibleLines = request.Lines.Where(x => x.VariantId == request.TargetVariantId.Value).ToList();
            if (eligibleLines.Count == 0)
                return Failure<PosDiscountValidationResponseDto>("pos_discounts.target_not_in_cart", "The target variant is not present in the cart.");
        }

        var eligibleCart = scope == "ORDER"
            ? fullCart
            : await CalculateCartAsync(context, request, eligibleLines, cancellationToken);
        if (!eligibleCart.IsSuccess || eligibleCart.Summary is null)
            return Failure<PosDiscountValidationResponseDto>(eligibleCart.ErrorCode ?? "pos_discounts.cart_invalid", "The eligible cart amount could not be calculated.");

        var subtotal = fullCart.Summary.BillingSummary.Subtotal;
        var eligibleSubtotal = eligibleCart.Summary.BillingSummary.Subtotal;
        var currency = fullCart.Summary.BillingSummary.Currency;
        var snapshotJson = PosDiscountCartFingerprint.CreateSnapshotJson(
            request.DeviceId, request.SaleType, request.CustomerId, request.Lines, subtotal, currency);
        var cartHash = PosDiscountCartFingerprint.Hash(snapshotJson);
        var messages = new List<string>();

        if (requestedValue <= 0) messages.Add("Requested discount must be greater than zero.");
        if (requestedValue > absoluteLimit) messages.Add("Requested discount exceeds the absolute policy maximum.");
        if (policy.CalculationMethod == "PERCENTAGE" && requestedValue > 100m) messages.Add("Percentage discount cannot exceed 100%.");
        if (policy.CalculationMethod == "FIXED_AMOUNT" && requestedValue > eligibleSubtotal)
            messages.Add("Fixed discount cannot exceed the eligible cart amount.");
        if (policy.MinOrderAmount.HasValue && subtotal < policy.MinOrderAmount.Value)
            messages.Add($"Minimum order amount is {policy.MinOrderAmount.Value:0.####}.");
        var quantity = request.Lines.Sum(x => x.Qty);
        if (policy.MinQuantity.HasValue && quantity < policy.MinQuantity.Value)
            messages.Add($"Minimum item quantity is {policy.MinQuantity.Value:0.####}.");

        var absoluteExceeded = requestedValue > absoluteLimit;
        var amount = policy.CalculationMethod == "PERCENTAGE"
            ? eligibleSubtotal * requestedValue / 100m
            : requestedValue;
        if (policy.MaxDiscountAmount.HasValue && amount > policy.MaxDiscountAmount.Value)
            amount = policy.MaxDiscountAmount.Value;
        amount = Math.Clamp(amount, 0m, eligibleSubtotal);
        var discountAmount = ToMoney(amount);
        var valid = messages.Count == 0 && discountAmount > 0;
        var approvalRequired = valid && !absoluteExceeded &&
            (requestedValue > cashierLimit || policy.RequiresManagerApproval);
        var outcome = !valid ? "rejected" : approvalRequired ? "approval_required" : "direct_apply";

        return ApplicationResult<PosDiscountValidationResponseDto>.Success(new(
            policy.Id, valid, outcome, policy.CalculationMethod, requestedValue,
            cashierLimit, absoluteLimit, subtotal, eligibleSubtotal,
            valid ? discountAmount : 0, subtotal - (valid ? discountAmount : 0),
            currency, approvalRequired, cartHash, messages));
    }

    private Task<PosCheckoutCalculationResult> CalculateCartAsync(
        TenantRequestContext context, PosDiscountValidationRequestDto request,
        IReadOnlyList<PosCheckoutLineRequestDto> lines, CancellationToken cancellationToken) =>
        _checkoutRepository.CalculateSummaryAsync(
            context.TenantId, context.UserId, context.Permissions.ToList(),
            new PosCheckoutSummaryRequestDto(request.DeviceId, request.SaleType, request.CustomerId, lines, null),
            _clock.UtcNow, cancellationToken);

    private static string? ValidateRequest(PosDiscountValidationRequestDto request, bool requireIdempotencyKey)
    {
        if (request is null || request.DeviceId == Guid.Empty)
            return "Device id is required.";
        if (string.Equals(request.DiscountSource, "POLICY", StringComparison.OrdinalIgnoreCase) &&
            (!request.DiscountId.HasValue || request.DiscountId == Guid.Empty))
            return "Discount policy id is required for predefined discounts.";
        if (request.Lines is null || request.Lines.Count == 0 || request.Lines.Any(x => x.VariantId == Guid.Empty || x.Qty <= 0))
            return "At least one valid cart line is required.";
        if (requireIdempotencyKey && string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return "Idempotency key is required when applying a discount.";
        if (request.IdempotencyKey?.Length > 100) return "Idempotency key cannot exceed 100 characters.";
        if (request.Reason?.Length > 500) return "Discount reason cannot exceed 500 characters.";
        return null;
    }

    private static string? NormalizeSource(string? source) => source?.Trim().ToUpperInvariant() switch
    {
        "POLICY" => "POLICY",
        "MANUAL" => "MANUAL",
        _ => null
    };

    private static string? NormalizeScope(string? scope, string policyScope)
    {
        var value = string.IsNullOrWhiteSpace(scope) ? policyScope : scope.Trim().ToUpperInvariant();
        return value is "ORDER" or "LINE" ? value : null;
    }

    private static string ContextErrorMessage(string? code) => code switch
    {
        "pos_discounts.device_not_found" => "POS device could not be found.",
        "pos_discounts.device_not_trusted" => "This POS device is not trusted.",
        "pos_discounts.till_not_assigned" => "No till is assigned to this POS device.",
        "pos_discounts.till_session_not_open" => "An open till session is required.",
        "pos_discounts.discount_not_found" => "The discount is not available for this POS context.",
        "pos_discounts.policy_not_applicable" => "The discount policy is not applicable to the current cart context.",
        "pos_discounts.manual_configuration_not_found" => "Manual discount configuration is not available for the requested scope and calculation method.",
        "pos_discounts.scope_mismatch" => "Requested discount scope does not match the policy scope.",
        _ => "The POS discount context could not be resolved."
    };

    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));

    private static int ToMoney(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static IReadOnlyCollection<Guid> VariantIdsForScope(
        string? scope, Guid? targetVariantId, IReadOnlyList<PosCheckoutLineRequestDto> lines)
    {
        var normalized = scope?.Trim().ToUpperInvariant();
        if (normalized == "LINE")
        {
            return targetVariantId.HasValue && targetVariantId.Value != Guid.Empty
                ? [targetVariantId.Value]
                : [];
        }

        return lines.Select(x => x.VariantId)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
    }
}
