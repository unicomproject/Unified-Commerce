using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Notifications;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Services;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class ClickCollectOrderStatusService : IClickCollectOrderStatusService
{
    private const string ManageFulfillmentOrdersPermission = "fulfillment.orders.manage";

    private readonly IClickCollectOrderStatusRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ClickCollectOrderStatusService(
        IClickCollectOrderStatusRepository repository,
        IDateTimeProvider dateTimeProvider)
        : this(repository, NoopNotificationService.Instance, dateTimeProvider)
    {
    }
    public ClickCollectOrderStatusService(
        IClickCollectOrderStatusRepository repository,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _notificationService = notificationService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<ClickCollectOrderStatusUpdateResponse>> UpdateStatusAsync(
        TenantRequestContext context,
        Guid orderId,
        ClickCollectOrderStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return Failure("click_collect_orders.invalid_tenant_context", "Invalid tenant context.");
        }

        if (!context.HasPermission(ManageFulfillmentOrdersPermission))
        {
            return Failure("click_collect_orders.permission_denied", "Permission denied for click & collect order management.");
        }

        if (orderId == Guid.Empty)
        {
            return Failure("click_collect_orders.invalid_order_id", "A valid order id is required.");
        }

        if (!TryNormalizeStatus(request.Status, out var targetStatus))
        {
            return Failure("click_collect_orders.invalid_status", "Invalid target status.");
        }

        var result = await _repository.UpdateStatusAsync(
            context.TenantId,
            context.UserId,
            orderId,
            targetStatus,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Failure(result.ErrorCode!, result.ErrorMessage ?? MapErrorMessage(result.ErrorCode!));
        }

        if (result.NotificationContext is not null)
        {
            await _notificationService.CreateAsync(
                ECommerceOrderNotificationFactory.OrderStatusChanged(
                    result.NotificationContext.TenantId,
                    result.NotificationContext.CustomerId,
                    result.NotificationContext.OrderId,
                    result.NotificationContext.OrderNumber,
                    result.Response!.Status,
                    context.UserId),
                cancellationToken);
        }

        return ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Success(result.Response!);
    }

    private static bool TryNormalizeStatus(string? status, out string targetStatus)
    {
        targetStatus = string.Empty;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        targetStatus = status.Trim().Replace('-', '_').ToUpperInvariant() switch
        {
            "ACCEPTED" => "ACCEPTED",
            "PREPARING" => "PREPARING",
            "READY" or "READY_FOR_COLLECTION" => "READY_FOR_COLLECTION",
            "COMPLETED" => "COMPLETED",
            _ => string.Empty
        };

        return targetStatus.Length > 0;
    }

    private static string MapErrorMessage(string code) => code switch
    {
        "click_collect_orders.not_found" => "Order was not found.",
        "click_collect_orders.invalid_transition" => "Order status cannot be changed to the requested status.",
        _ => "The click & collect order status could not be updated."
    };

    private static ApplicationResult<ClickCollectOrderStatusUpdateResponse> Failure(
        string code,
        string message) =>
        ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Failure(new ApplicationError(code, message));
}
