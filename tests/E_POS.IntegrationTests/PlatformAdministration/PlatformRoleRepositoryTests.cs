using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformRoleRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetRolesAsync_ReadsSeededSuperAdministratorRole()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        IPlatformRoleRepository repository = new PlatformRoleRepository(dbContext);

        var roles = await repository.GetRolesAsync(CancellationToken.None);

        Assert.NotEmpty(roles.Roles);
        var superAdmin = roles.Roles.Single(role =>
            role.RoleCode == PlatformRoleCodes.SuperAdministrator);

        Assert.Equal(PlatformAdminSeedConstants.SuperAdministratorRoleId, superAdmin.Id);
        Assert.Equal(36, superAdmin.PermissionCount);
        Assert.True(superAdmin.IsProtected);
        Assert.True(superAdmin.IsSystem);
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_UpdatesPermissionCountForCustomRole()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var roleId = Guid.NewGuid();
        dbContext.PlatformRoles.Add(E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformRole.Create(
            roleId,
            "integration_test_role",
            "Integration Test Role",
            "Integration test role.",
            PlatformAuthConstants.ActiveStatus,
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformRoleRepository repository = new PlatformRoleRepository(dbContext);
        var permissionMap = await repository.GetActiveBusinessPermissionIdMapAsync(CancellationToken.None);

        await repository.ReplaceRolePermissionsAsync(
            roleId,
            [permissionMap[PlatformPermissionCodes.DashboardView]],
            Now,
            CancellationToken.None);

        var role = await repository.GetRoleByIdAsync(roleId, CancellationToken.None);

        Assert.NotNull(role);
        Assert.Equal(1, role!.PermissionCount);

        var permissions = await repository.GetRolePermissionsAsync([], roleId, CancellationToken.None);

        Assert.NotNull(permissions);
        Assert.Equal([PlatformPermissionCodes.DashboardView], permissions!.AssignedPermissionCodes);
    }

    [Fact]
    public async Task GetActiveBusinessPermissionIdMapAsync_ReturnsThirtySixBusinessPermissions()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        IPlatformRoleRepository repository = new PlatformRoleRepository(dbContext);

        var permissionMap = await repository.GetActiveBusinessPermissionIdMapAsync(CancellationToken.None);

        Assert.Equal(36, permissionMap.Count);
        Assert.Equal(
            PlatformPermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal),
            permissionMap.Keys.OrderBy(x => x, StringComparer.Ordinal));
        Assert.False(permissionMap.ContainsKey(PlatformBootstrapPermissionCodes.AdminAccess));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}



