using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductWizardNextStageResolverTests
{
    [Theory]
    [InlineData(ProductStructureConstants.Simple, true, ProductWizardStage.UnitsPackConversion)]
    [InlineData(ProductStructureConstants.Variant, true, ProductWizardStage.UnitsPackConversion)]
    [InlineData(ProductStructureConstants.Simple, false, ProductWizardStage.BarcodeSku)]
    [InlineData(ProductStructureConstants.Variant, false, ProductWizardStage.ProductConfiguration)]
    [InlineData(ProductStructureConstants.Bundle, true, ProductWizardStage.ProductConfiguration)]
    [InlineData(ProductStructureConstants.Bundle, false, ProductWizardStage.ProductConfiguration)]
    public void FromProductTypeTracking_FollowsSevenStepUnitsMatrix(
        string structure,
        bool trackInventory,
        int expectedProcessor)
    {
        var next = ProductWizardNextStageResolver.ResolveNextApplicableStage(
            structure,
            trackInventory,
            ProductWizardStage.ProductTypeTracking);

        Assert.Equal(expectedProcessor, next);
    }

    [Theory]
    [InlineData(ProductStructureConstants.Simple, ProductWizardStage.BarcodeSku)]
    [InlineData(ProductStructureConstants.Variant, ProductWizardStage.ProductConfiguration)]
    public void FromUnitsPack_AdvancesToConfigurationOrLegacyBarcode(
        string structure,
        int expectedProcessor)
    {
        var next = ProductWizardNextStageResolver.ResolveNextApplicableStage(
            structure,
            trackInventory: true,
            ProductWizardStage.UnitsPackConversion);

        Assert.Equal(expectedProcessor, next);
    }

    [Fact]
    public void ScannerFirst_VariantTrackOn_MapsPublicStep4()
    {
        var processor = ProductWizardNextStageResolver.ResolveNextApplicableStage(
            ProductStructureConstants.Variant,
            trackInventory: true,
            ProductWizardStage.ProductTypeTracking);

        Assert.Equal(
            ScannerFirstWizardStageMapper.PublicUnitsPackConversion,
            ScannerFirstWizardStageMapper.MapProcessorToPublicStep(processor));
    }

    [Fact]
    public void ScannerFirst_Bundle_MapsPublicStep5()
    {
        var processor = ProductWizardNextStageResolver.ResolveNextApplicableStage(
            ProductStructureConstants.Bundle,
            trackInventory: false,
            ProductWizardStage.ProductTypeTracking);

        Assert.Equal(
            ScannerFirstWizardStageMapper.PublicProductConfiguration,
            ScannerFirstWizardStageMapper.MapProcessorToPublicStep(processor));
    }
}
