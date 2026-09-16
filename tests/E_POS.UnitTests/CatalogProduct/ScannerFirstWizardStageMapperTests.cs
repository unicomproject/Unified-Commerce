using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ScannerFirstWizardStageMapperTests
{
    [Theory]
    [InlineData(2, ProductWizardStage.BasicDetails, false)]
    [InlineData(3, ProductWizardStage.ProductTypeTracking, false)]
    [InlineData(4, ProductWizardStage.UnitsPackConversion, false)]
    [InlineData(6, ProductWizardStage.PricingTax, false)]
    [InlineData(7, ProductWizardStage.ReviewCreate, false)]
    public void TryMapToProcessorStage_NonAmbiguousSteps_MapsSemantically(
        int scannerStep,
        int expectedProcessor,
        bool expectedSpecial)
    {
        var ok = ScannerFirstWizardStageMapper.TryMapToProcessorStage(
            scannerStep,
            out var processor,
            out var special);

        Assert.True(ok);
        Assert.Equal(expectedProcessor, processor);
        Assert.Equal(expectedSpecial, special);
    }

    [Fact]
    public void TryMapToProcessorStage_Step5_IsSpecialComposite_NotArithmetic()
    {
        var ok = ScannerFirstWizardStageMapper.TryMapToProcessorStage(
            5,
            out var processor,
            out var special);

        Assert.True(ok);
        Assert.True(special);
        Assert.True(ScannerFirstWizardStageMapper.IsSpecialCompositeStep(5));
        // Semantic composite: configuration processor — NOT scannerStep-1 (=4) alone as the rule,
        // and NOT scannerStep+1. Legacy BarcodeSku (5) also converges to public 5.
        Assert.Equal(ProductWizardStage.ProductConfiguration, processor);
        // Blind ±1 would claim barcode processor owns Step 5 exclusively; both 4 and 5 map to public 5.
        Assert.Equal(5, ScannerFirstWizardStageMapper.MapProcessorToPublicStep(ProductWizardStage.BarcodeSku));
        Assert.Equal(5, ScannerFirstWizardStageMapper.MapProcessorToPublicStep(ProductWizardStage.ProductConfiguration));
        Assert.NotEqual(5 - 1, ProductWizardStage.BarcodeSku);
    }

    [Fact]
    public void MapProcessorToPublicStep_BasicDetails_PersistsPublicStep2()
    {
        Assert.Equal(2, ScannerFirstWizardStageMapper.MapProcessorToPublicStep(ProductWizardStage.BasicDetails));
        Assert.Equal(1, ProductWizardStage.BasicDetails);
    }

    [Fact]
    public void ProductWizardStage_Constants_NotGloballyRenumbered()
    {
        Assert.Equal(1, ProductWizardStage.BasicDetails);
        Assert.Equal(2, ProductWizardStage.ProductTypeTracking);
        Assert.Equal(3, ProductWizardStage.UnitsPackConversion);
        Assert.Equal(4, ProductWizardStage.ProductConfiguration);
        Assert.Equal(5, ProductWizardStage.BarcodeSku);
        Assert.Equal(6, ProductWizardStage.PricingTax);
        Assert.Equal(7, ProductWizardStage.ReviewCreate);
    }

    [Fact]
    public void TryMapToProcessorStage_PublicStep1_Fails()
    {
        Assert.False(ScannerFirstWizardStageMapper.TryMapToProcessorStage(1, out _, out _));
    }
}
