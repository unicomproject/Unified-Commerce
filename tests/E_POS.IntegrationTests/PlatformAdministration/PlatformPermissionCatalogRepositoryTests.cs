using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformPermissionCatalogRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActiveBusinessPermissionsAsync_ReadsSeededPermissionsFromDatabase()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        IPlatformPermissionCatalogRepository repository = new PlatformPermissionCatalogRepository(dbContext);

        var permissions = await repository.GetActiveBusinessPermissionsAsync(CancellationToken.None);

        Assert.Equal(36, permissions.Count);
        Assert.Equal(
            PlatformPermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal),
            permissions.Select(permission => permission.Code).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetActiveBusinessPermissionsAsync_ExcludesBootstrapAdminAccessPermission()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        dbContext.PlatformPermissions.Add(PlatformPermission.Create(
            Guid.Parse("61000000-0000-0000-0000-000000000001"),
            PlatformBootstrapPermissionCodes.AdminAccess,
            "Platform Admin Access",
            "Bootstrap platform admin access.",
            PlatformAuthConstants.ActiveStatus,
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformPermissionCatalogRepository repository = new PlatformPermissionCatalogRepository(dbContext);

        var permissions = await repository.GetActiveBusinessPermissionsAsync(CancellationToken.None);

        Assert.Equal(36, permissions.Count);
        Assert.DoesNotContain(
            permissions,
            permission => permission.Code == PlatformBootstrapPermissionCodes.AdminAccess);
    }

    [Fact]
    public async Task GetActiveBusinessPermissionsAsync_ExcludesInactivePermissions()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var inactivePermission = await dbContext.PlatformPermissions
            .SingleAsync(x => x.PermissionCode == PlatformPermissionCodes.AuditView);

        dbContext.Entry(inactivePermission).Property(nameof(PlatformPermission.Status)).CurrentValue = "INACTIVE";
        await dbContext.SaveChangesAsync();

        IPlatformPermissionCatalogRepository repository = new PlatformPermissionCatalogRepository(dbContext);

        var permissions = await repository.GetActiveBusinessPermissionsAsync(CancellationToken.None);

        Assert.Equal(35, permissions.Count);
        Assert.DoesNotContain(
            permissions,
            permission => permission.Code == PlatformPermissionCodes.AuditView);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
