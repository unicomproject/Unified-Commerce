using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformModulesCatalogServiceTests
{
    private static readonly Guid ModuleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FeatureId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task GetModulesAsync_WithModulesViewOnly_ReturnsModulesWithoutFeatures()
    {
        var service = CreateService(
            CreateCatalog(),
            hasModulesView: true,
            hasFeaturesView: false);

        var result = await service.GetModulesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Modules);
        Assert.Equal("core_pos", result.Value.Modules[0].ModuleCode);
        Assert.Equal("ACTIVE", result.Value.Modules[0].Status);
        Assert.Empty(result.Value.Modules[0].Features);
    }

    [Fact]
    public async Task GetModulesAsync_WithModulesAndFeaturesView_ReturnsModulesWithFeatures()
    {
        var service = CreateService(
            CreateCatalog(),
            hasModulesView: true,
            hasFeaturesView: true);

        var result = await service.GetModulesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Modules);
        Assert.Single(result.Value.Modules[0].Features);
        Assert.Equal("pos.sales", result.Value.Modules[0].Features[0].FeatureCode);
        Assert.Equal("ACTIVE", result.Value.Modules[0].Features[0].Status);
    }

    [Fact]
    public async Task GetModulesAsync_WithoutModulesView_ReturnsForbidden()
    {
        var service = CreateService(
            CreateCatalog(),
            hasModulesView: false,
            hasFeaturesView: true);

        var result = await service.GetModulesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_modules_catalog.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetModulesAsync_ReturnsExpectedResponseShape()
    {
        var service = CreateService(
            CreateCatalog(),
            hasModulesView: true,
            hasFeaturesView: true);

        var result = await service.GetModulesAsync(Guid.NewGuid(), CancellationToken.None);

        var module = Assert.Single(result.Value!.Modules);
        Assert.Equal(ModuleId, module.Id);
        Assert.Equal("Core POS", module.Name);
        Assert.Equal("Core module", module.Description);
        Assert.Equal(10, module.SortOrder);

        var feature = Assert.Single(module.Features);
        Assert.Equal(FeatureId, feature.Id);
        Assert.Equal("POS Sales", feature.Name);
        Assert.Equal("Start sale", feature.Description);
        Assert.Equal(1, feature.SortOrder);
    }

    private static PlatformModulesCatalogService CreateService(
        IReadOnlyList<PlatformModulesCatalogModuleDto> catalog,
        bool hasModulesView,
        bool hasFeaturesView)
    {
        return new PlatformModulesCatalogService(
            new FakePlatformModulesCatalogRepository(catalog),
            new FakePlatformPermissionChecker(hasModulesView, hasFeaturesView));
    }

    private static IReadOnlyList<PlatformModulesCatalogModuleDto> CreateCatalog()
    {
        return
        [
            new PlatformModulesCatalogModuleDto(
                ModuleId,
                "core_pos",
                "Core POS",
                "Core module",
                10,
                "ACTIVE",
                [
                    new PlatformModulesCatalogFeatureDto(
                        FeatureId,
                        "pos.sales",
                        "POS Sales",
                        "Start sale",
                        1,
                        "ACTIVE")
                ])
        ];
    }

    private sealed class FakePlatformModulesCatalogRepository : IPlatformModulesCatalogRepository
    {
        private readonly IReadOnlyList<PlatformModulesCatalogModuleDto> _modules;

        public FakePlatformModulesCatalogRepository(IReadOnlyList<PlatformModulesCatalogModuleDto> modules)
        {
            _modules = modules;
        }

        public Task<IReadOnlyList<PlatformModulesCatalogModuleDto>> GetActiveModulesAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_modules);
        }
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasModulesView;
        private readonly bool _hasFeaturesView;

        public FakePlatformPermissionChecker(bool hasModulesView, bool hasFeaturesView)
        {
            _hasModulesView = hasModulesView;
            _hasFeaturesView = hasFeaturesView;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            var allowed = permissionCode switch
            {
                _ when string.Equals(permissionCode, PlatformPermissionCodes.ModulesView, StringComparison.Ordinal) =>
                    _hasModulesView,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.FeaturesView, StringComparison.Ordinal) =>
                    _hasFeaturesView,
                _ => false
            };

            return Task.FromResult(allowed);
        }
    }
}


