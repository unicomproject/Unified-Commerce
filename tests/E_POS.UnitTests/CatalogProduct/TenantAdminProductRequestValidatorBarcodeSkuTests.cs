using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class TenantAdminProductRequestValidatorBarcodeSkuTests
{
    [Fact]
    public void ValidateBarcodeSkuDraft_Valid_ReturnsNull()
    {
        // Arrange
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = ProductWizardStage.BarcodeSku,
            WizardAction = "SAVE_DRAFT",
            BaseSku = "SKU-001",
            ParentProductBarcode = "1234567890123"
        };

        // Act
        var result = TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateBarcodeSkuDraft_DuplicateSkuInRequest_ReturnsError()
    {
        // Arrange
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = ProductWizardStage.BarcodeSku,
            WizardAction = "SAVE_DRAFT",
            BaseSku = "SKU-001",
            VariantIdentifiers = new List<Step5VariantIdentifierDto>
            {
                new Step5VariantIdentifierDto(Guid.NewGuid(), "SKU-001", "111")
            }
        };

        // Act
        var result = TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("product.validation_failed", result.Code);
        Assert.Contains(result.FieldErrors, e => e.Field == "variantIdentifiers[0].sku" && e.Message == "Duplicate SKU in request.");
    }

    [Fact]
    public void ValidateBarcodeSkuDraft_DuplicateBarcodeInRequest_ReturnsError()
    {
        // Arrange
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = ProductWizardStage.BarcodeSku,
            WizardAction = "SAVE_DRAFT",
            ParentProductBarcode = "1234567890123",
            AdditionalBarcodes = new List<Step5AdditionalBarcodeDto>
            {
                new Step5AdditionalBarcodeDto(null, "1234567890123", "EAN_13", null, null, 1, false, "ACTIVE")
            }
        };

        // Act
        var result = TenantAdminProductRequestValidator.ValidateBarcodeSkuDraft(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("product.validation_failed", result.Code);
        Assert.Contains(result.FieldErrors, e => e.Field == "additionalBarcodes[0].barcode" && e.Message == "Duplicate barcode in request.");
    }
}
