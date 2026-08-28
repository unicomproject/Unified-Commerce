using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.FulfilmentPickup;

public sealed class PosOnlineOrderOutletAccessRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Active_same_tenant_user_without_active_scope_uses_existing_tenant_wide_fallback()
    {
        await using var db = CreateDb();
        var fixture = SeedCore(db, TenantUserConstants.StatusActive, "ACTIVE");
        await db.SaveChangesAsync();

        var result = await new PosOnlineOrderRepository(db).CanAccessOutletAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Active_assignment_for_another_outlet_denies_requested_outlet()
    {
        await using var db = CreateDb();
        var fixture = SeedCore(db, TenantUserConstants.StatusActive, "ACTIVE");
        db.OutletUserRoles.Add(OutletUserRole.Create(
            Guid.NewGuid(), fixture.TenantId, Guid.NewGuid(), fixture.UserId,
            Guid.NewGuid(), fixture.UserId, Now));
        await db.SaveChangesAsync();

        var result = await new PosOnlineOrderRepository(db).CanAccessOutletAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Revoked_assignment_is_history_and_does_not_disable_fallback()
    {
        await using var db = CreateDb();
        var fixture = SeedCore(db, TenantUserConstants.StatusActive, "ACTIVE");
        var assignment = OutletUserRole.Create(
            Guid.NewGuid(), fixture.TenantId, Guid.NewGuid(), fixture.UserId,
            Guid.NewGuid(), fixture.UserId, Now);
        assignment.Revoke(fixture.UserId, Now.AddMinutes(1));
        db.OutletUserRoles.Add(assignment);
        await db.SaveChangesAsync();

        var result = await new PosOnlineOrderRepository(db).CanAccessOutletAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Inactive_tenant_user_is_denied()
    {
        await using var db = CreateDb();
        var fixture = SeedCore(db, TenantUserConstants.StatusInactive, "ACTIVE");
        await db.SaveChangesAsync();

        var result = await new PosOnlineOrderRepository(db).CanAccessOutletAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, CancellationToken.None);

        Assert.False(result);
    }

    private static (Guid TenantId, Guid UserId, Guid OutletId) SeedCore(
        EPosDbContext db,
        string userStatus,
        string outletStatus)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(
            tenantId, "TEST", "test", "Test", "ACTIVE", "LKR", "Asia/Colombo",
            null, null, Now));
        db.TenantUsers.Add(TenantUser.Create(
            userId, tenantId, "cashier@example.test", "Cashier", null, null,
            "hash", "salt", userStatus, "cashier", "outlet", null, Now,
            staffCode: "CASHIER-1"));
        db.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Outlet", "OUT-1", outletStatus, "STORE",
            "Asia/Colombo", true, null, null, userId, Now));
        return (tenantId, userId, outletId);
    }

    private static EPosDbContext CreateDb() => new(
        new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
}
