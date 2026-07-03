using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformPermissionRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActivePermissionCodesAsync_ForSuperAdministratorRole_ReturnsAllThirtySixCodes()
    {
        await using var dbContext = CreateDbContext();
        var platformUserId = Guid.NewGuid();

        dbContext.PlatformUsers.Add(PlatformUser.Create(
            platformUserId,
            "super-admin@tmepos.test",
            "hash",
            PlatformAuthConstants.ActiveStatus,
            Now));

        await dbContext.SaveChangesAsync();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var superAdminRole = await dbContext.PlatformRoles
            .SingleAsync(x => x.RoleCode == PlatformRoleCodes.SuperAdministrator);

        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.Parse("66000000-0000-0000-0000-000000000099"),
            platformUserId,
            superAdminRole.Id,
            "Integration test super administrator assignment.",
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformPermissionRepository repository = new PlatformPermissionRepository(dbContext);

        var permissionCodes = await repository.GetActivePermissionCodesAsync(platformUserId, CancellationToken.None);

        Assert.Equal(36, permissionCodes.Count);
        Assert.Equal(
            PlatformPermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal),
            permissionCodes.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public async Task PlatformPermissionChecker_ForSuperAdministratorRole_AllowsAndDeniesExpectedPermissions()
    {
        await using var dbContext = CreateDbContext();
        var platformUserId = Guid.NewGuid();

        dbContext.PlatformUsers.Add(PlatformUser.Create(
            platformUserId,
            "checker-admin@tmepos.test",
            "hash",
            PlatformAuthConstants.ActiveStatus,
            Now));

        await dbContext.SaveChangesAsync();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var superAdminRole = await dbContext.PlatformRoles
            .SingleAsync(x => x.RoleCode == PlatformRoleCodes.SuperAdministrator);

        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.Parse("66000000-0000-0000-0000-000000000098"),
            platformUserId,
            superAdminRole.Id,
            "Integration test super administrator assignment.",
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformPermissionChecker checker = new PlatformPermissionChecker(new PlatformPermissionRepository(dbContext));

        Assert.True(await checker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.SubscriptionPlansCreate,
            CancellationToken.None));

        Assert.True(await checker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.RolePermissionsUpdate,
            CancellationToken.None));

        Assert.False(await checker.HasPermissionAsync(
            platformUserId,
            "platform.unknown.permission",
            CancellationToken.None));
    }

    [Fact]
    public async Task GetActivePermissionCodesAsync_WithDirectUserPermissionOnly_ReturnsDirectPermission()
    {
        await using var dbContext = CreateDbContext();
        var platformUserId = Guid.NewGuid();
        var permissionId = Guid.Parse("62000000-0000-0000-0000-000000000099");

        dbContext.PlatformUsers.Add(PlatformUser.Create(
            platformUserId,
            "direct-permission@tmepos.test",
            "hash",
            PlatformAuthConstants.ActiveStatus,
            Now));

        dbContext.PlatformPermissions.Add(PlatformPermission.Create(
            permissionId,
            PlatformPermissionCodes.TenantsView,
            "View Tenants",
            "Direct permission test.",
            PlatformAuthConstants.ActiveStatus,
            Now));

        dbContext.PlatformUserPermissions.Add(PlatformUserPermission.Create(
            Guid.NewGuid(),
            platformUserId,
            permissionId,
            "Direct permission assignment.",
            Now));

        await dbContext.SaveChangesAsync();

        var repository = new PlatformPermissionRepository(dbContext);

        var permissionCodes = await repository.GetActivePermissionCodesAsync(platformUserId, CancellationToken.None);

        Assert.Single(permissionCodes);
        Assert.Contains(PlatformPermissionCodes.TenantsView, permissionCodes);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
