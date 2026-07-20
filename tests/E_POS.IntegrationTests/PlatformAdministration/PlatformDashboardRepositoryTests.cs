using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
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
            Tenant.Create(tenantOneId, "TEN-001", "ten-001", "Active Tenant", "active", "LKR", "Asia/Colombo", null, null, Now.AddDays(-3)),
            Tenant.Create(tenantTwoId, "TEN-002", "ten-002", "Suspended Tenant", "suspended", "LKR", "Asia/Colombo", null, null, Now.AddDays(-2)),
            Tenant.Create(tenantThreeId, "TEN-003", "ten-003", "Trial Tenant", "active", "LKR", "Asia/Colombo", null, null, Now.AddDays(-1)));

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

        var pendingInvoice = SubscriptionInvoice.CreateDraft(
            Guid.Parse("66666666-6666-4666-8666-666666666601"),
            tenantOneId,
            Guid.Parse("22222222-2222-4222-8222-222222222201"),
            "INV-001",
            100m,
            "MONTHLY",
            Now.AddDays(14),
            Now);
        pendingInvoice.Issue(Now);
        dbContext.SubscriptionInvoices.Add(pendingInvoice);

        dbContext.Outlets.Add(Outlet.Create(
            Guid.Parse("33333333-3333-4333-8333-333333333301"),
            tenantOneId,
            "Main Outlet",
            "OUT-001",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            null,
            Now));
        dbContext.Outlets.Add(Outlet.Create(
            Guid.Parse("33333333-3333-4333-8333-333333333302"),
            tenantOneId,
            "Deleted Outlet",
            "OUT-002",
            "DELETED",
            "STORE",
            "UTC",
            false,
            null,
            null,
            null,
            Now));

        dbContext.Tills.Add(Till.Create(
            Guid.Parse("44444444-4444-4444-8444-444444444401"),
            tenantOneId,
            Guid.Parse("33333333-3333-4333-8333-333333333301"),
            "Front Till",
            "Front",
            1,
            "TILL-001",
            TillConstants.StandardTillType,
            0m,
            TillConstants.DefaultCurrencyCode,
            true,
            "ACTIVE",
            null,
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            Guid.Parse("55555555-5555-4555-8555-555555555501"),
            tenantOneId,
            "user@tenant.test",
            "User Test",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "standard",
            "system",
            "default",
            Now));
        dbContext.TenantUsers.Add(TenantUser.Create(
            Guid.Parse("55555555-5555-4555-8555-555555555502"),
            tenantOneId,
            "deleted@tenant.test",
            "Deleted Test",
            null,
            null,
            "hash",
            "salt",
            "DELETED",
            "standard",
            "system",
            "default",
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
        Assert.Equal(1, dashboard.PendingBillingCount);
        Assert.Equal(1, dashboard.TotalOutlets);
        Assert.Equal(1, dashboard.TotalTills);
        Assert.Equal(1, dashboard.TotalUsers);
        Assert.Equal(3, dashboard.RecentTenants.Count);
        Assert.Equal("TEN-003", dashboard.RecentTenants[0].Code);
        Assert.Contains(dashboard.AttentionItems, x => x.Type == "suspended_tenants" && x.Count == 1);
        Assert.Contains(dashboard.AttentionItems, x => x.Type == "past_due_subscriptions" && x.Count == 1);
        Assert.Contains(dashboard.AttentionItems, x => x.Type == "pending_billing" && x.Count == 1);
        Assert.Contains(dashboard.AttentionItems, x => x.Type == "setup_pending" && x.Count == 0);
        Assert.Equal(Now, dashboard.GeneratedAt);
    }

    [Fact]
    public async Task GetDashboardAsync_AttentionCounts_DoNotSwapPastDueAndPendingBilling()
    {
        await using var dbContext = CreateDbContext();
        var pastDueTenantId = Guid.Parse("11111111-1111-4111-8111-111111111201");
        var activeTenantId = Guid.Parse("11111111-1111-4111-8111-111111111202");
        var secondPastDueTenantId = Guid.Parse("11111111-1111-4111-8111-111111111203");

        dbContext.Tenants.AddRange(
            Tenant.Create(pastDueTenantId, "TEN-PD1", "ten-pd1", "Past Due One", "active", "LKR", "Asia/Colombo", null, null, Now),
            Tenant.Create(activeTenantId, "TEN-ACT", "ten-act", "Active Billing", "active", "LKR", "Asia/Colombo", null, null, Now),
            Tenant.Create(secondPastDueTenantId, "TEN-PD2", "ten-pd2", "Past Due Two", "active", "LKR", "Asia/Colombo", null, null, Now));

        var activeSubscriptionId = Guid.Parse("22222222-2222-4222-8222-222222222302");
        dbContext.TenantSubscriptions.AddRange(
            TenantSubscription.Create(Guid.Parse("22222222-2222-4222-8222-222222222301"), pastDueTenantId, Guid.NewGuid(), "PAST_DUE", Now),
            TenantSubscription.Create(activeSubscriptionId, activeTenantId, Guid.NewGuid(), "ACTIVE", Now),
            TenantSubscription.Create(Guid.Parse("22222222-2222-4222-8222-222222222303"), secondPastDueTenantId, Guid.NewGuid(), "PAST_DUE", Now));

        var pendingInvoice = SubscriptionInvoice.CreateDraft(
            Guid.Parse("66666666-6666-4666-8666-666666666701"),
            activeTenantId,
            activeSubscriptionId,
            "INV-ATTN-001",
            50m,
            "MONTHLY",
            Now.AddDays(14),
            Now);
        pendingInvoice.Issue(Now);
        dbContext.SubscriptionInvoices.Add(pendingInvoice);
        await dbContext.SaveChangesAsync();

        IPlatformDashboardRepository repository = new PlatformDashboardRepository(dbContext);
        var dashboard = await repository.GetDashboardAsync(Now, CancellationToken.None);

        var pastDue = Assert.Single(dashboard.AttentionItems, x => x.Type == "past_due_subscriptions");
        var pendingBilling = Assert.Single(dashboard.AttentionItems, x => x.Type == "pending_billing");
        Assert.Equal(2, pastDue.Count);
        Assert.Equal(1, pendingBilling.Count);
        Assert.Equal(1, dashboard.PendingBillingCount);
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



