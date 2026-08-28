using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.ECommerce.FulfilmentPickup.Services;

public sealed class PosOnlineOrderService : IPosOnlineOrderService
{
    public const string AccessPermission = "commerce.online_order.orders.access";
    public const string ViewPermission = "commerce.online_order.orders.view";
    public const string StartFulfillmentPermission = "commerce.online_order.fulfilment.start";
    public const string PickingViewPermission = "commerce.online_order.picking.view";
    public const string PickingPermission = "commerce.online_order.picking.pick";
    public const string PickingScanPermission = "commerce.online_order.picking.scan";
    public const string PickingManualPermission = "commerce.online_order.picking.manual_entry";
    public const string PickingIssuePermission = "commerce.online_order.picking.report_issue";
    public const string PackingPermission = "commerce.online_order.packing.pack";
    public const string MarkReadyPermission = "commerce.online_order.collection.mark_ready";

    private readonly IPosOnlineOrderRepository _repository;
    private readonly IDateTimeProvider _clock;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;

    public PosOnlineOrderService(
        IPosOnlineOrderRepository repository,
        IDateTimeProvider clock,
        ITenantFeatureEntitlementEvaluator entitlements)
    {
        _repository = repository;
        _clock = clock;
        _entitlements = entitlements;
    }

    public async Task<ApplicationResult<PosOnlineOrderListDto>> ListAsync(
        TenantRequestContext context,
        PosOnlineOrderListQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateReadAsync(context, query.OutletId, cancellationToken);
        if (validation is not null)
            return ApplicationResult<PosOnlineOrderListDto>.Failure(validation);

        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return Failure<PosOnlineOrderListDto>("online_orders.invalid_pagination", "Page must be positive and pageSize must be between 1 and 100.");

        return ApplicationResult<PosOnlineOrderListDto>.Success(
            await _repository.ListAsync(context.TenantId, query, _clock.UtcNow, cancellationToken));
    }

    public async Task<ApplicationResult<PosOnlineOrderDetailDto>> GetAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateReadAsync(context, outletId, cancellationToken);
        if (validation is not null)
            return ApplicationResult<PosOnlineOrderDetailDto>.Failure(validation);
        if (orderId == Guid.Empty)
            return Failure<PosOnlineOrderDetailDto>("online_orders.invalid_order_id", "A valid order id is required.");

        var order = await _repository.GetAsync(context.TenantId, outletId, orderId, cancellationToken);
        return order is null
            ? Failure<PosOnlineOrderDetailDto>("online_orders.not_found", "Online order was not found.")
            : ApplicationResult<PosOnlineOrderDetailDto>.Success(order);
    }

    public async Task<ApplicationResult<PosStartFulfillmentDto>> StartFulfillmentAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateCommandAsync(
            context, outletId, orderId, StartFulfillmentPermission, cancellationToken);
        if (validation is not null)
            return ApplicationResult<PosStartFulfillmentDto>.Failure(validation);

        try
        {
            var result = await _repository.StartFulfillmentAsync(
                context.TenantId, outletId, orderId, context.UserId, _clock.UtcNow, cancellationToken);
            return result is null
                ? Failure<PosStartFulfillmentDto>("online_orders.not_found", "Online order was not found.")
                : ApplicationResult<PosStartFulfillmentDto>.Success(result);
        }
        catch (InvalidOperationException exception)
        {
            return Failure<PosStartFulfillmentDto>("online_orders.fulfilment_conflict", exception.Message);
        }
    }

    public async Task<ApplicationResult<PosPickingOrderDto>> GetPickingAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateCommandAsync(
            context, outletId, orderId, PickingViewPermission, cancellationToken);
        if (validation is not null)
            return ApplicationResult<PosPickingOrderDto>.Failure(validation);

        var result = await _repository.GetPickingAsync(
            context.TenantId, outletId, orderId, cancellationToken);
        return result is null
            ? Failure<PosPickingOrderDto>("online_orders.picking_not_found", "Picking has not been started for this order.")
            : ApplicationResult<PosPickingOrderDto>.Success(result);
    }

    public Task<ApplicationResult<PosFulfillmentCommandDto>> PickLineAsync(TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId, PosPickLineRequest request, CancellationToken cancellationToken)
    {
        var inputPermission = request.InputMethod.Equals("SCAN", StringComparison.OrdinalIgnoreCase) ? PickingScanPermission : PickingManualPermission;
        return Execute(context, outletId, orderId, new[] { PickingPermission, inputPermission },
            ct => _repository.PickLineAsync(context.TenantId, outletId, orderId, lineId, context.UserId, request, _clock.UtcNow, ct), cancellationToken);
    }

    public Task<ApplicationResult<PosFulfillmentCommandDto>> ReportIssueAsync(TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId, PosReportPickingIssueRequest request, CancellationToken cancellationToken) =>
        Execute(context, outletId, orderId, new[] { PickingIssuePermission }, ct => _repository.ReportIssueAsync(context.TenantId, outletId, orderId, lineId, context.UserId, request, _clock.UtcNow, ct), cancellationToken);

    public Task<ApplicationResult<PosFulfillmentCommandDto>> PackAsync(TenantRequestContext context, Guid outletId, Guid orderId, PosPackOrderRequest request, CancellationToken cancellationToken) =>
        Execute(context, outletId, orderId, new[] { PackingPermission }, ct => _repository.PackAsync(context.TenantId, outletId, orderId, context.UserId, request, _clock.UtcNow, ct), cancellationToken);

    public Task<ApplicationResult<PosFulfillmentCommandDto>> MarkReadyAsync(TenantRequestContext context, Guid outletId, Guid orderId, CancellationToken cancellationToken) =>
        Execute(context, outletId, orderId, new[] { MarkReadyPermission }, ct => _repository.MarkReadyAsync(context.TenantId, outletId, orderId, context.UserId, _clock.UtcNow, ct), cancellationToken);

    private async Task<ApplicationResult<PosFulfillmentCommandDto>> Execute(TenantRequestContext context, Guid outletId, Guid orderId, IReadOnlyCollection<string> permissions, Func<CancellationToken, Task<PosFulfillmentCommandDto?>> action, CancellationToken cancellationToken)
    {
        foreach (var permission in permissions)
        {
            var validation = await ValidateCommandAsync(context, outletId, orderId, permission, cancellationToken);
            if (validation is not null) return ApplicationResult<PosFulfillmentCommandDto>.Failure(validation);
        }
        try
        {
            var value = await action(cancellationToken);
            return value is null ? Failure<PosFulfillmentCommandDto>("online_orders.not_found", "Online order was not found.") : ApplicationResult<PosFulfillmentCommandDto>.Success(value);
        }
        catch (InvalidOperationException ex) { return Failure<PosFulfillmentCommandDto>("online_orders.fulfilment_conflict", ex.Message); }
    }

    private async Task<ApplicationError?> ValidateCommandAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        string permission,
        CancellationToken cancellationToken)
    {
        var common = Validate(context, outletId);
        if (common is not null) return common;
        if (orderId == Guid.Empty)
            return new("online_orders.invalid_order_id", "A valid order id is required.");
        if (!context.HasPermission(permission))
            return new("online_orders.permission_denied", "Permission denied for this fulfilment action.");
        var enabled = await _entitlements.IsEnabledAsync(
            context.TenantId, PlatformTenantFeatureCodes.ClickCollect, _clock.UtcNow, cancellationToken);
        if (!enabled)
            return new("online_orders.feature_not_entitled", "Click and collect is not enabled for this tenant.");
        return await _repository.CanAccessOutletAsync(
            context.TenantId, context.UserId, outletId, cancellationToken)
            ? null
            : new("online_orders.outlet_access_denied", "The requested outlet is not accessible.");
    }

    private async Task<ApplicationError?> ValidateReadAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var common = Validate(context, outletId);
        if (common is not null) return common;

        var enabled = await _entitlements.IsEnabledAsync(
            context.TenantId,
            PlatformTenantFeatureCodes.ClickCollect,
            _clock.UtcNow,
            cancellationToken);
        if (!enabled)
            return new("online_orders.feature_not_entitled", "Click and collect is not enabled for this tenant.");

        return await _repository.CanAccessOutletAsync(
            context.TenantId, context.UserId, outletId, cancellationToken)
            ? null
            : new("online_orders.outlet_access_denied", "The requested outlet is not accessible.");
    }

    private static ApplicationError? Validate(TenantRequestContext context, Guid outletId)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return new("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(AccessPermission) || !context.HasPermission(ViewPermission))
            return new("online_orders.permission_denied", "Permission denied for online orders.");
        return outletId == Guid.Empty
            ? new("online_orders.invalid_outlet", "A valid outlet is required.")
            : null;
    }

    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));
}
