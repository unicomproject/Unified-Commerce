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
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    [InlineData(5, 5)]
    [InlineData(6, 6)]
    [InlineData(7, 7)]
    public void MapLegacyPersistedStepToPublic_ExactTable(int legacy, int expectedPublic)
    {
        Assert.Equal(expectedPublic, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(legacy));
    }

    [Fact]
    public void MapLegacy_Old4AndOld5_BothBecomePublic5()
    {
        Assert.Equal(5, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(4));
        Assert.Equal(5, ScannerFirstSetupReadMapper.MapLegacyPersistedStepToPublic(5));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
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
    public void ApplyReadCompatibility_LegacyOldStep5_ReturnsPublic5()
    {
        var setup = MinimalSetup(5, ProductStructureConstants.Simple, scanContext: null);

        var result = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup, hasScanContextRow: false);

        Assert.Equal(5, result.CurrentSetupStep);
        Assert.Equal("LEGACY", result.ScanContext!.AcquisitionMode);
    }

    [Fact]
    public void ResolveTargetSetupStep_BundleAtUnits_TargetsConfiguration()
    {
        Assert.Equal(5, ScannerFirstSetupReadMapper.ResolveTargetSetupStep(ProductStructureConstants.Bundle, 4));
        Assert.Equal(4, ScannerFirstSetupReadMapper.ResolveTargetSetupStep(ProductStructureConstants.Simple, 4));
    }

    [Fact]
    public void ApplyReadCompatibility_LegacyBundleOldStep3_MapsTo4_Target5()
    {
        var setup = MinimalSetup(3, ProductStructureConstants.Bundle, scanContext: null);

        var result = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup, hasScanContextRow: false);

        Assert.Equal(4, result.CurrentSetupStep);
        Assert.Equal(5, result.TargetSetupStep);
    }

    [Fact]
    public void ApplyReadCompatibility_ScannerFirstBundleAtStep4_NoLegacyRemap_Target5()
    {
        var setup = MinimalSetup(4, ProductStructureConstants.Bundle, new ProductSetupScanContextDto("NO_BARCODE", NoBarcodeReason: "OWN_MADE"));

        var result = ScannerFirstSetupReadMapper.ApplyReadCompatibility(setup, hasScanContextRow: true);

        Assert.Equal(4, result.CurrentSetupStep);
        Assert.Equal(5, result.TargetSetupStep);
        Assert.Equal("NO_BARCODE", result.ScanContext!.AcquisitionMode);
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
