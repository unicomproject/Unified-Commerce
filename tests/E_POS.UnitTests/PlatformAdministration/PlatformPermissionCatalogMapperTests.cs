using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformPermissionCatalogMapperTests
{
    [Fact]
    public void BuildFlat_IncludesAllThirtySixOptionAPermissions()
    {
        var permissions = CreateSeedPermissions();

        var flat = PlatformPermissionCatalogMapper.BuildFlat(permissions);

        Assert.Equal(36, flat.TotalCount);
        Assert.Equal(
            PlatformPermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal),
            flat.Permissions.Select(permission => permission.Code).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void BuildFlat_ExcludesBootstrapAdminAccessPermission()
    {
        var permissions = CreateSeedPermissions();
        permissions.Add(new PlatformPermissionCatalogItem(
            Guid.Parse("61000000-0000-0000-0000-000000000001"),
            PlatformBootstrapPermissionCodes.AdminAccess,
            "Platform Admin Access",
            "Bootstrap platform admin access.",
            "ACTIVE"));

        var flat = PlatformPermissionCatalogMapper.BuildFlat(permissions);

        Assert.Equal(37, permissions.Count);
        Assert.Equal(36, flat.TotalCount);
        Assert.DoesNotContain(
            flat.Permissions,
            permission => permission.Code == PlatformBootstrapPermissionCodes.AdminAccess);
    }

    [Fact]
    public void BuildCatalog_UsesStableModuleAndPermissionSorting()
    {
        var permissions = CreateSeedPermissions()
            .OrderByDescending(permission => permission.Code, StringComparer.Ordinal)
            .ToList();

        var catalog = PlatformPermissionCatalogMapper.BuildCatalog(permissions);

        Assert.Equal(
            ["dashboard", "tenants", "subscription_plans", "modules", "features", "users", "audit", "settings", "billing", "integrations", "permissions", "roles", "return_policy_templates"],
            catalog.Modules.Select(module => module.Key).ToList());

        var tenantModule = catalog.Modules.Single(module => module.Key == "tenants");
        Assert.Equal(["entitlements", "general"], tenantModule.Features.Select(feature => feature.Key).ToList());

        var rolesModule = catalog.Modules.Single(module => module.Key == "roles");
        Assert.Equal(["general", "permissions"], rolesModule.Features.Select(feature => feature.Key).ToList());
        Assert.Equal(2, rolesModule.Features.Single(feature => feature.Key == "permissions").Permissions.Count);

        var allPermissionCodes = catalog.Modules
            .SelectMany(module => module.Features)
            .SelectMany(feature => feature.Permissions)
            .Select(permission => permission.Code)
            .ToList();

        Assert.Equal(
            PlatformPermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal),
            allPermissionCodes.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void IsBootstrapPermission_IdentifiesPlatformAdminAccess()
    {
        Assert.True(PlatformPermissionCatalogMapper.IsBootstrapPermission(PlatformBootstrapPermissionCodes.AdminAccess));
        Assert.False(PlatformPermissionCatalogMapper.IsBootstrapPermission(PlatformPermissionCodes.PermissionsView));
    }

    private static List<PlatformPermissionCatalogItem> CreateSeedPermissions()
    {
        return PlatformAdminPermissionsSeedData.Definitions
            .Select(definition => new PlatformPermissionCatalogItem(
                definition.Id,
                definition.PermissionCode,
                definition.Name,
                definition.Description,
                "ACTIVE"))
            .ToList();
    }
}


