using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosCheckoutService : IPosCheckoutService
{
    private static readonly ApplicationError CartPermissionDenied = new(
        "pos_cart.permission_denied",
        "You do not have permission to update the POS cart.");

    private static readonly ApplicationError PermissionDenied = new(
        "pos_checkout.permission_denied",
        "You do not have permission to checkout POS sales.");

    private static readonly ApplicationError InvalidDeviceId = new(
        "pos_checkout.invalid_device_id",
        "Device id is required.");

    private static readonly ApplicationError InvalidLines = new(
        "pos_checkout.invalid_lines",
        "Checkout requires at least one cart line.");

    private static readonly ApplicationError InvalidSaleType = new(
        "pos_checkout.invalid_sale_type",
        "Sale type must be NewSale.");

    private static readonly ApplicationError DeviceNotFound = new(
        "pos_checkout.device_not_found",
        "POS device could not be found.");

    private static readonly ApplicationError TillSessionNotOpen = new(
        "pos_checkout.till_session_not_open",
        "An open till session is required before checkout.");

    private static readonly ApplicationError VariantNotFound = new(
        "pos_checkout.variant_not_found",
        "One or more product variants could not be found.");

    private static readonly ApplicationError CustomerNotFound = new(
        "pos_checkout.customer_not_found",
        "The selected customer could not be found.");

    private static readonly ApplicationError CustomerInactive = new(
        "pos_checkout.customer_inactive",
        "Inactive customers cannot be used at checkout.");

    private static readonly ApplicationError CustomerBlocked = new(
        "pos_checkout.customer_blocked",
        "Blocked customers cannot be used at checkout.");

    private static readonly ApplicationError CustomerDeleted = new(
        "pos_checkout.customer_deleted",
        "Deleted customers cannot be used at checkout.");

    private static readonly ApplicationError CustomerNotEligible = new(
        "pos_checkout.customer_not_eligible",
        "The selected customer is not eligible for checkout.");

    private readonly IPosCheckoutRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosCheckoutService(
        IPosCheckoutRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<ApplicationResult<PosCheckoutSummaryResponseDto>> CalculateCartAsync(
        TenantRequestContext context,
        PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Cart.UpdateItem))
        {
            return Task.FromResult(
                ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(CartPermissionDenied));
        }

        return CalculateSummaryAsync(context, request, cancellationToken);
    }

    public async Task<ApplicationResult<PosCheckoutSummaryResponseDto>> GetSummaryAsync(
        TenantRequestContext context,
        PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Sale.Checkout))
        {
            return ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(PermissionDenied);
        }

        return await CalculateSummaryAsync(context, request, cancellationToken);
    }

    private async Task<ApplicationResult<PosCheckoutSummaryResponseDto>> CalculateSummaryAsync(
        TenantRequestContext context,
        PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken)
    {

        if (request.DeviceId == Guid.Empty)
        {
            return ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(InvalidDeviceId);
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(InvalidLines);
        }

        if (!IsSupportedSaleType(request.SaleType))
        {
            return ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(InvalidSaleType);
        }

        var result = await _repository.CalculateSummaryAsync(
            context.TenantId,
            context.UserId,
            context.Permissions.ToList(),
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess || result.Summary is null)
        {
            return ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(
                result.ErrorCode switch
                {
                    "pos_checkout.device_not_found" => DeviceNotFound,
                    "till_session.not_found" or
                    "till_session.device_not_trusted" or
                    "till_session.till_not_assigned" or
                    "pos_checkout.till_session_not_open" => TillSessionNotOpen,
                    "pos_checkout.variant_not_found" => VariantNotFound,
                    "pos_checkout.customer_not_found" => CustomerNotFound,
                    "pos_checkout.customer_inactive" => CustomerInactive,
                    "pos_checkout.customer_blocked" => CustomerBlocked,
                    "pos_checkout.customer_deleted" => CustomerDeleted,
                    "pos_checkout.customer_not_eligible" => CustomerNotEligible,
                    "pos_checkout.invalid_lines" => InvalidLines,
                    "pos_checkout.invalid_sale_type" => InvalidSaleType,
                    _ => new ApplicationError(
                        result.ErrorCode ?? "pos_checkout.summary_failed",
                        "Checkout summary could not be calculated.")
                });
        }

        return ApplicationResult<PosCheckoutSummaryResponseDto>.Success(result.Summary);
    }

    public async Task<ApplicationResult<PosCheckoutStartPaymentResponseDto>> StartPaymentAsync(
        TenantRequestContext context,
        PosCheckoutStartPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Sale.Checkout))
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(PermissionDenied);
        }

        if (request.DeviceId == Guid.Empty)
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(InvalidDeviceId);
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(InvalidLines);
        }


        if (!IsSupportedSaleType(request.SaleType))
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(InvalidSaleType);
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(
                new ApplicationError(
                    "pos_checkout.invalid_payment_method",
                    "A payment method is required."));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 100)
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(
                new ApplicationError("pos_checkout.invalid_idempotency_key",
                    "A valid idempotency key of at most 100 characters is required."));
        }

        var result = await _repository.StartPaymentAsync(
            context.TenantId,
            context.UserId,
            context.Permissions.ToList(),
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess || result.Payment is null)
        {
            return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(
                result.ErrorCode switch
                {
                    "pos_checkout.device_not_found" => DeviceNotFound,
                    "till_session.not_found" or
                    "till_session.device_not_trusted" or
                    "till_session.till_not_assigned" or
                    "pos_checkout.till_session_not_open" => TillSessionNotOpen,
                    "pos_checkout.variant_not_found" => VariantNotFound,
                    "pos_checkout.customer_not_found" => CustomerNotFound,
                    "pos_checkout.customer_inactive" => CustomerInactive,
                    "pos_checkout.customer_blocked" => CustomerBlocked,
                    "pos_checkout.customer_deleted" => CustomerDeleted,
                    "pos_checkout.customer_not_eligible" => CustomerNotEligible,
                    "pos_checkout.invalid_lines" => InvalidLines,
                    "pos_checkout.invalid_sale_type" => InvalidSaleType,
                    "pos_checkout.invalid_payment_method" => new ApplicationError(
                        "pos_checkout.invalid_payment_method",
                        "The selected payment method is not supported."),
                    "pos_checkout.payment_permission_denied" => new ApplicationError(
                        "pos_checkout.payment_permission_denied",
                        "You do not have permission to accept this payment method."),
                    "pos_checkout.price_not_configured" => new ApplicationError(
                        "pos_checkout.price_not_configured",
                        "One or more cart lines do not have a configured price."),
                    "pos_checkout.insufficient_stock" => new ApplicationError(
                        "pos_checkout.insufficient_stock",
                        "One or more cart lines do not have enough stock."),
                    "pos_checkout.cash_received_required" => new ApplicationError(
                        "pos_checkout.cash_received_required",
                        "Cash received is required for cash payments."),
                    "pos_checkout.insufficient_cash" => new ApplicationError(
                        "pos_checkout.insufficient_cash",
                        "Cash received is less than the amount due."),
                    "pos_checkout.payment_method_not_found" => new ApplicationError(
                        "pos_checkout.payment_method_not_found",
                        "The selected payment method could not be found."),
                    "pos_checkout.payment_provider_required" => new ApplicationError(
                        "pos_checkout.payment_provider_required",
                        "This payment method requires its provider confirmation flow."),
                    "pos_checkout.idempotency_conflict" => new ApplicationError(
                        "pos_checkout.idempotency_conflict",
                        "The idempotency key was already used for a different checkout request."),
                    "pos_checkout.stock_conflict" => new ApplicationError(
                        "pos_checkout.stock_conflict",
                        "Stock changed while the payment was being completed. Recalculate and retry."),
                    _ => new ApplicationError(
                        result.ErrorCode ?? "pos_checkout.start_payment_failed",
                        "Checkout payment could not be started.")
                });
        }

        return ApplicationResult<PosCheckoutStartPaymentResponseDto>.Success(result.Payment);
    }

    private static bool IsSupportedSaleType(string? saleType) =>
        string.IsNullOrWhiteSpace(saleType) ||
        string.Equals(saleType.Trim(), "NewSale", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(saleType.Trim(), "New Sale", StringComparison.OrdinalIgnoreCase);
}
