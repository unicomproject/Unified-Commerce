using System;
using System.Collections.Generic;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class TenantAdminProductRequestValidatorBundleConfigurationTests
{
    private readonly TenantAdminProductRequestValidator _validator = new();

    private BundleConfigurationDto ValidBundleConfig(int count)
    {
        var components = new List<BundleComponentDto>();
        for (int i = 0; i < count; i++)
        {
            components.Add(new BundleComponentDto(
                null,
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                1m,
                i
            ));
        }
        return new BundleConfigurationDto(null, components);
    }

    [Fact]
    public void Validate_VariantStructure_WithVariantConfig_Passes()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            VariantConfiguration = new VariantConfigurationDto(null, null, null)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_BundleStructure_WithBundleConfig_Passes()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(0)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_BundleStructure_WithVariantConfigOnly_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            VariantConfiguration = new VariantConfigurationDto(null, null, null)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "variantConfiguration");
    }

    [Fact]
    public void Validate_BundleStructure_WithBothConfigs_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(0),
            VariantConfiguration = new VariantConfigurationDto(null, null, null)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "variantConfiguration");
    }

    [Fact]
    public void Validate_VariantStructure_WithBundleConfigOnly_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "VARIANT",
            BundleConfiguration = ValidBundleConfig(0)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration");
    }

    [Fact]
    public void Validate_SaveDraft_ZeroComponents_Passes()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(0)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_SaveDraft_OneComponent_Passes()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(1)
        };
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_SaveContinue_ZeroComponents_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(0)
        };
        var error = _validator.ValidateStepSaveAndContinue(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components");
        Assert.Contains(error.FieldErrors!, e => e.Code == "product.bundle.minimum_components_required");
    }

    [Fact]
    public void Validate_SaveContinue_OneComponent_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(1)
        };
        var error = _validator.ValidateStepSaveAndContinue(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components");
    }

    [Fact]
    public void Validate_SaveContinue_TwoComponents_Passes()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = ValidBundleConfig(2)
        };
        var error = _validator.ValidateStepSaveAndContinue(request);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_QuantityZero_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = new BundleConfigurationDto(
                null,
                new List<BundleComponentDto>
                {
                    new BundleComponentDto(null, Guid.NewGuid(), null, Guid.NewGuid(), 0m, 0)
                }
            )
        };
        
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components[0].requiredQuantity");
    }

    [Fact]
    public void Validate_QuantityNegative_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = new BundleConfigurationDto(
                null,
                new List<BundleComponentDto>
                {
                    new BundleComponentDto(null, Guid.NewGuid(), null, Guid.NewGuid(), -5m, 0)
                }
            )
        };
        
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components[0].requiredQuantity");
    }

    [Fact]
    public void Validate_MissingComponentProductId_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = new BundleConfigurationDto(
                null,
                new List<BundleComponentDto>
                {
                    new BundleComponentDto(null, Guid.Empty, null, Guid.NewGuid(), 1m, 0)
                }
            )
        };
        
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components[0].componentProductId");
    }

    [Fact]
    public void Validate_MissingComponentUomId_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = new BundleConfigurationDto(
                null,
                new List<BundleComponentDto>
                {
                    new BundleComponentDto(null, Guid.NewGuid(), null, Guid.Empty, 1m, 0)
                }
            )
        };
        
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components[0].componentUomId");
    }

    [Fact]
    public void Validate_InvalidSortOrder_Rejects()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 4,
            ProductStructure = "BUNDLE",
            BundleConfiguration = new BundleConfigurationDto(
                null,
                new List<BundleComponentDto>
                {
                    new BundleComponentDto(null, Guid.NewGuid(), null, Guid.NewGuid(), 1m, -1)
                }
            )
        };
        
        var error = _validator.ValidateStepSaveDraft(request);
        Assert.NotNull(error);
        Assert.Contains(error.FieldErrors!, e => e.Field == "bundleConfiguration.components[0].sortOrder");
    }
}
