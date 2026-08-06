using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformTenantOnboardingService : IPlatformTenantOnboardingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IPlatformTenantOnboardingRepository _repository;
    private readonly IPlatformTenantService _tenantService;
    private readonly IPlatformPermissionChecker _permissions;
    private readonly IDateTimeProvider _clock;

    public PlatformTenantOnboardingService(IPlatformTenantOnboardingRepository repository,
        IPlatformTenantService tenantService, IPlatformPermissionChecker permissions, IDateTimeProvider clock)
    {
        _repository = repository;
        _tenantService = tenantService;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<ApplicationResult<TenantOnboardingDraftResponse>> CreateDraftAsync(CreateTenantOnboardingDraftRequest request, Guid actorId, CancellationToken ct)
    {
        if (!await Has(actorId, PlatformPermissionCodes.TenantsCreate, ct)) return Failure<TenantOnboardingDraftResponse>("access_denied", "Tenant onboarding access denied.");
        var payload = request.Payload ?? EmptyPayload();
        var evaluation = TenantOnboardingProgressEvaluator.Evaluate(payload);
        var now = _clock.UtcNow;
        var draft = PlatformTenantOnboardingDraft.Create(Guid.NewGuid(), actorId, Serialize(payload), ClampStep(request.CurrentStep),
            evaluation.Mask, evaluation.Percent, now, now.AddDays(30));
        await _repository.AddDraftAsync(draft, ct);
        return ApplicationResult<TenantOnboardingDraftResponse>.Success(MapDraft(draft, payload));
    }

    public async Task<ApplicationResult<TenantOnboardingDraftListResponse>> ListDraftsAsync(Guid actorId, bool includeAll, CancellationToken ct)
    {
        if (!await Has(actorId, PlatformPermissionCodes.TenantsCreate, ct)) return Failure<TenantOnboardingDraftListResponse>("access_denied", "Tenant onboarding access denied.");
        var canAll = includeAll && await Has(actorId, PlatformPermissionCodes.TenantsUpdate, ct);
        var rows = await _repository.ListDraftsAsync(actorId, canAll, ct);
        var items = rows.Select(x =>
        {
            var payload = Deserialize(x.PayloadJson);
            return new TenantOnboardingDraftSummaryResponse(x.Id, payload.BasicDetails?.DisplayName, payload.BasicDetails?.TenantCode,
                x.Status, x.CurrentStep, x.ProgressPercent, x.OwnerPlatformUserId, x.UpdatedAt, x.ExpiresAt, x.Version);
        }).ToArray();
        return ApplicationResult<TenantOnboardingDraftListResponse>.Success(new(items, items.Length));
    }

    public async Task<ApplicationResult<TenantOnboardingDraftResponse>> GetDraftAsync(Guid draftId, Guid actorId, CancellationToken ct)
    {
        var draft = await _repository.GetDraftAsync(draftId, ct, tracking: false);
        if (draft is null || !await CanAccess(draft, actorId, ct)) return Failure<TenantOnboardingDraftResponse>("not_found", "Tenant onboarding draft not found.");
        return ApplicationResult<TenantOnboardingDraftResponse>.Success(MapDraft(draft, Deserialize(draft.PayloadJson)));
    }

    public async Task<ApplicationResult<TenantOnboardingDraftResponse>> UpdateDraftAsync(Guid draftId, UpdateTenantOnboardingDraftRequest request, long expectedVersion, Guid actorId, CancellationToken ct)
    {
        var draft = await _repository.GetDraftAsync(draftId, ct);
        if (draft is null || !await CanAccess(draft, actorId, ct)) return Failure<TenantOnboardingDraftResponse>("not_found", "Tenant onboarding draft not found.");
        if (draft.Version != expectedVersion) return Failure<TenantOnboardingDraftResponse>("concurrency_conflict", $"Draft changed. Latest version is {draft.Version}.");
        var evaluation = TenantOnboardingProgressEvaluator.Evaluate(request.Payload);
        var basic = request.Payload.BasicDetails;
        var now = _clock.UtcNow;
        draft.Update(Serialize(request.Payload), ClampStep(request.CurrentStep), evaluation.Mask, evaluation.Percent,
            basic?.TenantCode, basic?.TenantSlug, basic?.RequestedSubdomain, request.Payload.TenantAdmin?.Email,
            actorId, now, now.AddDays(30));
        try { await _repository.SaveChangesAsync(ct); }
        catch (TenantOnboardingConcurrencyException) { return Failure<TenantOnboardingDraftResponse>("concurrency_conflict", "Draft changed while it was being saved."); }
        return ApplicationResult<TenantOnboardingDraftResponse>.Success(MapDraft(draft, request.Payload));
    }

    public async Task<ApplicationResult> DiscardDraftAsync(Guid draftId, long expectedVersion, Guid actorId, CancellationToken ct)
    {
        var draft = await _repository.GetDraftAsync(draftId, ct);
        if (draft is null || !await CanAccess(draft, actorId, ct)) return ApplicationResult.Failure(Error("not_found", "Tenant onboarding draft not found."));
        if (draft.Version != expectedVersion) return ApplicationResult.Failure(Error("concurrency_conflict", $"Draft changed. Latest version is {draft.Version}."));
        draft.Discard(actorId, _clock.UtcNow);
        await _repository.SaveChangesAsync(ct);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<TenantOnboardingValidationResponse>> ValidateDraftAsync(Guid draftId, Guid actorId, CancellationToken ct)
    {
        var draft = await _repository.GetDraftAsync(draftId, ct, tracking: false);
        if (draft is null || !await CanAccess(draft, actorId, ct)) return Failure<TenantOnboardingValidationResponse>("not_found", "Tenant onboarding draft not found.");
        var result = TenantOnboardingProgressEvaluator.Evaluate(Deserialize(draft.PayloadJson));
        return ApplicationResult<TenantOnboardingValidationResponse>.Success(new(result.Errors.Count == 0, result.Steps, result.Percent, result.Errors, []));
    }

    public async Task<ApplicationResult<TenantOnboardingReceiptResponse>> FinalizeAsync(Guid draftId, FinalizeTenantOnboardingRequest request,
        long expectedVersion, string idempotencyKey, Guid actorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            return Failure<TenantOnboardingReceiptResponse>("precondition_required", "A valid Idempotency-Key is required.");
        var draft = await _repository.GetDraftAsync(draftId, ct, tracking: false);
        if (draft is null || !await CanAccess(draft, actorId, ct)) return Failure<TenantOnboardingReceiptResponse>("not_found", "Tenant onboarding draft not found.");
        var keyHash = Hash(idempotencyKey.Trim());
        var operation = await _repository.GetOperationByDraftAsync(draftId, ct);
        if (draft.Status == "completed" && operation is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(operation.IdempotencyKeyHash), Convert.FromHexString(keyHash)))
                return Failure<TenantOnboardingReceiptResponse>("idempotency_conflict", "Draft was finalized by a different request.");
            return ApplicationResult<TenantOnboardingReceiptResponse>.Success(MapReceipt(operation, InferTenantStatus(operation), true));
        }
        if (draft.Version != expectedVersion) return Failure<TenantOnboardingReceiptResponse>("concurrency_conflict", $"Draft changed. Latest version is {draft.Version}.");
        if (!request.FinalReviewConfirmed) return Failure<TenantOnboardingReceiptResponse>("validation_failed", "Final review confirmation is required.");

        var payload = Deserialize(draft.PayloadJson) with { ReviewConfirmed = true };
        var validation = TenantOnboardingProgressEvaluator.Evaluate(payload);
        if (validation.Mask != 127) return Failure<TenantOnboardingReceiptResponse>("validation_failed", string.Join(" ", validation.Errors.Select(x => x.Message)));
        var requestHash = Hash(Serialize(payload) + "|" + Serialize(request));
        var now = _clock.UtcNow;
        var paid = string.Equals(payload.Plan?.SubscriptionType, "PAID", StringComparison.OrdinalIgnoreCase);
        var operationId = Guid.NewGuid();
        var finalizeContext = new PlatformTenantOnboardingFinalizeContext(
            draft.Id, expectedVersion, operationId, keyHash, requestHash, paid,
            BuildContactWrites(payload), actorId, now);
        ApplicationResult<PlatformTenantDetailResponse> tenantResult;
        try
        {
            tenantResult = await _tenantService.CreateTenantAsync(
                MapCreateRequest(payload) with { OnboardingFinalizeContext = finalizeContext }, actorId, ct);
        }
        catch (TenantOnboardingAlreadyFinalizedException ex)
        {
            var completed = await _repository.GetOperationByDraftAsync(draftId, ct);
            if (!ex.SameRequest || completed is null)
                return Failure<TenantOnboardingReceiptResponse>("idempotency_conflict", "Draft was finalized by a different request.");
            return ApplicationResult<TenantOnboardingReceiptResponse>.Success(MapReceipt(completed, InferTenantStatus(completed), true));
        }
        catch (TenantOnboardingConcurrencyException)
        {
            return Failure<TenantOnboardingReceiptResponse>("concurrency_conflict", "Draft changed while finalization was in progress.");
        }
        if (tenantResult.IsFailure || tenantResult.Value is null)
        {
            return ApplicationResult<TenantOnboardingReceiptResponse>.Failure(tenantResult.Error);
        }

        operation = await _repository.GetOperationAsync(operationId, ct);
        if (operation is null)
            return Failure<TenantOnboardingReceiptResponse>("operation_missing", "Tenant committed but onboarding operation could not be loaded.");
        return ApplicationResult<TenantOnboardingReceiptResponse>.Success(MapReceipt(operation, tenantResult.Value.LifecycleStatus, false));
    }

    public async Task<ApplicationResult<TenantOnboardingOperationResponse>> GetOperationAsync(Guid operationId, Guid actorId, CancellationToken ct)
    {
        if (!await Has(actorId, PlatformPermissionCodes.TenantsView, ct)) return Failure<TenantOnboardingOperationResponse>("access_denied", "Tenant onboarding access denied.");
        var operation = await _repository.GetOperationAsync(operationId, ct);
        if (operation is null) return Failure<TenantOnboardingOperationResponse>("not_found", "Tenant onboarding operation not found.");
        return ApplicationResult<TenantOnboardingOperationResponse>.Success(MapOperation(operation));
    }

    public async Task<ApplicationResult<TenantOnboardingOperationResponse>> RetryOperationAsync(Guid operationId, Guid actorId, CancellationToken ct)
    {
        var current = await _repository.GetOperationAsync(operationId, ct);
        if (current is null) return Failure<TenantOnboardingOperationResponse>("not_found", "Tenant onboarding operation not found.");
        var permission = current.PaymentStatus == "NOT_REQUIRED"
            ? PlatformPermissionCodes.TenantsUpdate : PlatformPermissionCodes.BillingManage;
        if (!await Has(actorId, permission, ct))
            return Failure<TenantOnboardingOperationResponse>("access_denied", "Tenant onboarding retry access denied.");
        var retried = await _repository.RetryOperationAsync(operationId, _clock.UtcNow, ct);
        if (!retried) return Failure<TenantOnboardingOperationResponse>("invalid_transition", "Operation has no failed delivery to retry.");
        var operation = await _repository.GetOperationAsync(operationId, ct);
        return operation is null ? Failure<TenantOnboardingOperationResponse>("not_found", "Tenant onboarding operation not found.") :
            ApplicationResult<TenantOnboardingOperationResponse>.Success(MapOperation(operation));
    }

    public async Task<ApplicationResult<TenantOnboardingOperationResponse>> ResendInvitationAsync(Guid tenantId,
        string idempotencyKey, Guid actorId, CancellationToken ct)
    {
        if (!await Has(actorId, PlatformPermissionCodes.TenantsUpdate, ct))
            return Failure<TenantOnboardingOperationResponse>("access_denied", "Tenant invitation resend access denied.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 100)
            return Failure<TenantOnboardingOperationResponse>("precondition_required", "A valid Idempotency-Key is required.");
        var keyHash = Hash(idempotencyKey.Trim());
        var requestHash = Hash($"invitation-resend:{tenantId:D}");
        var result = await _repository.ResendInvitationAsync(tenantId, keyHash, requestHash, actorId, _clock.UtcNow, ct);
        if (result.Outcome is TenantInvitationResendOutcome.Success or TenantInvitationResendOutcome.Replay)
        {
            var operation = await _repository.GetOperationByTenantAsync(tenantId, ct);
            return operation is null ? Failure<TenantOnboardingOperationResponse>("not_found", "Tenant onboarding operation not found.") :
                ApplicationResult<TenantOnboardingOperationResponse>.Success(MapOperation(operation));
        }
        return result.Outcome switch
        {
            TenantInvitationResendOutcome.NotFound => Failure<TenantOnboardingOperationResponse>("not_found", "Tenant onboarding operation not found."),
            TenantInvitationResendOutcome.IdempotencyConflict => Failure<TenantOnboardingOperationResponse>("idempotency_conflict", "Idempotency key was reused with a different request."),
            TenantInvitationResendOutcome.RateLimited => Failure<TenantOnboardingOperationResponse>("rate_limited", "Invitation was resent too recently."),
            _ => Failure<TenantOnboardingOperationResponse>("invalid_transition", "Tenant invitation cannot be resent in its current state.")
        };
    }


    private static CreatePlatformTenantRequest MapCreateRequest(TenantOnboardingPayloadDto p)
    {
        var b = p.BasicDetails!; var c = p.BusinessContact!; var plan = p.Plan!; var billing = p.Billing!; var admin = p.TenantAdmin!;
        var address = c.RegisteredAddress!;
        return new CreatePlatformTenantRequest
        {
            Code = b.TenantCode, Name = b.DisplayName, TenantSlug = b.TenantSlug, RequestedSubdomain = b.RequestedSubdomain,
            LegalName = b.LegalName, RegistrationNumber = b.RegistrationNumber, TaxNumber = b.TaxNumber, WebsiteUrl = c.WebsiteUrl,
            BaseCurrency = b.BaseCurrencyCode, DefaultTimezone = b.Timezone, DefaultLocale = b.Locale, OperatingMode = b.OperatingMode,
            BusinessType = b.BusinessTypeCode, CountryCode = address.CountryCode, BillingStatus = plan.SubscriptionType == "PAID" ? "pending" : "paid",
            SubscriptionPlanId = plan.SubscriptionPlanId,
            Address = new() { Line1 = address.Line1, Line2 = address.Line2, City = address.City, State = address.StateOrProvince, PostalCode = address.PostalCode, CountryCode = address.CountryCode },
            PrimaryContact = new() { Name = c.PrimaryContact?.Name, Email = c.PrimaryContact?.Email, Phone = c.PrimaryContact?.Phone },
            Limits = plan.RequestedLimits is null ? null : new() { MaxOutlets = plan.RequestedLimits.MaxOutlets, MaxTills = plan.RequestedLimits.MaxTills, MaxUsers = plan.RequestedLimits.MaxUsers },
            Addons = plan.Addons?.Select(x => new CreatePlatformTenantAddonSelectionRequest { AddonId = x.AddonId, Quantity = x.Quantity }).ToArray(),
            EnabledFeatureIds = p.Entitlements?.FeatureIds,
            TenantAdmin = new() { FirstName = admin.FirstName, LastName = admin.LastName, Email = admin.Email, Phone = admin.Phone, SendInvite = true },
            Subscription = new() { SubscriptionType = plan.SubscriptionType, BillingCycle = plan.BillingCycle,
                SubscriptionStatus = plan.SubscriptionType == "TRIAL" ? "TRIAL" : "ACTIVE", TrialStartAt = billing.TrialStartAt,
                TrialEndAt = billing.TrialEndAt, BillingStartAt = billing.BillingStartAt, NextBillingAt = billing.NextBillingAt,
                AutoRenew = billing.AutoRenew, DiscountType = billing.DiscountType, DiscountValue = billing.DiscountValue,
                TaxPercentage = billing.TaxPercentage, InvoiceEmail = billing.InvoiceEmail, PaymentMethod = billing.PaymentMethod,
                Notes = billing.Notes, CreateDraftInvoice = plan.SubscriptionType == "PAID" }
        };
    }

    private static IReadOnlyList<PlatformTenantOnboardingContactWriteDto> BuildContactWrites(TenantOnboardingPayloadDto p)
    {
        var result = new List<PlatformTenantOnboardingContactWriteDto>(); var c = p.BusinessContact!;
        var billing = c.BillingContactSameAsPrimary ? c.PrimaryContact : c.BillingContact;
        if (billing is not null && !string.IsNullOrWhiteSpace(billing.Name)) result.Add(new("BILLING", billing.Name!, billing.Email, billing.Phone));
        if (c.SupportContact is { } support && !string.IsNullOrWhiteSpace(support.Name) && (!string.IsNullOrWhiteSpace(support.Email) || !string.IsNullOrWhiteSpace(support.Phone)))
            result.Add(new("SUPPORT", support.Name!, support.Email, support.Phone));
        return result;
    }

    private async Task<bool> CanAccess(PlatformTenantOnboardingDraft draft, Guid actorId, CancellationToken ct) =>
        draft.OwnerPlatformUserId == actorId ? await Has(actorId, PlatformPermissionCodes.TenantsCreate, ct) : await Has(actorId, PlatformPermissionCodes.TenantsUpdate, ct);
    private Task<bool> Has(Guid actorId, string permission, CancellationToken ct) => _permissions.HasPermissionAsync(actorId, permission, ct);
    private static short ClampStep(short step) => Math.Clamp(step, (short)1, (short)7);
    private static TenantOnboardingPayloadDto EmptyPayload() => new(null, null, null, null, null, null);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    private static TenantOnboardingPayloadDto Deserialize(string json) => JsonSerializer.Deserialize<TenantOnboardingPayloadDto>(json, JsonOptions) ?? EmptyPayload();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static ApplicationError Error(string suffix, string message) => new($"platform_tenant_onboarding.{suffix}", message);
    private static ApplicationResult<T> Failure<T>(string suffix, string message) => ApplicationResult<T>.Failure(Error(suffix, message));
    private static TenantOnboardingDraftResponse MapDraft(PlatformTenantOnboardingDraft d, TenantOnboardingPayloadDto p) =>
        new(d.Id, d.OwnerPlatformUserId, d.Status, d.CurrentStep, Enumerable.Range(1, 7).Where(s => (d.CompletedStepsMask & (1 << (s - 1))) != 0).ToArray(),
            d.ProgressPercent, p, d.SchemaVersion, d.Version, d.CreatedAt, d.UpdatedAt, d.ExpiresAt, d.CreatedTenantId, []);
    private static TenantOnboardingReceiptResponse MapReceipt(PlatformTenantOnboardingOperation o, string tenantStatus, bool replay) =>
        new(o.TenantId, o.DraftId, o.Id, tenantStatus, o.ProvisioningStatus, o.PaymentStatus, o.InvitationStatus, o.CreatedAt, replay);
    private static TenantOnboardingOperationResponse MapOperation(PlatformTenantOnboardingOperation o) =>
        new(o.Id, o.DraftId, o.TenantId, o.Status, o.ProvisioningStatus, o.PaymentStatus, o.InvitationStatus,
            o.AttemptCount, o.FailureCode, o.Status == "FAILED_RETRYABLE", o.NextRetryAt, o.Version, o.UpdatedAt);
    private static string InferTenantStatus(PlatformTenantOnboardingOperation operation) =>
        operation.PaymentStatus is "AWAITING_PAYMENT" or "PAYMENT_SUBMITTED" or "UNDER_REVIEW" or "ACTION_REQUIRED" or "REJECTED"
            ? "pending_payment"
            : operation.PaymentStatus == "PAID" ? "pending_activation" : "active";
}
