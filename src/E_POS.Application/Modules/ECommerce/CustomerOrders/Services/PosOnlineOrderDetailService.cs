using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderDetailService : IPosOnlineOrderDetailService
{
    public const string AccessPermission = "commerce.online_order.orders.access";
    public const string ViewPermission = "commerce.online_order.orders.view";

    private readonly IPosOnlineOrderDetailRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;

    public PosOnlineOrderDetailService(
        IPosOnlineOrderDetailRepository repository,
        ITenantFeatureEntitlementEvaluator entitlements,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosOnlineOrderListResponse>> ListAsync(
        TenantRequestContext context,
        PosOnlineOrderListQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateReadAsync(context, query.OutletId, cancellationToken);
        if (validation is not null)
            return ApplicationResult<PosOnlineOrderListResponse>.Failure(validation);

        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return ListFailure("online_orders.invalid_pagination",
                "Page must be positive and pageSize must be between 1 and 100.");

        var now = _clock.UtcNow;
        var result = await _repository.ListAsync(
            context.TenantId, context.UserId, query, now, cancellationToken);

        return result.IsSuccess
            ? ApplicationResult<PosOnlineOrderListResponse>.Success(result.List!)
            : ListFailure(result.ErrorCode!, MapError(result.ErrorCode!));
    }

    public async Task<ApplicationResult<PosOnlineOrderDetailResponse>> GetAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateReadAsync(context, outletId, cancellationToken);
        if (validation is not null)
            return ApplicationResult<PosOnlineOrderDetailResponse>.Failure(validation);

        if (orderId == Guid.Empty)
            return Failure("online_orders.invalid_order_id", "A valid order id is required.");

        var now = _clock.UtcNow;

        var result = await _repository.GetAsync(
            context.TenantId,
            context.UserId,
            outletId,
            orderId,
            now,
            cancellationToken);

        return result.IsSuccess
            ? ApplicationResult<PosOnlineOrderDetailResponse>.Success(result.Detail!)
            : Failure(result.ErrorCode!, MapError(result.ErrorCode!));
    }

    private async Task<ApplicationError?> ValidateReadAsync(
        TenantRequestContext context,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return new("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(AccessPermission) || !context.HasPermission(ViewPermission))
            return new("online_orders.permission_denied", "Permission denied for online orders.");
        if (outletId == Guid.Empty)
            return new("online_orders.invalid_outlet", "A valid outlet id is required.");

        var entitlement = await _entitlements.EvaluateAsync(
            context.TenantId, PlatformTenantFeatureCodes.ClickCollect,
            _clock.UtcNow, cancellationToken);
        return entitlement.IsAllowed
            ? null
            : new("online_orders.feature_not_entitled", "Click & collect is not enabled for this tenant.");
    }

    private static string MapError(string code) => code switch
    {
        "online_orders.outlet_access_denied" => "You do not have access to this outlet.",
        "online_orders.not_found" => "Online order was not found.",
        _ => "Online order details could not be loaded."
    };

    private static ApplicationResult<PosOnlineOrderDetailResponse> Failure(string code, string message) =>
        ApplicationResult<PosOnlineOrderDetailResponse>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<PosOnlineOrderListResponse> ListFailure(string code, string message) =>
        ApplicationResult<PosOnlineOrderListResponse>.Failure(new ApplicationError(code, message));
}
