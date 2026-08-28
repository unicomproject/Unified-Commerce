using E_POS.Application.Common.Contracts;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Options;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.IntegrationTests.TenantAdministration;

public sealed class DevelopmentTenantRoleAccessTestAccountSeederTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SeedAsync_ActivatesFixedAccountsAndReactivatesExpectedAssignments()
    {
        await using var dbContext = CreateDbContext();
        await SeedExpectedAccountsAsync(dbContext);

        await CreateSeeder(dbContext).SeedAsync();

        var tenantAdmin = await dbContext.TenantUsers.SingleAsync(user =>
            user.Id == DevelopmentTenantSeedConstants.TenantAdminUserId);
        var cashier = await dbContext.TenantUsers.SingleAsync(user =>
            user.Id == DevelopmentTenantSeedConstants.CashierUserId);
        var tenantAdminAssignment = await dbContext.TenantUserRoles.SingleAsync(assignment =>
            assignment.TenantUserId == DevelopmentTenantSeedConstants.TenantAdminUserId &&
            assignment.TenantRoleId == DevelopmentTenantSeedConstants.TenantAdminRoleId);
        var cashierAssignment = await dbContext.TenantUserRoles.SingleAsync(assignment =>
            assignment.TenantUserId == DevelopmentTenantSeedConstants.CashierUserId &&
            assignment.TenantRoleId == DevelopmentTenantSeedConstants.CashierRoleId);

        var passwordHashService = new PasswordHashService();
        Assert.Equal(TenantUserConstants.StatusActive, tenantAdmin.AccountStatus);
        Assert.Equal(TenantUserConstants.StatusActive, cashier.AccountStatus);
        Assert.True(passwordHashService.VerifyPassword("TenantAdmin-Test-Password-1", tenantAdmin.EncryptedPassword));
        Assert.True(passwordHashService.VerifyPassword("Cashier-Test-Password-1", cashier.EncryptedPassword));
        Assert.Null(tenantAdminAssignment.RevokedAt);
        Assert.Null(cashierAssignment.RevokedAt);
    }

    [Fact]
    public async Task SeedAsync_MissingPasswordLeavesExpectedAccountUnchanged()
    {
        await using var dbContext = CreateDbContext();
        await SeedExpectedAccountsAsync(dbContext);
        var tenantAdmin = await dbContext.TenantUsers.SingleAsync(user =>
            user.Id == DevelopmentTenantSeedConstants.TenantAdminUserId);
        var originalHash = tenantAdmin.EncryptedPassword;

        await CreateSeeder(
            dbContext,
            new DevelopmentTenantRoleAccessSeedOptions()).SeedAsync();

        await dbContext.Entry(tenantAdmin).ReloadAsync();
        Assert.Equal(originalHash, tenantAdmin.EncryptedPassword);
        Assert.Equal(TenantUserConstants.StatusInactive, tenantAdmin.AccountStatus);
    }

    private static DevelopmentTenantRoleAccessTestAccountSeeder CreateSeeder(
        EPosDbContext dbContext,
        DevelopmentTenantRoleAccessSeedOptions? options = null)
    {
        options ??= new DevelopmentTenantRoleAccessSeedOptions
        {
            TenantAdmin = new DevelopmentTenantRoleAccessAccountOptions
            {
                Password = "TenantAdmin-Test-Password-1"
            },
            Cashier = new DevelopmentTenantRoleAccessAccountOptions
            {
                Password = "Cashier-Test-Password-1"
            }
        };

        return new DevelopmentTenantRoleAccessTestAccountSeeder(
            Options.Create(options),
            dbContext,
            new PasswordHashService(),
            new FixedDateTimeProvider(),
            NullLogger<DevelopmentTenantRoleAccessTestAccountSeeder>.Instance);
    }

    private static async Task SeedExpectedAccountsAsync(EPosDbContext dbContext)
    {
        var tenantId = DevelopmentTenantSeedConstants.DevelopmentTenantId;
        var passwordHashService = new PasswordHashService();
        var tenantAdmin = TenantUser.Create(
            DevelopmentTenantSeedConstants.TenantAdminUserId,
            tenantId,
            DevelopmentTenantSeedConstants.TenantAdminEmail,
            "Tenant Admin",
            null,
            null,
            passwordHashService.HashPassword("old-tenant-admin-password"),
            "test-salt",
            TenantUserConstants.StatusInactive,
            "admin",
            "admin",
            null,
            Now,
            staffCode: "TENANTADMIN001");
        var cashier = TenantUser.Create(
            DevelopmentTenantSeedConstants.CashierUserId,
            tenantId,
            DevelopmentTenantSeedConstants.CashierEmail,
            "Cashier",
            null,
            null,
            passwordHashService.HashPassword("old-cashier-password"),
            "test-salt",
            TenantUserConstants.StatusInactive,
            "outlet",
            "outlet",
            null,
            Now,
            staffCode: "CASHIER001");
        var tenantAdminRole = TenantRole.Create(
            DevelopmentTenantSeedConstants.TenantAdminRoleId,
            tenantId,
            null,
            null,
            DevelopmentTenantSeedConstants.TenantAdminRoleCode,
            "Tenant Admin",
            null,
            false,
            true,
            tenantAdmin.Id,
            Now);
        var cashierRole = TenantRole.Create(
            DevelopmentTenantSeedConstants.CashierRoleId,
            tenantId,
            null,
            null,
            DevelopmentTenantSeedConstants.CashierRoleCode,
            "Cashier",
            null,
            false,
            true,
            tenantAdmin.Id,
            Now);
        var tenantAdminAssignment = TenantUserRole.Create(
            Guid.NewGuid(), tenantId, tenantAdmin.Id, tenantAdminRole.Id, null, Now);
        var cashierAssignment = TenantUserRole.Create(
            Guid.NewGuid(), tenantId, cashier.Id, cashierRole.Id, null, Now);
        tenantAdminAssignment.Revoke(Now);
        cashierAssignment.Revoke(Now);

        dbContext.TenantUsers.AddRange(tenantAdmin, cashier);
        dbContext.TenantRoles.AddRange(tenantAdminRole, cashierRole);
        dbContext.TenantUserRoles.AddRange(tenantAdminAssignment, cashierAssignment);
        await dbContext.SaveChangesAsync();
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
