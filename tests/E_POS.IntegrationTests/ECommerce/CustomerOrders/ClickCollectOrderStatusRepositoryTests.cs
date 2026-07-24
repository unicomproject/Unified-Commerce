using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class ClickCollectOrderStatusRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateStatusAsync_PendingToAccepted_PersistsStatusAndEnablesQr()
    {
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(tenantId, Guid.NewGuid(), "SO-WEB-ACCEPT");
        dbContext.SalesOrders.Add(order);
        await dbContext.SaveChangesAsync();
        var repository = new ClickCollectOrderStatusRepository(dbContext);

        var result = await repository.UpdateStatusAsync(
            tenantId,
            tenantUserId,
            order.Id,
            "ACCEPTED",
            Now.AddMinutes(5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ACCEPTED", result.Response!.Status);
        Assert.True(result.Response.CollectionQrAvailable);
        var persisted = await dbContext.SalesOrders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal("ACCEPTED", persisted.Status);
        Assert.Equal("ACCEPTED", persisted.FulfillmentStatus);
        Assert.Equal(tenantUserId, persisted.UpdatedByTenantUserId);
        var history = await dbContext.SalesOrderStatusHistory
            .Where(x => x.SalesOrderId == order.Id)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync();
        Assert.Contains(history, x =>
            x.StatusType == "ORDER_STATUS" &&
            x.OldStatus == "CONFIRMED" &&
            x.NewStatus == "ACCEPTED" &&
            x.ChangedByTenantUserId == tenantUserId &&
            x.ChangedAt == Now.AddMinutes(5));
        Assert.Contains(history, x =>
            x.StatusType == "FULFILLMENT_STATUS" &&
            x.OldStatus == "PENDING" &&
            x.NewStatus == "ACCEPTED" &&
            x.ChangedByTenantUserId == tenantUserId &&
            x.ChangedAt == Now.AddMinutes(5));
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsConflictAndDoesNotMutate()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(tenantId, Guid.NewGuid(), "SO-WEB-SKIP");
        dbContext.SalesOrders.Add(order);
        await dbContext.SaveChangesAsync();
        var repository = new ClickCollectOrderStatusRepository(dbContext);

        var result = await repository.UpdateStatusAsync(
            tenantId,
            Guid.NewGuid(),
            order.Id,
            "READY_FOR_COLLECTION",
            Now.AddMinutes(5),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("click_collect_orders.invalid_transition", result.ErrorCode);
        var persisted = await dbContext.SalesOrders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal("CONFIRMED", persisted.Status);
        Assert.Equal("PENDING", persisted.FulfillmentStatus);
        Assert.Empty(await dbContext.SalesOrderStatusHistory.Where(x => x.SalesOrderId == order.Id).ToListAsync());
    }

    [Fact]
    public async Task UpdateStatusAsync_OrderFromAnotherTenant_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), "SO-WEB-TENANT");
        dbContext.SalesOrders.Add(order);
        await dbContext.SaveChangesAsync();
        var repository = new ClickCollectOrderStatusRepository(dbContext);

        var result = await repository.UpdateStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            order.Id,
            "ACCEPTED",
            Now.AddMinutes(5),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("click_collect_orders.not_found", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_FullWorkflow_PersistsCompletedState()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(tenantId, Guid.NewGuid(), "SO-WEB-FLOW");
        dbContext.SalesOrders.Add(order);
        await dbContext.SaveChangesAsync();
        var repository = new ClickCollectOrderStatusRepository(dbContext);

        await repository.UpdateStatusAsync(tenantId, Guid.NewGuid(), order.Id, "ACCEPTED", Now.AddMinutes(1), CancellationToken.None);
        await repository.UpdateStatusAsync(tenantId, Guid.NewGuid(), order.Id, "PREPARING", Now.AddMinutes(2), CancellationToken.None);
        await repository.UpdateStatusAsync(tenantId, Guid.NewGuid(), order.Id, "READY_FOR_COLLECTION", Now.AddMinutes(3), CancellationToken.None);
        var completed = await repository.UpdateStatusAsync(tenantId, Guid.NewGuid(), order.Id, "COMPLETED", Now.AddMinutes(4), CancellationToken.None);

        Assert.True(completed.IsSuccess);
        Assert.Equal("COMPLETED", completed.Response!.Status);
        var persisted = await dbContext.SalesOrders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal("COMPLETED", persisted.Status);
        Assert.Equal("COLLECTED", persisted.FulfillmentStatus);
        Assert.Equal(Now.AddMinutes(4), persisted.CompletedAt);
        var fulfillmentStatuses = await dbContext.SalesOrderStatusHistory
            .Where(x => x.SalesOrderId == order.Id && x.StatusType == "FULFILLMENT_STATUS")
            .OrderBy(x => x.SequenceNumber)
            .Select(x => x.NewStatus)
            .ToListAsync();
        Assert.Equal(
            ["ACCEPTED", "PREPARING", "READY_FOR_COLLECTION", "COLLECTED"],
            fulfillmentStatuses);
    }

    private static SalesOrder CreateOrder(Guid tenantId, Guid customerId, string orderNumber) =>
        SalesOrder.CreateClickAndCollect(
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
            24.99m,
            0m,
            0m,
            0m,
            24.99m,
            Now.AddDays(1),
            Now.AddDays(1).AddMinutes(30),
            "Europe/London",
            Now);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}

