using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Notifications;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderReadyService(
    IPosOnlineOrderReadyRepository repository,
    INotificationService notifications,
    ITenantFeatureEntitlementEvaluator entitlements,
    IDateTimeProvider clock) : IPosOnlineOrderReadyService
{
    public async Task<ApplicationResult<NotificationCreateResult>> NotifyAsync(
        TenantRequestContext context, Guid outletId, Guid orderId, CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return Failure("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(OnlineOrderPickingPermissions.OrdersAccess) ||
            !context.HasPermission(OnlineOrderPickingPermissions.OrdersView) ||
            !context.HasPermission(OnlineOrderPickingPermissions.CollectionViewReady) ||
            !context.HasPermission(OnlineOrderPickingPermissions.CollectionNotifyCustomer))
            return Failure("online_orders.permission_denied", "Permission denied for ready notification.");
        if (outletId == Guid.Empty || orderId == Guid.Empty)
            return Failure("online_orders.invalid_request", "A valid outlet and order are required.");
        var entitlement = await entitlements.EvaluateAsync(
            context.TenantId, PlatformTenantFeatureCodes.ClickCollect, clock.UtcNow, cancellationToken);
        if (!entitlement.IsAllowed)
            return Failure("online_orders.feature_not_entitled", "Click & collect is not enabled for this tenant.");

        return await repository.ExecuteNotificationAsync(
            context.TenantId, context.UserId, outletId, orderId,
            async (order, fulfillment, pickup, ct) =>
            {
                if (!ReadyForCollectionPolicy.IsReady(order, fulfillment, pickup))
                    return Failure("online_orders.invalid_state", "The order is no longer ready for collection.");
                if (order.CustomerId is null || order.CustomerId == Guid.Empty)
                    return Failure("online_orders.notification_recipient_unavailable", "A customer notification destination is unavailable.");
                var request = ECommerceOrderNotificationFactory.OrderStatusChanged(
                    context.TenantId, order.CustomerId!.Value, order.Id, order.OrderNumber,
                    "READY_FOR_COLLECTION", context.UserId);
                var result = await notifications.CreateAsync(request, ct);
                return result.IsSuccess
                    ? result
                    : Failure("online_orders.notification_failed", "Customer notification could not be completed. Retry safely.");
            }, cancellationToken);
    }

    private static ApplicationResult<NotificationCreateResult> Failure(string code, string message) =>
        ApplicationResult<NotificationCreateResult>.Failure(new(code, message));
}
