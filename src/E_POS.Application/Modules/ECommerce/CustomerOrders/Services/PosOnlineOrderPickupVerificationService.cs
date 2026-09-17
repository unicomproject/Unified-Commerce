using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderPickupVerificationService : IPosOnlineOrderPickupVerificationService
{
    public const string AccessPermission = OnlineOrderPickingPermissions.OrdersAccess;
    public const string OrdersViewPermission = OnlineOrderPickingPermissions.OrdersView;
    public const string VerifyPermission = OnlineOrderPickingPermissions.CollectionValidateQr;
    public const string CompletePermission = OnlineOrderPickingPermissions.CollectionCollect;
    public const int PickupCodeMaxLength = 128;

    private readonly IPosOnlineOrderPickupVerificationRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;

    public PosOnlineOrderPickupVerificationService(
        IPosOnlineOrderPickupVerificationRepository repository,
        ITenantFeatureEntitlementEvaluator entitlements,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosOnlineOrderPickupVerifyResponse>> VerifyAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderPickupVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(context, outletId, VerifyPermission, cancellationToken);
        if (accessError is not null)
            return VerifyFailure(accessError);
        if (orderId == Guid.Empty)
            return VerifyFailure(new("online_orders.invalid_order_id", "A valid order id is required."));

        var pickupCode = request.PickupCode?.Trim();
        if (string.IsNullOrWhiteSpace(pickupCode))
            return VerifyFailure(new("online_orders.invalid_pickup_code", "A pickup code is required."));
        if (pickupCode.Length > PickupCodeMaxLength)
            return VerifyFailure(new("online_orders.invalid_pickup_code", "Pickup code is not a recognised format."));

        var result = await _repository.VerifyAsync(
            context.TenantId, context.UserId, outletId, orderId, pickupCode, _clock.UtcNow, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? ApplicationResult<PosOnlineOrderPickupVerifyResponse>.Success(result.Value)
            : VerifyFailure(MapVerifyError(result.ErrorCode, result.RemainingAttempts));
    }

    public async Task<ApplicationResult<PosOnlineOrderPickupCollectResponse>> CollectAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(context, outletId, CompletePermission, cancellationToken);
        if (accessError is not null)
            return CollectFailure(accessError);
        if (orderId == Guid.Empty)
            return CollectFailure(new("online_orders.invalid_order_id", "A valid order id is required."));

        var result = await _repository.CollectAsync(
            context.TenantId, context.UserId, outletId, orderId, _clock.UtcNow, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? ApplicationResult<PosOnlineOrderPickupCollectResponse>.Success(result.Value)
            : CollectFailure(MapCollectError(result.ErrorCode));
    }

    private async Task<ApplicationError?> ValidateBaseAsync(
        TenantRequestContext context, Guid outletId, string operationPermission,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return new("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(AccessPermission) ||
            !context.HasPermission(OrdersViewPermission) ||
            !context.HasPermission(operationPermission))
            return new("online_orders.permission_denied", "Permission denied for online-order pickup collection.");
        if (outletId == Guid.Empty)
            return new("online_orders.invalid_outlet", "A valid outlet id is required.");

        var entitlement = await _entitlements.EvaluateAsync(
            context.TenantId, PlatformTenantFeatureCodes.ClickCollect,
            _clock.UtcNow, cancellationToken);
        return entitlement.IsAllowed
            ? null
            : new("online_orders.feature_not_entitled", "Click & collect is not enabled for this tenant.");
    }

    private static ApplicationError MapVerifyError(string? code, int? remainingAttempts) => code switch
    {
        "online_orders.outlet_access_denied" => new(code, "You do not have access to this outlet."),
        "online_orders.not_found" => new(code, "Online order was not found."),
        "online_orders.invalid_state" => new(code, "This order is not ready for pickup verification."),
        "online_orders.pickup_code_expired" => new(code, "This pickup code has expired. Ask the customer to refresh their order to get a new one."),
        "online_orders.pickup_code_mismatch" => new(code, remainingAttempts is > 0
            ? $"The pickup code does not match this order. {remainingAttempts} attempt(s) remaining."
            : "The pickup code does not match this order."),
        "online_orders.pickup_verification_locked" => new(
            code, "Too many failed attempts. This pickup code is locked — a new one must be issued."),
        _ => new(code ?? "online_orders.pickup_verification_failed", "Pickup verification could not be completed.")
    };

    private static ApplicationError MapCollectError(string? code) => code switch
    {
        "online_orders.outlet_access_denied" => new(code, "You do not have access to this outlet."),
        "online_orders.not_found" => new(code, "Online order was not found."),
        "online_orders.invalid_state" => new(code, "This order must be verified before it can be marked collected."),
        _ => new(code ?? "online_orders.pickup_collection_failed", "Marking the order collected could not be completed.")
    };

    private static ApplicationResult<PosOnlineOrderPickupVerifyResponse> VerifyFailure(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderPickupVerifyResponse>.Failure(error);

    private static ApplicationResult<PosOnlineOrderPickupCollectResponse> CollectFailure(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderPickupCollectResponse>.Failure(error);
}
