using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

/// <summary>
/// Step 6 Pricing & Tax — client-side validator structure rules (SIMPLE regression + VARIANT).
/// Authoritative DB coverage is covered by repository/integration tests.
/// </summary>
public sealed class TenantAdminProductPricingTaxValidatorTests
{
    private readonly TenantAdminProductRequestValidator _validator = new();

    [Fact]
    public void Draft_Allows_Partial_Variant_Prices()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 6,
            ProductStructure = "VARIANT",
            PricingTax = new PricingTaxConfigurationDto(
                null,
                null,
                null,
                Guid.NewGuid(),
                true,
                [
                    new VariantPriceConfigurationDto(Guid.NewGuid(), null, 750m),
                    new VariantPriceConfigurationDto(Guid.NewGuid(), null, null)
                ])
        };

        Assert.Null(_validator.ValidateStepSaveDraft(request));
    }

    [Fact]
    public void Continue_Variant_Rejects_Zero_Price()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 6,
            AdvanceStep = true,
            ProductStructure = "VARIANT",
            PricingTax = new PricingTaxConfigurationDto(
                null,
                null,
                null,
                Guid.NewGuid(),
                true,
                [
                    new VariantPriceConfigurationDto(Guid.NewGuid(), null, 0m)
                ])
        };

        var error = _validator.ValidateStepSaveAndContinue(request);
        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field.Contains("sellingPrice", StringComparison.Ordinal));
    }

    [Fact]
    public void Continue_Simple_Ignores_VariantPrices_Requirement()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 6,
            AdvanceStep = true,
            ProductStructure = "SIMPLE",
            PricingTax = new PricingTaxConfigurationDto(
                null,
                650m,
                null,
                Guid.NewGuid(),
                false)
        };

        Assert.Null(_validator.ValidateStepSaveAndContinue(request));
    }

    [Fact]
    public void Draft_Rejects_Scalar_Negative_On_Simple()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 6,
            ProductStructure = "SIMPLE",
            PricingTax = new PricingTaxConfigurationDto(null, -1m, null, Guid.NewGuid(), true)
        };

        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field == "pricingTax.standardSellingPrice");
    }
}
