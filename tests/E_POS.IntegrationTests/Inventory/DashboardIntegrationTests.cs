using E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.Dashboard;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.Inventory;

public sealed class DashboardIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task GetDashboardMetricsAsync_ReturnsZero_WhenNoDataExists()
    {
        if (!await CanConnectAsync()) return;

        await using var db = CreateDb();
        var repository = new DashboardRepository(db);
        var tenantId = Guid.NewGuid();

        var result = await repository.GetDashboardMetricsAsync(tenantId, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.LowStockCount);
        Assert.Equal(0, result.OutOfStockCount);
        Assert.Equal(0, result.NearExpiryCount);
    }

    [Fact]
    public async Task UserHasOutletAccessAsync_ReturnsFalse_WhenOutletDoesNotExist()
    {
        if (!await CanConnectAsync()) return;

        await using var db = CreateDb();
        var repository = new DashboardRepository(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();

        var result = await repository.UserHasOutletAccessAsync(tenantId, userId, outletId, CancellationToken.None);

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
