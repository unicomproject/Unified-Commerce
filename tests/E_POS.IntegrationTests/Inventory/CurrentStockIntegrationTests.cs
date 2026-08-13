using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.CurrentStock;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.Inventory;

public sealed class CurrentStockIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task GetCurrentStockAsync_ReturnsEmptyList_WhenNoStockExists()
    {
        if (!await CanConnectAsync()) return;

        await using var db = CreateDb();
        var repository = new CurrentStockRepository(db);
        var tenantId = Guid.NewGuid();

        var result = await repository.GetCurrentStockAsync(tenantId, new CurrentStockQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCurrentStockSummaryAsync_ReturnsZeros_WhenNoStockExists()
    {
        if (!await CanConnectAsync()) return;

        await using var db = CreateDb();
        var repository = new CurrentStockRepository(db);
        var tenantId = Guid.NewGuid();

        var result = await repository.GetCurrentStockSummaryAsync(tenantId, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsInStock);
        Assert.Equal(0, result.TotalItemsLowStock);
        Assert.Equal(0, result.TotalItemsOutOfStock);
    }

    private static EPosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new EPosDbContext(options);
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var db = CreateDb();
            return await db.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
