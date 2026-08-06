using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Notifications;

public static class ECommerceOrderNotificationFactory
{
    private const string SourceModule = "ECommerce";
    private const string SourceReferenceType = "SALES_ORDER";

    public static CreateNotificationEventRequest OrderPlaced(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        string orderNumber)
    {
        return Create(
            tenantId,
            customerId,
            orderId,
            orderNumber,
            "ecommerce.order_placed",
            "E-commerce order placed",
            "Order placed",
            $"Your order {orderNumber} has been placed and is waiting for outlet acceptance.",
            "PLACED");
    }

    public static CreateNotificationEventRequest OrderStatusChanged(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        string orderNumber,
        string status,
        Guid? changedByTenantUserId = null)
    {
        var normalizedStatus = status.Trim().ToUpperInvariant();
        var descriptor = normalizedStatus switch
        {
            "ACCEPTED" => new EventDescriptor(
                "ecommerce.order_accepted",
                "E-commerce order accepted",
                "Order accepted",
                $"Your order {orderNumber} has been accepted by the outlet.",
                "ACCEPTED"),
            "PREPARING" => new EventDescriptor(
                "ecommerce.order_preparing",
                "E-commerce order preparing",
                "Order is being prepared",
                $"Your order {orderNumber} is being prepared by the outlet.",
                "PREPARING"),
            "READY_FOR_COLLECTION" => new EventDescriptor(
                "ecommerce.order_ready_for_collection",
                "E-commerce order ready for collection",
                "Order ready for collection",
                $"Your order {orderNumber} is ready for collection. Open the order to view your collection QR code.",
                "READY"),
            "COMPLETED" => new EventDescriptor(
                "ecommerce.order_completed",
                "E-commerce order completed",
                "Order completed",
                $"Your order {orderNumber} has been completed.",
                "COMPLETED"),
            "CANCELLED" => new EventDescriptor(
                "ecommerce.order_cancelled",
                "E-commerce order cancelled",
                "Order cancelled",
                $"Your order {orderNumber} has been cancelled.",
                "CANCELLED"),
            _ => new EventDescriptor(
                "ecommerce.order_updated",
                "E-commerce order updated",
                "Order updated",
                $"Your order {orderNumber} has been updated.",
                "UPDATED")
        };

        return Create(
            tenantId,
            customerId,
            orderId,
            orderNumber,
            descriptor.EventCode,
            descriptor.EventName,
            descriptor.Title,
            descriptor.Body,
            descriptor.EventNumberSuffix,
            changedByTenantUserId);
    }

    private static CreateNotificationEventRequest Create(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        string orderNumber,
        string eventCode,
        string eventName,
        string title,
        string body,
        string eventNumberSuffix,
        Guid? createdByTenantUserId = null)
    {
        return new CreateNotificationEventRequest
        {
            TenantId = tenantId,
            EventCode = eventCode,
            EventName = eventName,
            SourceModule = SourceModule,
            SourceReferenceType = SourceReferenceType,
            SourceReferenceId = orderId,
            EventNumber = $"ECOM-ORDER-{eventNumberSuffix}-{orderId:N}",
            Priority = NotificationPriorities.Normal,
            Recipient = new NotificationRecipientDto
            {
                RecipientType = NotificationRecipientTypes.Customer,
                CustomerId = customerId
            },
            Content = new NotificationContentDto
            {
                Title = title,
                Body = body,
                ActionUrl = $"/orders/{orderId:N}"
            },
            CreatedByTenantUserId = createdByTenantUserId
        };
    }

    private sealed record EventDescriptor(
        string EventCode,
        string EventName,
        string Title,
        string Body,
        string EventNumberSuffix);
}