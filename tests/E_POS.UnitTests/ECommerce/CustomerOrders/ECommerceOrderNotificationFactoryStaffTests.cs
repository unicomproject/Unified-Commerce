using E_POS.Application.Modules.ECommerce.CustomerOrders.Notifications;
using E_POS.Application.Modules.Shared.Notification.Constants;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class ECommerceOrderNotificationFactoryStaffTests
{
    [Fact]
    public void OrderPlacedForStaff_UsesCanonicalStaffEventCodeAndSalesOrderReference()
    {
        var tenantId = Guid.Parse("55555555-0000-4000-8000-000000000001");
        var staffId = Guid.Parse("99999999-0003-4000-8000-000000000001");
        var orderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var request = ECommerceOrderNotificationFactory.OrderPlacedForStaff(
            tenantId,
            staffId,
            orderId,
            "ORD-000001");

        Assert.Equal("ecommerce.order_placed.staff", request.EventCode);
        Assert.Equal("ECommerce", request.SourceModule);
        Assert.Equal("SALES_ORDER", request.SourceReferenceType);
        Assert.Equal(orderId, request.SourceReferenceId);
        Assert.Equal(NotificationRecipientTypes.TenantUser, request.Recipient.RecipientType);
        Assert.Equal(staffId, request.Recipient.TenantUserId);
        Assert.Equal($"ECOM-STF-{orderId:N}-{staffId:N}", request.EventNumber);
        Assert.Contains("ORD-000001", request.Content.Body, StringComparison.Ordinal);
        Assert.Equal($"/orders/{orderId:N}", request.Content.ActionUrl);
    }

    [Fact]
    public void OrderPlacedForStaff_EventNumberIsUniquePerStaffRecipient()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var staffA = Guid.NewGuid();
        var staffB = Guid.NewGuid();

        var a = ECommerceOrderNotificationFactory.OrderPlacedForStaff(tenantId, staffA, orderId, "ORD-1");
        var b = ECommerceOrderNotificationFactory.OrderPlacedForStaff(tenantId, staffB, orderId, "ORD-1");

        Assert.NotEqual(a.EventNumber, b.EventNumber);
        Assert.StartsWith("ECOM-STF-", a.EventNumber, StringComparison.Ordinal);
    }
}
