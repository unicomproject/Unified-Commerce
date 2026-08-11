using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;
using E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.CurrentStock;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.Inventory;

public sealed class StockInIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task OutletExistsAsync_ReturnsFalse_WhenOutletDoesNotExist()
    {
        if (!await CanConnectAsync()) return;

        await using var db = CreateDb();
        var repository = new CurrentStockRepository(db);
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();

        var result = await repository.OutletExistsAsync(tenantId, outletId, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IdempotencyKeyExistsAsync_ReturnsFalse_ForNewKey()
    {
        if (!await CanConnectAsync()) return;

        await using var db = CreateDb();
        var repository = new CurrentStockRepository(db);
        var tenantId = Guid.NewGuid();

        var result = await repository.IdempotencyKeyExistsAsync(tenantId, Guid.NewGuid().ToString(), CancellationToken.None);

        Assert.False(result);
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
