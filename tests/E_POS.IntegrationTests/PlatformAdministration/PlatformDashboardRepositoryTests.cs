using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformDashboardRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetDashboardAsync_WithSeededData_ReturnsRepositoryCounts()
    {
        await using var dbContext = CreateDbContext();
        var tenantOneId = Guid.Parse("11111111-1111-4111-8111-111111111101");
        var tenantTwoId = Guid.Parse("11111111-1111-4111-8111-111111111102");
        var tenantThreeId = Guid.Parse("11111111-1111-4111-8111-111111111103");

        dbContext.Tenants.AddRange(
            Tenant.Create(tenantOneId, "TEN-001", "Active Tenant", "active", "paid", Now.AddDays(-3)),
            Tenant.Create(tenantTwoId, "TEN-002", "Suspended Tenant", "suspended", "overdue", Now.AddDays(-2)),
            Tenant.Create(tenantThreeId, "TEN-003", "Trial Tenant", "active", "pending", Now.AddDays(-1)));

        dbContext.TenantSubscriptions.AddRange(
            TenantSubscription.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222201"),
                tenantOneId,
                Guid.NewGuid(),
                "ACTIVE",
                Now),
            TenantSubscription.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222202"),
                tenantThreeId,
                Guid.NewGuid(),
                "TRIAL",
                Now),
            TenantSubscription.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222203"),
                tenantTwoId,
                Guid.NewGuid(),
                "PAST_DUE",
                Now));

        dbContext.Outlets.Add(Outlet.Create(
            Guid.Parse("33333333-3333-4333-8333-333333333301"),
            tenantOneId,
            "OUT-001",
            "Main Outlet",
            "ACTIVE",
            Now));
        dbContext.Outlets.Add(Outlet.Create(
            Guid.Parse("33333333-3333-4333-8333-333333333302"),
            tenantOneId,
            "OUT-002",
            "Deleted Outlet",
            "DELETED",
            Now));

        dbContext.Tills.Add(Till.Create(
            Guid.Parse("44444444-4444-4444-8444-444444444401"),
            tenantOneId,
            "TILL-001",
            "Front Till",
            "ACTIVE",
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            Guid.Parse("55555555-5555-4555-8555-555555555501"),
            tenantOneId,
            "user@tenant.test",
            null,
            "hash",
            "ACTIVE",
            Now));
        dbContext.TenantUsers.Add(TenantUser.Create(
            Guid.Parse("55555555-5555-4555-8555-555555555502"),
            tenantOneId,
            "deleted@tenant.test",
            null,
            "hash",
            "DELETED",
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformDashboardRepository repository = new PlatformDashboardRepository(dbContext);

        var dashboard = await repository.GetDashboardAsync(Now, CancellationToken.None);

        Assert.Equal(3, dashboard.TotalTenants);
        Assert.Equal(2, dashboard.ActiveTenants);
        Assert.Equal(1, dashboard.SuspendedTenants);
        Assert.Equal(1, dashboard.TrialTenants);
        Assert.Equal(3, dashboard.TotalSubscriptions);
        Assert.Equal(1, dashboard.ActiveSubscriptions);
        Assert.Equal(2, dashboard.PendingBillingCount);
        Assert.Equal(1, dashboard.TotalOutlets);
        Assert.Equal(1, dashboard.TotalTills);
        Assert.Equal(1, dashboard.TotalUsers);
        Assert.Equal(3, dashboard.RecentTenants.Count);
        Assert.Equal("TEN-003", dashboard.RecentTenants[0].Code);
        Assert.Contains(dashboard.AttentionItems, x => x.Type == "suspended_tenants" && x.Count == 1);
        Assert.Contains(dashboard.AttentionItems, x => x.Type == "past_due_subscriptions" && x.Count == 1);
        Assert.Equal(Now, dashboard.GeneratedAt);
    }

    [Fact]
    public async Task GetDashboardAsync_WithEmptyDatabase_ReturnsZeroCountsAndEmptyLists()
    {
        await using var dbContext = CreateDbContext();
        IPlatformDashboardRepository repository = new PlatformDashboardRepository(dbContext);

        var dashboard = await repository.GetDashboardAsync(Now, CancellationToken.None);

        Assert.Equal(0, dashboard.TotalTenants);
        Assert.Equal(0, dashboard.ActiveTenants);
        Assert.Equal(0, dashboard.SuspendedTenants);
        Assert.Equal(0, dashboard.TrialTenants);
        Assert.Equal(0, dashboard.TotalSubscriptions);
        Assert.Equal(0, dashboard.ActiveSubscriptions);
        Assert.Equal(0, dashboard.PendingBillingCount);
        Assert.Equal(0, dashboard.TotalOutlets);
        Assert.Equal(0, dashboard.TotalTills);
        Assert.Equal(0, dashboard.TotalUsers);
        Assert.Empty(dashboard.RecentTenants);
        Assert.All(dashboard.AttentionItems, item => Assert.Equal(0, item.Count));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
