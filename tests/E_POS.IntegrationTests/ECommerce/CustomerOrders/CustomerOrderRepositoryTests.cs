using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class CustomerOrderRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_ReturnsOnlyCurrentTenantAndCustomerOrdersWithThumbnail()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var ownOrder = AddOrderWithLine(dbContext, tenantId, customerId, productId, "SO-WEB-OWN", 2m);
        AddOrderWithLine(dbContext, tenantId, Guid.NewGuid(), Guid.NewGuid(), "SO-WEB-OTHER-CUSTOMER", 1m);
        AddOrderWithLine(dbContext, Guid.NewGuid(), customerId, Guid.NewGuid(), "SO-WEB-OTHER-TENANT", 1m);
        dbContext.ProductImages.Add(ProductImage.Create(
            Guid.NewGuid(), tenantId, productId, null, null,
            "jersey-main", "/images/jersey.jpg", "Jersey", "MAIN", "image/jpeg",
            null, null, null, null, 0, true, "ACTIVE", null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var result = await repository.GetAsync(
            tenantId,
            customerId,
            null,
            1,
            10,
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(ownOrder.Id, item.Id);
        Assert.Equal("SO-WEB-OWN", item.OrderNumber);
        Assert.Equal(2, item.ItemCount);
        Assert.Equal("Pending Confirmation", item.StatusLabel);
        Assert.Equal("/images/jersey.jpg", Assert.Single(item.ThumbnailUrls));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAsync_StatusFilter_ReturnsOnlyMatchingOrders()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var accepted = AddOrderWithLine(dbContext, tenantId, customerId, Guid.NewGuid(), "SO-WEB-ACCEPTED", 1m);
        accepted.UpdateClickAndCollectStatus("ACCEPTED", Guid.NewGuid(), Now.AddMinutes(5));
        AddOrderWithLine(dbContext, tenantId, customerId, Guid.NewGuid(), "SO-WEB-PENDING", 1m);
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var result = await repository.GetAsync(
            tenantId,
            customerId,
            "ACCEPTED",
            1,
            10,
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(accepted.Id, item.Id);
        Assert.Equal("ACCEPTED", item.Status);
    }

    [Fact]
    public async Task GetDetailAsync_PendingOrder_HidesCollectionQrAndMapsItemsTotals()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = AddOrderWithLine(dbContext, tenantId, customerId, productId, "SO-WEB-PENDING", 2m);
        dbContext.ProductImages.Add(ProductImage.Create(
            Guid.NewGuid(), tenantId, productId, null, null,
            "jersey-main", "/images/jersey.jpg", "Jersey", "MAIN", "image/jpeg",
            null, null, null, null, 0, true, "ACTIVE", null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var detail = await repository.GetDetailAsync(
            tenantId,
            customerId,
            order.Id,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("PENDING_CONFIRMATION", detail!.Status);
        Assert.False(detail.CanShowCollectionQr);
        Assert.Null(detail.CollectionQr);
        Assert.Equal("Pay at Pickup", detail.PaymentLabel);
        Assert.Equal(2, detail.ItemCount);
        Assert.Equal(49.98m, detail.GrandTotal);
        Assert.Contains(detail.TimelineSteps, x => x.Code == "ORDER_CONFIRMED" && x.State == "CURRENT");
        Assert.Contains(detail.TimelineSteps, x => x.Code == "PREPARING" && x.State == "PENDING");
        Assert.Contains("TRACK", detail.AvailableActions);
        Assert.Contains("CANCEL", detail.AvailableActions);
        Assert.Contains("NEED_HELP", detail.AvailableActions);
        var line = Assert.Single(detail.Items);
        Assert.Equal("Arena Jersey", line.ProductName);
        Assert.Equal("/images/jersey.jpg", line.ImageUrl);
    }

    [Fact]
    public async Task GetDetailAsync_AcceptedOrder_ReturnsCollectionQr()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = AddOrderWithLine(dbContext, tenantId, customerId, Guid.NewGuid(), "SO-WEB-ACCEPTED", 1m);
        order.UpdateClickAndCollectStatus("ACCEPTED", Guid.NewGuid(), Now.AddMinutes(5));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var detail = await repository.GetDetailAsync(
            tenantId,
            customerId,
            order.Id,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("ACCEPTED", detail!.Status);
        Assert.True(detail.CanShowCollectionQr);
        Assert.Contains(order.Id.ToString("N"), detail.CollectionQr);
        Assert.Contains(detail.TimelineSteps, x => x.Code == "ORDER_CONFIRMED" && x.State == "COMPLETED");
        Assert.Contains(detail.TimelineSteps, x => x.Code == "PREPARING" && x.State == "CURRENT");
        Assert.Contains("TRACK", detail.AvailableActions);
        Assert.Contains("CANCEL", detail.AvailableActions);
        Assert.Contains("NEED_HELP", detail.AvailableActions);
    }

    [Fact]
    public async Task CancelAsync_PendingOrder_PersistsCancelledStateAndStatusHistory()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = AddOrderWithLine(dbContext, tenantId, customerId, Guid.NewGuid(), "SO-WEB-CANCEL", 1m);
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var result = await repository.CancelAsync(
            tenantId,
            customerId,
            order.Id,
            "Changed my mind",
            Now.AddMinutes(10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CANCELLED", result.Response!.Status);
        Assert.Equal(Now.AddMinutes(10), result.Response.CancelledAt);
        var persisted = await dbContext.SalesOrders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal("CANCELLED", persisted.Status);
        Assert.Equal("CANCELLED", persisted.FulfillmentStatus);
        Assert.Equal(Now.AddMinutes(10), persisted.CancelledAt);
        Assert.Equal("Changed my mind", persisted.CancellationReason);

        var history = await dbContext.SalesOrderStatusHistory
            .Where(x => x.SalesOrderId == order.Id)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync();
        Assert.Contains(history, x =>
            x.StatusType == "ORDER_STATUS" &&
            x.OldStatus == "CONFIRMED" &&
            x.NewStatus == "CANCELLED" &&
            x.ChangedByTenantUserId == null);
        Assert.Contains(history, x =>
            x.StatusType == "FULFILLMENT_STATUS" &&
            x.OldStatus == "PENDING" &&
            x.NewStatus == "CANCELLED" &&
            x.ChangedByTenantUserId == null);

        var detail = await repository.GetDetailAsync(tenantId, customerId, order.Id, CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal("CANCELLED", detail!.Status);
        Assert.False(detail.CanShowCollectionQr);
        Assert.DoesNotContain("TRACK", detail.AvailableActions);
        Assert.DoesNotContain("CANCEL", detail.AvailableActions);
        Assert.Contains("NEED_HELP", detail.AvailableActions);
    }

    [Fact]
    public async Task CancelAsync_PreparingOrder_ReturnsConflictAndDoesNotMutate()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = AddOrderWithLine(dbContext, tenantId, customerId, Guid.NewGuid(), "SO-WEB-PREPARING", 1m);
        order.UpdateClickAndCollectStatus("ACCEPTED", Guid.NewGuid(), Now.AddMinutes(1));
        order.UpdateClickAndCollectStatus("PREPARING", Guid.NewGuid(), Now.AddMinutes(2));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var result = await repository.CancelAsync(
            tenantId,
            customerId,
            order.Id,
            "Too late",
            Now.AddMinutes(10),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_orders.invalid_transition", result.ErrorCode);
        var persisted = await dbContext.SalesOrders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal("ACCEPTED", persisted.Status);
        Assert.Equal("PREPARING", persisted.FulfillmentStatus);
        Assert.Null(persisted.CancelledAt);
        Assert.Empty(await dbContext.SalesOrderStatusHistory.Where(x => x.SalesOrderId == order.Id).ToListAsync());
    }

    [Fact]
    public async Task GetDetailAsync_CrossCustomerOrder_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = AddOrderWithLine(dbContext, tenantId, Guid.NewGuid(), Guid.NewGuid(), "SO-WEB-OTHER", 1m);
        await dbContext.SaveChangesAsync();
        var repository = new CustomerOrderRepository(dbContext);

        var detail = await repository.GetDetailAsync(
            tenantId,
            Guid.NewGuid(),
            order.Id,
            CancellationToken.None);

        Assert.Null(detail);
    }

    private static SalesOrder AddOrderWithLine(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid customerId,
        Guid productId,
        string orderNumber,
        decimal quantity)
    {
        var unitPrice = 24.99m;
        var lineSubtotal = unitPrice * quantity;
        var order = SalesOrder.CreateClickAndCollect(
            Guid.NewGuid(),
            tenantId,
            orderNumber,
            $"idem-{orderNumber}",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CLICK_COLLECT",
            Guid.NewGuid(),
            "OUT-1",
            "Etihad Stadium Store",
            customerId,
            "Test Customer",
            "customer@example.com",
            null,
            "GBP",
            false,
            lineSubtotal,
            0m,
            0m,
            0m,
            lineSubtotal,
            Now.AddDays(1),
            Now.AddDays(1).AddMinutes(30),
            "Europe/London",
            Now);
        var line = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(),
            tenantId,
            order.Id,
            1,
            productId,
            null,
            Guid.NewGuid(),
            "SKU-001",
            "Arena Jersey",
            null,
            "EA",
            "Each",
            "STANDARD",
            "SIMPLE",
            quantity,
            unitPrice,
            lineSubtotal,
            0m,
            0m,
            false,
            Now);

        dbContext.SalesOrders.Add(order);
        dbContext.SalesOrderLines.Add(line);
        return order;
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}

