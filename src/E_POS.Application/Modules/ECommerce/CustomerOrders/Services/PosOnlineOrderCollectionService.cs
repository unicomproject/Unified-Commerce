using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderCollectionService : IPosOnlineOrderCollectionService
{
    public const string AccessPermission = OnlineOrderPickingPermissions.OrdersAccess;
    public const string OrdersViewPermission = OnlineOrderPickingPermissions.OrdersView;
    public const string ScanQrPermission = OnlineOrderPickingPermissions.CollectionScanQr;
    public const string ValidateQrPermission = OnlineOrderPickingPermissions.CollectionValidateQr;
    public const string HandoverPermission = OnlineOrderPickingPermissions.CollectionHandover;
    public const string CollectPermission = OnlineOrderPickingPermissions.CollectionCollect;

    private readonly IPosOnlineOrderCollectionRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;

    public PosOnlineOrderCollectionService(
        IPosOnlineOrderCollectionRepository repository,
        ITenantFeatureEntitlementEvaluator entitlements,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosOnlineOrderCollectionValidateResponse>> ValidateQrAsync(
        TenantRequestContext context,
        Guid outletId,
        PosOnlineOrderCollectionValidateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(
            context,
            outletId,
            requireScanAndValidate: true,
            requireHandoverAndCollect: false,
            cancellationToken);
        if (accessError is not null)
            return FailureValidate(accessError);

        if (string.IsNullOrWhiteSpace(request.Token))
            return FailureValidate(new("online_orders.collection.qr_invalid", "A collection QR token is required."));

        var tokenHash = HashToken(request.Token.Trim());
        var result = await _repository.ValidateQrAsync(
            context.TenantId,
            context.UserId,
            outletId,
            tokenHash,
            _clock.UtcNow,
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? ApplicationResult<PosOnlineOrderCollectionValidateResponse>.Success(result.Value)
            : FailureValidate(MapRepositoryError(result.ErrorCode));
    }

    public async Task<ApplicationResult<PosOnlineOrderCollectionCompleteResponse>> CompleteAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderCollectionCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(
            context,
            outletId,
            requireScanAndValidate: false,
            requireHandoverAndCollect: true,
            cancellationToken);
        if (accessError is not null)
            return FailureComplete(accessError);

        if (orderId == Guid.Empty)
            return FailureComplete(new("online_orders.invalid_order_id", "A valid order id is required."));
        if (request.ExpectedVersion <= 0)
            return FailureComplete(new("online_orders.invalid_expected_version", "A positive expectedVersion is required."));

        var result = await _repository.CompleteAsync(
            context.TenantId,
            context.UserId,
            outletId,
            orderId,
            request.ExpectedVersion,
            _clock.UtcNow,
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? ApplicationResult<PosOnlineOrderCollectionCompleteResponse>.Success(result.Value)
            : FailureComplete(MapRepositoryError(result.ErrorCode));
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private async Task<ApplicationError?> ValidateBaseAsync(
        TenantRequestContext context,
        Guid outletId,
        bool requireScanAndValidate,
        bool requireHandoverAndCollect,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return new("online_orders.invalid_tenant_context", "Invalid tenant context.");

        if (!context.HasPermission(AccessPermission) ||
            !context.HasPermission(OrdersViewPermission) ||
            (requireScanAndValidate &&
             (!context.HasPermission(ScanQrPermission) || !context.HasPermission(ValidateQrPermission))) ||
            (requireHandoverAndCollect &&
             (!context.HasPermission(HandoverPermission) || !context.HasPermission(CollectPermission))))
            return new("online_orders.permission_denied", "Permission denied for online-order collection.");

        if (outletId == Guid.Empty)
            return new("online_orders.invalid_outlet", "A valid outlet id is required.");

        var entitlement = await _entitlements.EvaluateAsync(
            context.TenantId, PlatformTenantFeatureCodes.ClickCollect,
            _clock.UtcNow, cancellationToken);
        return entitlement.IsAllowed
            ? null
            : new("online_orders.feature_not_entitled", "Click & collect is not enabled for this tenant.");
    }

    private static ApplicationError MapRepositoryError(string? code) => code switch
    {
        "online_orders.outlet_access_denied" => new(code, "You do not have access to this outlet."),
        "online_orders.not_found" => new(code, "Online order was not found."),
        "online_orders.collection.qr_invalid" => new(code, "The collection QR token is invalid."),
        "online_orders.collection.qr_expired" => new(code, "The collection QR token has expired."),
        "online_orders.collection.wrong_outlet" => new(code, "This order belongs to a different outlet."),
        "online_orders.collection.not_ready" => new(code, "This order is not ready for collection."),
        "online_orders.collection.cancelled" => new(code, "This order has been cancelled."),
        "online_orders.collection.already_collected" => new(code, "This order has already been collected."),
        "online_orders.collection.missing_graph" => new(code, "Collection data is incomplete for this order."),
        "online_orders.collection.payment_required" => new(code, "Outstanding balance must be settled before collection."),
        "online_orders.concurrency_conflict" => new(code, "The order changed. Refresh before trying again."),
        "online_orders.invalid_state" => new(code, "The order is not available for collection."),
        _ => new(code ?? "online_orders.collection_failed", "Online-order collection could not be completed.")
    };

    private static ApplicationResult<PosOnlineOrderCollectionValidateResponse> FailureValidate(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderCollectionValidateResponse>.Failure(error);

    private static ApplicationResult<PosOnlineOrderCollectionCompleteResponse> FailureComplete(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderCollectionCompleteResponse>.Failure(error);
}
