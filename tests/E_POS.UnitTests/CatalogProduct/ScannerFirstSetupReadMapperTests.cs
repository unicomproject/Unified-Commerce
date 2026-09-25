using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ScannerFirstSetupReadMapperTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    [InlineData(5, 3)]
    [InlineData(6, 4)]
    [InlineData(7, 6)]
    public void MapLegacyPersistedStepToPublic_ExactTable(int legacy, int expectedPublic)
    {
        Assert.Equal(expectedPublic, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(legacy));
    }

    [Fact]
    public void MapLegacy_Old3And4And5_AllBecomePublic3()
    {
        Assert.Equal(3, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(3));
        Assert.Equal(3, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(4));
        Assert.Equal(3, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(5));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ApplyReadCompatibility_ScannerFirst_DoesNotLegacyRemap(int persisted)
    {
        var setup = MinimalSetup(persisted, ProductStructureConstants.Simple, new ProductSetupScanContextDto("SCAN", "4006381333931"));

        var result = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup, hasScanContextRow: true);

        Assert.Equal(persisted, result.CurrentSetupStep);
        Assert.Equal(persisted, result.TargetSetupStep);
        Assert.Equal("SCAN", result.ScanContext!.AcquisitionMode);
        Assert.Equal("4006381333931", result.ScanContext.CandidateIdentifier);
    }

    [Fact]
    public void ApplyReadCompatibility_Legacy_Maps1To2_AndProjectsLegacyAcquisition()
    {
        var setup = MinimalSetup(1, ProductStructureConstants.Simple, scanContext: null);

        var result = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup, hasScanContextRow: false);

        Assert.Equal(2, result.CurrentSetupStep);
        Assert.Equal(2, result.TargetSetupStep);
        Assert.Equal("LEGACY", result.ScanContext!.AcquisitionMode);
        Assert.Null(result.ScanContext.CandidateIdentifier);
        Assert.Null(result.ScanContext.ExternalLookupStatus);
        Assert.Null(result.ScanContext.NormalizedPrefill);
        Assert.Null(result.ScanContext.GeneratedSkuCandidate);
        Assert.Null(result.ScanContext.ExternalSourceReference);
    }

    [Fact]
    public void ApplyReadCompatibility_LegacyOldStep6_ReturnsPublic4()
    {
        var setup = MinimalSetup(6, ProductStructureConstants.Simple, scanContext: null);

        var result = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup, hasScanContextRow: false);

        Assert.Equal(4, result.CurrentSetupStep);
        Assert.Equal("LEGACY", result.ScanContext!.AcquisitionMode);
    }

    private static ProductSetupWizardDto MinimalSetup(
        int currentSetupStep,
        string structure,
        ProductSetupScanContextDto? scanContext) =>
        new(
            Guid.NewGuid(),
            "P",
            "C",
            ProductConstants.DraftStatus,
            ProductConstants.DesiredPublishActive,
            currentSetupStep,
            null,
            1,
            null,
            null,
            null,
            null,
            true,
            false,
            false,
            false,
            false,
            structure,
            false,
            [],
            TargetSetupStep: currentSetupStep,
            LastCompletedSetupStep: currentSetupStep,
            ScanContext: scanContext);
}
