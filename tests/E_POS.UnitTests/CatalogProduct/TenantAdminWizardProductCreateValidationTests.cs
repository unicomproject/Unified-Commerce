using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminWizardProductCreateValidationTests
{
    [Fact]
    public void Wizard_create_request_defaults_to_simple_publishable_shape()
    {
        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "E2E Simple",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                [
                    new BarcodeSkuAssignmentDto(
                        null,
                        "Simple",
                        "E2E-SIMPLE-1",
                        "100000000001",
                        null,
                        "SIMPLE_DEFAULT")
                ]),
            PricingTax = new PricingTaxConfigurationDto(100, 150, 140, Guid.NewGuid(), true),
        };

        Assert.Equal("SIMPLE", request.ProductStructure);
        Assert.NotNull(request.BarcodeSkuConfiguration?.Assignments);
        Assert.Single(request.BarcodeSkuConfiguration!.Assignments!);
        Assert.Equal("SIMPLE_DEFAULT", request.BarcodeSkuConfiguration.Assignments![0].ClientCombinationKey);
        Assert.Null(request.VariantConfiguration);
    }

    [Fact]
    public void Wizard_create_variant_assignments_carry_client_combination_keys()
    {
        var key = "Color:Red;Size:Small";
        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "E2E Variant",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(
                [
                    new VariantConfigurationOptionDto(
                        null, null, "COLOR", "Color", "TEXT", "TEXT", 0,
                        [
                            new VariantConfigurationOptionValueDto(
                                null, null, "RED", "Red", "Red", null, 0, null)
                        ])
                ],
                [
                    new VariantConfigurationVariantDto(
                        key, null, "V1", key, "Red / Small", "Tee - Red / Small", true, "ACTIVE", null,
                        [
                            new VariantConfigurationSelectedValueDto(null, null, "Color", "Red")
                        ])
                ],
                Array.Empty<VariantConfigurationDeletedCombinationDto>()),
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                [
                    new BarcodeSkuAssignmentDto(null, "Red / Small", "SKU-R-S", null, null, key)
                ]),
            PricingTax = new PricingTaxConfigurationDto(10, 20, null, Guid.NewGuid(), true),
        };

        Assert.NotNull(request.VariantConfiguration?.Variants);
        Assert.Single(request.VariantConfiguration!.Variants);
        Assert.Equal(key, request.BarcodeSkuConfiguration!.Assignments![0].ClientCombinationKey);
        Assert.Null(request.BarcodeSkuConfiguration.Assignments![0].ProductVariantId);
    }
}
