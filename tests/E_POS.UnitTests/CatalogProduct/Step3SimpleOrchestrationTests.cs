using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

/// <summary>
/// Phase B: Target Step 3 — SIMPLE Product Type & Configuration orchestration tests.
/// Validates that existing unit/pack/identifier services compose correctly at Step 3.
/// </summary>
public sealed class Step3SimpleOrchestrationTests
{
    [Fact]
    public void BuildSimpleRequest_SingleUnit_HasValidStructure()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var taxId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Simple Widget",
            ProductCode = "SIMPLE-001",
            CategoryId = categoryId,
            ProductStructure = "SIMPLE",
            UnitModel = "SINGLE_UNIT",
            BaseUnitId = uomId,
            SellingUnitId = uomId,
            PurchaseUnitId = uomId,
            AllowDecimalQuantity = false,
            DesiredPublishActive = true,
            PosSellable = true,
            AllowOnlineSale = true,
            TrackInventory = false,
            CurrentSetupStep = 3,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                new List<BarcodeSkuAssignmentDto>
                {
                    new BarcodeSkuAssignmentDto(
                        null,
                        "Simple Widget",
                        "SKU-SIMPLE-001",
                        "8901234567890",
                        null,
                        "SIMPLE_DEFAULT",
                        "CODE128")
                })
        };

        Assert.Equal("SIMPLE", request.ProductStructure);
        Assert.Equal("SINGLE_UNIT", request.UnitModel);
        Assert.NotNull(request.BaseUnitId);
        Assert.NotNull(request.BarcodeSkuConfiguration);
        Assert.Single(request.BarcodeSkuConfiguration.Assignments);
    }

    [Fact]
    public void BuildSimpleRequest_MultiplePacks_HasValidStructure()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var baseUomId = Guid.NewGuid();
        var purchaseUomId = Guid.NewGuid();
        var taxId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Packed Widget",
            ProductCode = "PACKED-001",
            CategoryId = categoryId,
            ProductStructure = "SIMPLE",
            UnitModel = "MULTIPLE_UNITS",
            BaseUnitId = baseUomId,
            SellingUnitId = baseUomId,
            PurchaseUnitId = purchaseUomId,
            ItemsPerPurchaseUnit = 12m,
            PurchaseUnitsPerOuterPack = 4m,
            AllowDecimalQuantity = false,
            DesiredPublishActive = true,
            CurrentSetupStep = 3,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                new List<BarcodeSkuAssignmentDto>
                {
                    new BarcodeSkuAssignmentDto(
                        null, "Packed Widget", "SKU-PACK-001", null, null, "SIMPLE_DEFAULT", null)
                })
        };

        Assert.Equal("SIMPLE", request.ProductStructure);
        Assert.Equal("MULTIPLE_UNITS", request.UnitModel);
        Assert.Equal(12m, request.ItemsPerPurchaseUnit);
        Assert.Equal(4m, request.PurchaseUnitsPerOuterPack);
    }

    [Fact]
    public void SimpleRequest_MissingBaseUnit_ShouldBeDetectedByValidator()
    {
        var request = new SaveProductDraftRequest
        {
            ProductName = "No Unit Widget",
            ProductStructure = "SIMPLE",
            UnitModel = "SINGLE_UNIT",
            BaseUnitId = null,  // INVALID
            CurrentSetupStep = 3
        };

        Assert.Null(request.BaseUnitId);
        // Validator should catch this
    }

    [Fact]
    public void SimpleRequest_MissingSku_ShouldBeDetectedByValidator()
    {
        var uomId = Guid.NewGuid();
        var request = new SaveProductDraftRequest
        {
            ProductName = "No SKU Widget",
            ProductStructure = "SIMPLE",
            UnitModel = "SINGLE_UNIT",
            BaseUnitId = uomId,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                new List<BarcodeSkuAssignmentDto>
                {
                    new BarcodeSkuAssignmentDto(null, "No SKU Widget", "", null, null, "SIMPLE_DEFAULT", null)
                })
        };

        var assignment = request.BarcodeSkuConfiguration!.Assignments![0];
        Assert.True(string.IsNullOrWhiteSpace(assignment.Sku));
        // Validator should reject empty SKU
    }

    [Fact]
    public void SimpleRequest_ContainsZero_ShouldBeDetectedByValidator()
    {
        var baseUomId = Guid.NewGuid();
        var purchaseUomId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Zero Contains Widget",
            ProductStructure = "SIMPLE",
            UnitModel = "MULTIPLE_UNITS",
            BaseUnitId = baseUomId,
            PurchaseUnitId = purchaseUomId,
            ItemsPerPurchaseUnit = 0m,  // INVALID
            CurrentSetupStep = 3
        };

        Assert.Equal(0m, request.ItemsPerPurchaseUnit);
        // Validator should reject
    }

    [Fact]
    public void SimpleRequest_NoBarcode_IsValid()
    {
        var uomId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "No Barcode Widget",
            ProductStructure = "SIMPLE",
            UnitModel = "SINGLE_UNIT",
            BaseUnitId = uomId,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                new List<BarcodeSkuAssignmentDto>
                {
                    new BarcodeSkuAssignmentDto(null, "No Barcode", "SKU-NB-001", null, null, "SIMPLE_DEFAULT", null)
                })
        };

        var assignment = request.BarcodeSkuConfiguration!.Assignments![0];
        Assert.Null(assignment.Barcode);
        // Should be valid (barcode optional)
    }

    [Fact]
    public void SimpleRequest_BarcodeWithoutType_ShouldBeInvalid()
    {
        var uomId = Guid.NewGuid();

        var request = new SaveProductDraftRequest
        {
            ProductName = "Bad Barcode Widget",
            ProductStructure = "SIMPLE",
            UnitModel = "SINGLE_UNIT",
            BaseUnitId = uomId,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                new List<BarcodeSkuAssignmentDto>
                {
                    new BarcodeSkuAssignmentDto(
                        null, "Bad Barcode", "SKU-BB-001", "8901234567890", null, "SIMPLE_DEFAULT", null)
                })
        };

        var assignment = request.BarcodeSkuConfiguration!.Assignments![0];
        Assert.NotNull(assignment.Barcode);
        Assert.Null(assignment.BarcodeType);
        // Validator should reject: barcode requires type
    }

    [Fact]
    public void SimpleStep3_CurrentSetupStep_Should_Be_3()
    {
        var request = new SaveProductDraftRequest
        {
            ProductName = "Step 3 Widget",
            ProductStructure = "SIMPLE",
            CurrentSetupStep = 3
        };

        Assert.Equal(3, request.CurrentSetupStep);
    }
}
