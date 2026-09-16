using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    [Fact]
    public async Task GetSetupAsync_LegacyDraft_RemapsStepAndProjectsLegacyAcquisition()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = 1,
                TargetSetupStep = 1,
                LastCompletedSetupStep = 1,
                ScanContext = null
            }
        };
        var coordinator = new FakeExternalProductLookupCoordinator();
        var service = CreateService(repository, coordinator);

        var result = await service.GetSetupAsync(
            CreateContext([ProductConstants.ViewPermission]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CurrentSetupStep);
        Assert.Equal(2, result.Value.TargetSetupStep);
        Assert.Equal("LEGACY", result.Value.ScanContext!.AcquisitionMode);
        Assert.Null(result.Value.ScanContext.CandidateIdentifier);
        Assert.Null(result.Value.ScanContext.ExternalLookupStatus);
        Assert.Null(result.Value.ScanContext.NormalizedPrefill);
        Assert.Null(result.Value.ScanContext.GeneratedSkuCandidate);
        Assert.Equal(0, coordinator.CallCount);
        Assert.Equal(0, repository.ResolveLookupCallCount);
        Assert.Equal(0, repository.SkuExistsCallCount);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task GetSetupAsync_ScannerFirst_DoesNotLegacyRemap(int persistedStep)
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = persistedStep,
                TargetSetupStep = persistedStep,
                ScanContext = new ProductSetupScanContextDto(
                    "SCAN",
                    "012345678905",
                    "GTIN12",
                    "UNKNOWN",
                    null,
                    "NOT_STARTED",
                    null,
                    null,
                    null)
            }
        };
        var service = CreateService(repository);

        var result = await service.GetSetupAsync(
            CreateContext([ProductConstants.ViewPermission]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(persistedStep, result.Value!.CurrentSetupStep);
        Assert.Equal("SCAN", result.Value.ScanContext!.AcquisitionMode);
        Assert.Equal("012345678905", result.Value.ScanContext.CandidateIdentifier);
    }

    [Fact]
    public async Task GetSetupAsync_LegacyBundleOldStep3_MapsTo4_Target5()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                ProductStructure = ProductStructureConstants.Bundle,
                CurrentSetupStep = 3,
                TargetSetupStep = 3,
                ScanContext = null
            }
        };
        var service = CreateService(repository);

        var result = await service.GetSetupAsync(
            CreateContext([ProductConstants.ViewPermission]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.CurrentSetupStep);
        Assert.Equal(5, result.Value.TargetSetupStep);
        Assert.Equal("LEGACY", result.Value.ScanContext!.AcquisitionMode);
    }

    [Fact]
    public async Task GetSetupAsync_ScannerFirstHydratesPrefill_NoExternalCall()
    {
        var productId = Guid.NewGuid();
        var prefill = new ExternalProductSuggestion(
            "Prefill Name",
            null,
            "Brand Text",
            null,
            null,
            null,
            null,
            null,
            null,
            "4006381333931",
            "GTIN13");
        var coordinator = new FakeExternalProductLookupCoordinator();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = 2,
                ScanContext = new ProductSetupScanContextDto(
                    "SCAN",
                    "4006381333931",
                    "GTIN13",
                    "EAN13",
                    null,
                    "FOUND",
                    "provider:ref-1",
                    prefill,
                    null)
            }
        };
        var service = CreateService(repository, coordinator);

        var result = await service.GetSetupAsync(
            CreateContext([ProductConstants.ViewPermission]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CurrentSetupStep);
        Assert.Equal("FOUND", result.Value.ScanContext!.ExternalLookupStatus);
        Assert.Equal("Prefill Name", result.Value.ScanContext.NormalizedPrefill!.ProductName);
        Assert.Equal("4006381333931", result.Value.ScanContext.NormalizedPrefill.PrimaryGtin);
        Assert.Equal(0, coordinator.CallCount);
    }

    [Fact]
    public async Task GetSetupAsync_MissingPermission_ReturnsForbidden()
    {
        var service = CreateService(new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(Guid.NewGuid(), Guid.NewGuid())
        });

        var result = await service.GetSetupAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.permission_denied", result.Error!.Code);
    }

    [Fact]
    public async Task GetSetupAsync_UnknownProduct_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository { SetupDto = null });

        var result = await service.GetSetupAsync(
            CreateContext([ProductConstants.ViewPermission]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.not_found", result.Error!.Code);
    }
}
