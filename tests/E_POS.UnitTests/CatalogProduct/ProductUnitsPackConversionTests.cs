using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class ProductUnitsPackConversionTests
{
    [Fact]
    public void ProductUnitModelConstants_ValidateAndNormalize_BehavesAsExpected()
    {
        Assert.True(ProductUnitModelConstants.IsValid("SINGLE_UNIT"));
        Assert.True(ProductUnitModelConstants.IsValid("MULTIPLE_UNITS"));
        Assert.True(ProductUnitModelConstants.IsValid("single_unit"));
        Assert.False(ProductUnitModelConstants.IsValid("INVALID"));

        Assert.Equal(ProductUnitModelConstants.SingleUnit, ProductUnitModelConstants.Normalize("single_unit"));
        Assert.Equal(ProductUnitModelConstants.MultipleUnits, ProductUnitModelConstants.Normalize("MULTIPLE_UNITS"));
        Assert.Equal(ProductUnitModelConstants.SingleUnit, ProductUnitModelConstants.Normalize(null));
    }

    [Fact]
    public void ValidateUnitsPackConversionDraft_ValidSingleUnit_ReturnsNull()
    {
        var request = new SaveProductDraftRequest
        {
            ProductName = "Test Product",
            ProductCode = "PRD-001",
            ProductStructure = ProductStructureConstants.Simple,
            CategoryId = Guid.NewGuid(),
            DesiredPublishActive = true,
            PosSellable = true,
            TrackInventory = true,
            CurrentSetupStep = ProductWizardStage.UnitsPackConversion,
            ExpectedRowVersion = 1L,
            UnitModel = ProductUnitModelConstants.SingleUnit,
            BaseUnitId = Guid.NewGuid()
        };

        var error = TenantAdminProductRequestValidator.ValidateUnitsPackConversionDraft(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateUnitsPackConversionDraft_InvalidUnitModel_ReturnsError()
    {
        var request = new SaveProductDraftRequest
        {
            ProductName = "Test Product",
            ProductCode = "PRD-001",
            ProductStructure = ProductStructureConstants.Simple,
            CategoryId = Guid.NewGuid(),
            DesiredPublishActive = true,
            PosSellable = true,
            TrackInventory = true,
            CurrentSetupStep = ProductWizardStage.UnitsPackConversion,
            ExpectedRowVersion = 1L,
            UnitModel = "INVALID_MODEL"
        };

        var error = TenantAdminProductRequestValidator.ValidateUnitsPackConversionDraft(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, fe => fe.Field == "unitModel");
    }

    [Fact]
    public void ValidateUnitsPackConversionContinue_MultipleUnits_InvalidSellingTier_ReturnsExpectedErrorCode()
    {
        var baseId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var outerId = Guid.NewGuid();
        var unconfiguredSellingId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Test Product",
            ProductCode = "PRD-001",
            ProductStructure = ProductStructureConstants.Simple,
            CategoryId = Guid.NewGuid(),
            DesiredPublishActive = true,
            PosSellable = true,
            TrackInventory = true,
            CurrentSetupStep = ProductWizardStage.UnitsPackConversion,
            ExpectedRowVersion = 1L,
            UnitModel = ProductUnitModelConstants.MultipleUnits,
            BaseUnitId = baseId,
            PurchaseUnitId = purchaseId,
            OuterPackUnitId = outerId,
            SellingUnitId = unconfiguredSellingId,
            ItemsPerPurchaseUnit = 6m,
            PurchaseUnitsPerOuterPack = 12m,
            AllowDecimalQuantity = false
        };

        var error = TenantAdminProductRequestValidator.ValidateUnitsPackConversionContinue(request);

        Assert.NotNull(error);
        var sellingError = Assert.Single(error!.FieldErrors!, fe => fe.Field == "sellingUnitId");
        Assert.Equal("unit.selling_unit_must_match_configured_tier", sellingError.Code);
    }

    [Fact]
    public void ValidateUnitsPackConversionContinue_FractionalMultiplierWithoutDecimal_ReturnsExpectedErrorCode()
    {
        var baseId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Test Product",
            ProductCode = "PRD-001",
            ProductStructure = ProductStructureConstants.Simple,
            CategoryId = Guid.NewGuid(),
            DesiredPublishActive = true,
            PosSellable = true,
            TrackInventory = true,
            CurrentSetupStep = ProductWizardStage.UnitsPackConversion,
            ExpectedRowVersion = 1L,
            UnitModel = ProductUnitModelConstants.MultipleUnits,
            BaseUnitId = baseId,
            PurchaseUnitId = purchaseId,
            SellingUnitId = baseId,
            ItemsPerPurchaseUnit = 2.5m,
            AllowDecimalQuantity = false
        };

        var error = TenantAdminProductRequestValidator.ValidateUnitsPackConversionContinue(request);

        Assert.NotNull(error);
        var decimalError = Assert.Single(error!.FieldErrors!, fe => fe.Field == "allowDecimalQuantity");
        Assert.Equal("unit.fractional_conversion_requires_decimal_quantity", decimalError.Code);
    }

    [Fact]
    public void ValidateUnitsPackConversionContinue_ValidMultipleUnits_ReturnsNull()
    {
        var baseId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var outerId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Test Product",
            ProductCode = "PRD-001",
            ProductStructure = ProductStructureConstants.Simple,
            CategoryId = Guid.NewGuid(),
            DesiredPublishActive = true,
            PosSellable = true,
            TrackInventory = true,
            CurrentSetupStep = ProductWizardStage.UnitsPackConversion,
            ExpectedRowVersion = 1L,
            UnitModel = ProductUnitModelConstants.MultipleUnits,
            BaseUnitId = baseId,
            PurchaseUnitId = purchaseId,
            OuterPackUnitId = outerId,
            SellingUnitId = baseId,
            ItemsPerPurchaseUnit = 6m,
            PurchaseUnitsPerOuterPack = 12m,
            AllowDecimalQuantity = false
        };

        var error = TenantAdminProductRequestValidator.ValidateUnitsPackConversionContinue(request);

        Assert.Null(error);
    }
}
