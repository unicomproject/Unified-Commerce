using System;
using System.Collections.Generic;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class TenantAdminProductRequestValidatorVariantConfigurationTests
{
    private readonly TenantAdminProductRequestValidator _validator = new();

    [Fact]
    public void ValidateSaveDraft_IncompleteStep4_Succeeds()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(new List<VariantConfigurationOptionDto>(), new List<VariantConfigurationVariantDto>(), new List<VariantConfigurationDeletedCombinationDto>())
        };
        
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateSaveDraft_DuplicateAttribute_ReturnsFieldError()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(
                new List<VariantConfigurationOptionDto>
                {
                    new VariantConfigurationOptionDto(null, Guid.NewGuid(), "COLOR", "Color", "STRING", "SELECT", 1, new List<VariantConfigurationOptionValueDto>()),
                    new VariantConfigurationOptionDto(null, Guid.NewGuid(), "COLOR", "Colour", "STRING", "SELECT", 2, new List<VariantConfigurationOptionValueDto>())
                },
                new List<VariantConfigurationVariantDto>(),
                new List<VariantConfigurationDeletedCombinationDto>())
        };

        var error = _validator.ValidateStepSaveDraft(request);
        
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "options[1].optionCode");
        Assert.Contains(error.FieldErrors!, e => e.Code == "product.duplicate_attribute");
    }

    [Fact]
    public void ValidateSaveDraft_DuplicateValue_ReturnsFieldError()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(
                new List<VariantConfigurationOptionDto>
                {
                    new VariantConfigurationOptionDto(null, Guid.NewGuid(), "COLOR", "Color", "STRING", "SELECT", 1, 
                        new List<VariantConfigurationOptionValueDto>
                        {
                            new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), "RED", "Red", "Red", "#FF0000", 1, null),
                            new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), "RED", "Rouge", "Rouge", "#FF0000", 2, null)
                        })
                },
                new List<VariantConfigurationVariantDto>(), 
                new List<VariantConfigurationDeletedCombinationDto>())
        };

        var error = _validator.ValidateStepSaveDraft(request);
        
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "options[0].values[1]");
        Assert.Contains(error.FieldErrors!, e => e.Code == "product.duplicate_option_value");
    }

    [Fact]
    public void ValidateSaveDraft_DuplicateClientCombinationKey_ReturnsFieldError()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(
                new List<VariantConfigurationOptionDto>(),
                new List<VariantConfigurationVariantDto>
                {
                    new VariantConfigurationVariantDto("key1", null, null, null, null, null, true, null, null, new List<VariantConfigurationSelectedValueDto>()),
                    new VariantConfigurationVariantDto("key1", null, null, null, null, null, true, null, null, new List<VariantConfigurationSelectedValueDto>())
                }, new List<VariantConfigurationDeletedCombinationDto>())
        };

        var error = _validator.ValidateStepSaveDraft(request);
        
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "variants[1].clientCombinationKey");
    }

    [Fact]
    public void ValidateSaveAndContinue_MissingConfiguration_Fails()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT"
        };
        
        var error = _validator.ValidateStepSaveAndContinue(request);
        
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "variantConfiguration.options");
    }

    [Fact]
    public void ValidateSaveAndContinue_OverMaxCombinations_Fails()
    {
        var options = new List<VariantConfigurationOptionDto>();
        for (int i = 0; i < 3; i++)
        {
            var values = new List<VariantConfigurationOptionValueDto>();
            for (int j = 0; j < 5; j++) // 5 * 5 * 5 = 125
            {
                values.Add(new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), $"VAL{i}_{j}", "Val", "Val", null, j, null));
            }
            options.Add(new VariantConfigurationOptionDto(null, Guid.NewGuid(), $"OPT{i}", "Opt", "STRING", "SELECT", i, values));
        }

        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(options, 
                new List<VariantConfigurationVariantDto>
                {
                    new VariantConfigurationVariantDto("key", null, null, null, null, null, true, null, null, new List<VariantConfigurationSelectedValueDto>())
                }, null)
        };
        
        var error = _validator.ValidateStepSaveAndContinue(request);
        
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "variantConfiguration");
        Assert.Contains(error.FieldErrors!, e => e.Code == "product.max_variants_exceeded");
    }
    
    [Fact]
    public void ValidateSaveAndContinue_NoIncludedVariant_Fails()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(
                new List<VariantConfigurationOptionDto>
                {
                    new VariantConfigurationOptionDto(null, Guid.NewGuid(), "OPT", "Opt", "STRING", "SELECT", 1, 
                        new List<VariantConfigurationOptionValueDto>
                        {
                            new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), "VAL", "Val", "Val", null, 1, null)
                        })
                },
                new List<VariantConfigurationVariantDto>
                {
                    new VariantConfigurationVariantDto("key1", null, null, null, null, null, false, null, null, new List<VariantConfigurationSelectedValueDto>())
                }, null)
        };

        var error = _validator.ValidateStepSaveAndContinue(request);
        
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "variantConfiguration.variants");
        Assert.Contains(error.FieldErrors!, e => e.Code == "product.included_variant_required");
    }
}
