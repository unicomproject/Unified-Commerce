using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformPermissionCatalogServiceTests
{
    [Fact]
    public async Task GetCatalogAsync_WithPermissionsView_ReturnsCatalog()
    {
        var permissions = CreateSeedPermissions();
        var service = CreateService(
            new FakePlatformPermissionCatalogRepository(permissions),
            new FakePlatformPermissionChecker(hasPermission: true));

        var result = await service.GetCatalogAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(13, result.Value!.Modules.Count);
        Assert.Equal(36, CountPermissions(result.Value));
    }

    [Fact]
    public async Task GetFlatCatalogAsync_WithPermissionsView_ReturnsFlatCatalog()
    {
        var permissions = CreateSeedPermissions();
        var service = CreateService(
            new FakePlatformPermissionCatalogRepository(permissions),
            new FakePlatformPermissionChecker(hasPermission: true));

        var result = await service.GetFlatCatalogAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(36, result.Value!.TotalCount);
    }

    [Fact]
    public async Task GetCatalogAsync_WithoutPermissionsView_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformPermissionCatalogRepository(CreateSeedPermissions()),
            new FakePlatformPermissionChecker(hasPermission: false));

        var result = await service.GetCatalogAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_permission_catalog.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetFlatCatalogAsync_WithoutPermissionsView_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformPermissionCatalogRepository(CreateSeedPermissions()),
            new FakePlatformPermissionChecker(hasPermission: false));

        var result = await service.GetFlatCatalogAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_permission_catalog.access_denied", result.Error.Code);
    }

    private static int CountPermissions(PlatformPermissionCatalogResponse catalog)
    {
        return catalog.Modules
            .SelectMany(module => module.Features)
            .SelectMany(feature => feature.Permissions)
            .Count();
    }

    private static PlatformPermissionCatalogService CreateService(
        IPlatformPermissionCatalogRepository repository,
        IPlatformPermissionChecker permissionChecker)
    {
        return new PlatformPermissionCatalogService(repository, permissionChecker);
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

    private sealed class FakePlatformPermissionCatalogRepository : IPlatformPermissionCatalogRepository
    {
        private readonly IReadOnlyList<PlatformPermissionCatalogItem> _permissions;

        public FakePlatformPermissionCatalogRepository(IReadOnlyList<PlatformPermissionCatalogItem> permissions)
        {
            _permissions = permissions;
        }

        public Task<IReadOnlyList<PlatformPermissionCatalogItem>> GetActiveBusinessPermissionsAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_permissions);
        }
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasPermission;

        public FakePlatformPermissionChecker(bool hasPermission)
        {
            _hasPermission = hasPermission;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _hasPermission &&
                string.Equals(permissionCode, PlatformPermissionCodes.PermissionsView, StringComparison.Ordinal));
        }
    }
}


