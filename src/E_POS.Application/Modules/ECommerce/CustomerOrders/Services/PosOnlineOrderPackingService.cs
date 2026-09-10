using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderPackingService : IPosOnlineOrderPackingService
{
    public const string AccessPermission = OnlineOrderPickingPermissions.OrdersAccess;
    public const string OrdersViewPermission = OnlineOrderPickingPermissions.OrdersView;
    public const string PackingViewPermission = OnlineOrderPickingPermissions.PackingView;
    public const string PackPermission = OnlineOrderPickingPermissions.PackingPack;
    public const string MarkReadyPermission = OnlineOrderPickingPermissions.CollectionMarkReady;
    public const int PackingNoteMaxLength = 200;

    private readonly IPosOnlineOrderPackingRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;

    public PosOnlineOrderPackingService(
        IPosOnlineOrderPackingRepository repository,
        ITenantFeatureEntitlementEvaluator entitlements,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosOnlineOrderPackReadyCommandResponse>> PackAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderPackRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(
            context, outletId, PackPermission, requirePackingView: true, cancellationToken);
        if (accessError is not null)
            return Failure(accessError);
        if (orderId == Guid.Empty)
            return Failure(new("online_orders.invalid_order_id", "A valid order id is required."));
        if (request.ExpectedVersion <= 0)
            return Failure(new("online_orders.invalid_expected_version", "A positive expectedVersion is required."));

        string? packingNote = null;
        if (!string.IsNullOrWhiteSpace(request.PackingNote))
        {
            packingNote = request.PackingNote.Trim();
            if (packingNote.Length > PackingNoteMaxLength)
            {
                return Failure(new(
                    "online_orders.invalid_packing_note",
                    $"Packing note must not exceed {PackingNoteMaxLength} characters."));
            }
        }

        var result = await _repository.PackAsync(
            context.TenantId,
            context.UserId,
            outletId,
            orderId,
            new PosOnlineOrderPackRequest
            {
                PackingNote = packingNote,
                ExpectedVersion = request.ExpectedVersion
            },
            _clock.UtcNow,
            cancellationToken);
        return result.IsSuccess && result.Command is not null
            ? ApplicationResult<PosOnlineOrderPackReadyCommandResponse>.Success(result.Command)
            : Failure(MapRepositoryError(result.ErrorCode));
    }

    public async Task<ApplicationResult<PosOnlineOrderPackReadyCommandResponse>> MarkReadyAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderReadyRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(
            context, outletId, MarkReadyPermission, requirePackingView: true, cancellationToken);
        if (accessError is not null)
            return Failure(accessError);
        if (orderId == Guid.Empty)
            return Failure(new("online_orders.invalid_order_id", "A valid order id is required."));
        if (request.ExpectedVersion <= 0)
            return Failure(new("online_orders.invalid_expected_version", "A positive expectedVersion is required."));

        var result = await _repository.MarkReadyAsync(
            context.TenantId,
            context.UserId,
            outletId,
            orderId,
            new PosOnlineOrderReadyRequest { ExpectedVersion = request.ExpectedVersion },
            _clock.UtcNow,
            cancellationToken);
        return result.IsSuccess && result.Command is not null
            ? ApplicationResult<PosOnlineOrderPackReadyCommandResponse>.Success(result.Command)
            : Failure(MapRepositoryError(result.ErrorCode));
    }

    private async Task<ApplicationError?> ValidateBaseAsync(
        TenantRequestContext context,
        Guid outletId,
        string operationPermission,
        bool requirePackingView,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return new("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(AccessPermission) ||
            !context.HasPermission(OrdersViewPermission) ||
            (requirePackingView && !context.HasPermission(PackingViewPermission)) ||
            !context.HasPermission(operationPermission))
            return new("online_orders.permission_denied", "Permission denied for online-order packing.");
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
        "online_orders.invalid_state" => new(code, "The order is not available for packing or ready."),
        "online_orders.concurrency_conflict" => new(code, "The order changed. Refresh before trying again."),
        "online_orders.not_packable" => new(code, "The order is not eligible to pack yet."),
        "online_orders.not_readyable" => new(code, "The order must be packed before it can be marked ready."),
        "online_orders.invalid_pickup" => new(code, "Pickup readiness could not be updated for this order."),
        _ => new(code ?? "online_orders.packing_failed", "Online-order packing could not be completed.")
    };

    private static ApplicationResult<PosOnlineOrderPackReadyCommandResponse> Failure(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderPackReadyCommandResponse>.Failure(error);
}
