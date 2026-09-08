using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminBarcodeSkuStep5Tests
{
    // Valid EAN-13 (checksum verified): 4006381333931
    private const string ValidEan13 = "4006381333931";

    [Fact]
    public void BarcodeSkuAssignmentDto_RoundTrips_BarcodeType()
    {
        var dto = new BarcodeSkuAssignmentDto(
            Guid.NewGuid(),
            "Blue / 500ml",
            "SKU-BLU-500",
            ValidEan13,
            "COMPLETE",
            "Color:Blue;Size:500",
            "EAN13");

        Assert.Equal("EAN13", dto.BarcodeType);
        Assert.Equal(ValidEan13, dto.Barcode);
        Assert.Equal("Color:Blue;Size:500", dto.ClientCombinationKey);
    }

    [Fact]
    public void ProductBarcodeFormatValidator_CanonicalTypes_MatchSpecLabels()
    {
        Assert.Contains(ProductBarcodeFormatValidator.CanonicalTypes, t => t is ("EAN13", "EAN-13"));
        Assert.Contains(ProductBarcodeFormatValidator.CanonicalTypes, t => t is ("EAN8", "EAN-8"));
        Assert.Contains(ProductBarcodeFormatValidator.CanonicalTypes, t => t is ("UPCA", "UPC-A"));
        Assert.Contains(ProductBarcodeFormatValidator.CanonicalTypes, t => t is ("CODE128", "CODE-128"));
        Assert.Contains(ProductBarcodeFormatValidator.CanonicalTypes, t => t is ("CODE39", "CODE-39"));
    }

    [Fact]
    public void ProductBarcodeFormatValidator_PreservesLeadingZeros_AndValidatesChecksum()
    {
        // UPC-A with leading zero preserved as string (12 digits, valid checksum).
        const string upc = "012345678905";
        Assert.Null(ProductBarcodeFormatValidator.Validate(upc, "UPCA"));
        Assert.StartsWith("0", upc, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductBarcodeFormatValidator_RejectsMissingType_WhenBarcodePresent()
    {
        var error = ProductBarcodeFormatValidator.Validate(ValidEan13, null);
        Assert.NotNull(error);
        Assert.Contains("type", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductBarcodeFormatValidator_AllowsBlankBarcode_WithoutType()
    {
        Assert.Null(ProductBarcodeFormatValidator.Validate(null, null));
        Assert.Null(ProductBarcodeFormatValidator.Validate("  ", null));
    }

    [Fact]
    public void ValidateBarcodeSkuDraft_RequiresBarcodeType_WhenBarcodePresent()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 5,
            ProductStructure = ProductStructureConstants.Variant,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                null,
                [
                    new BarcodeSkuAssignmentDto(
                        Guid.NewGuid(),
                        "Red",
                        "SKU-1",
                        ValidEan13,
                        null,
                        "k1",
                        null)
                ])
        };

        var error = TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field.EndsWith(".barcodeType", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateBarcodeSkuDraft_AcceptsValidBarcodeAndType()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 5,
            ProductStructure = ProductStructureConstants.Variant,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                null,
                [
                    new BarcodeSkuAssignmentDto(
                        Guid.NewGuid(),
                        "Red",
                        "SKU-1",
                        ValidEan13,
                        null,
                        "k1",
                        "EAN13")
                ])
        };

        Assert.Null(TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(request));
    }

    [Fact]
    public void ValidateBarcodeSkuDraft_DetectsInRequestDuplicateSku_CaseSensitive()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 5,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                null,
                [
                    new BarcodeSkuAssignmentDto(Guid.NewGuid(), "A", "Sku-A", null, null, "a"),
                    new BarcodeSkuAssignmentDto(Guid.NewGuid(), "B", "Sku-A", null, null, "b"),
                ])
        };

        var error = TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field.Contains(".sku", StringComparison.Ordinal));

        // Different case must NOT collide under Ordinal.
        var differentCase = new SaveProductDraftRequest
        {
            CurrentSetupStep = 5,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                null,
                [
                    new BarcodeSkuAssignmentDto(Guid.NewGuid(), "A", "Sku-A", null, null, "a"),
                    new BarcodeSkuAssignmentDto(Guid.NewGuid(), "B", "sku-a", null, null, "b"),
                ])
        };

        Assert.Null(TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(differentCase));
    }

    [Fact]
    public void ValidateBarcodeSkuContinue_RejectsBlankSku_OnSubmittedRows()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 5,
            WizardAction = "SAVE_AND_CONTINUE",
            ProductStructure = ProductStructureConstants.Variant,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                null,
                [
                    new BarcodeSkuAssignmentDto(Guid.NewGuid(), "A", "SKU-1", null, null, "a"),
                    new BarcodeSkuAssignmentDto(Guid.NewGuid(), "B", " ", null, null, "b"),
                ])
        };

        var error = TenantAdminProductRequestValidator.ValidateBarcodeSkuContinue(request);
        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field.EndsWith(".sku", StringComparison.Ordinal));
    }
}
