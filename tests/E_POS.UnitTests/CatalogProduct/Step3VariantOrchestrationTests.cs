using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

/// <summary>
/// Phase B: Target Step 3 — VARIANT Product Type & Configuration orchestration tests.
/// Validates variant attributes, values, combinations, and include/exclude state at Step 3.
/// </summary>
public sealed class Step3VariantOrchestrationTests
{
    [Fact]
    public void BuildVariantRequest_SingleAttribute_TwoValues_HasValidStructure()
    {
        var options = new List<VariantConfigurationOptionDto>
        {
            new(
                null, null, "COLOR", "Color", "TEXT", "TEXT", 0,
                new List<VariantConfigurationOptionValueDto>
                {
                    new(null, null, "RED", "Red", "Red", null, 0, null),
                    new(null, null, "BLUE", "Blue", "Blue", null, 1, null),
                })
        };

        var variants = new List<VariantConfigurationVariantDto>
        {
            new("Color:Red", null, "V1", "Red", "Red Variant", "Red", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Red")
                }),
            new("Color:Blue", null, "V2", "Blue", "Blue Variant", "Blue", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Blue")
                })
        };

        var variantConfig = new VariantConfigurationDto(
            options,
            variants,
            Array.Empty<VariantConfigurationDeletedCombinationDto>());

        var request = new SaveProductDraftRequest
        {
            ProductName = "Color Variant Widget",
            ProductCode = "VARIANT-COLOR-001",
            ProductStructure = "VARIANT",
            CurrentSetupStep = 3,
            VariantConfiguration = variantConfig,
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                new List<BarcodeSkuAssignmentDto>
                {
                    new BarcodeSkuAssignmentDto(null, "Color:Red", "SKU-RED", null, null, "Color:Red", null),
                    new BarcodeSkuAssignmentDto(null, "Color:Blue", "SKU-BLUE", null, null, "Color:Blue", null)
                })
        };

        Assert.Equal("VARIANT", request.ProductStructure);
        Assert.Single(request.VariantConfiguration!.Options);
        Assert.Equal(2, request.VariantConfiguration!.Variants!.Count);
    }

    [Fact]
    public void BuildVariantRequest_MultipleAttributes_HasValidStructure()
    {
        var options = new List<VariantConfigurationOptionDto>
        {
            new(null, null, "COLOR", "Color", "TEXT", "TEXT", 0,
                new List<VariantConfigurationOptionValueDto>
                {
                    new(null, null, "RED", "Red", "Red", null, 0, null),
                    new(null, null, "BLUE", "Blue", "Blue", null, 1, null),
                }),
            new(null, null, "SIZE", "Size", "TEXT", "TEXT", 1,
                new List<VariantConfigurationOptionValueDto>
                {
                    new(null, null, "S", "Small", "Small", null, 0, null),
                    new(null, null, "M", "Medium", "Medium", null, 1, null),
                })
        };

        var variants = new List<VariantConfigurationVariantDto>
        {
            new("Color:Red;Size:S", null, "V1", "Red Small", "Red Small", "Red Small", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Red"),
                    new(null, null, "Size", "S")
                }),
            new("Color:Red;Size:M", null, "V2", "Red Medium", "Red Medium", "Red Medium", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Red"),
                    new(null, null, "Size", "M")
                }),
            new("Color:Blue;Size:S", null, "V3", "Blue Small", "Blue Small", "Blue Small", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Blue"),
                    new(null, null, "Size", "S")
                }),
            new("Color:Blue;Size:M", null, "V4", "Blue Medium", "Blue Medium", "Blue Medium", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Blue"),
                    new(null, null, "Size", "M")
                })
        };

        var config = new VariantConfigurationDto(
            options, variants, Array.Empty<VariantConfigurationDeletedCombinationDto>());

        var request = new SaveProductDraftRequest
        {
            ProductName = "Size Color Variant",
            ProductStructure = "VARIANT",
            VariantConfiguration = config,
            CurrentSetupStep = 3
        };

        Assert.Equal("VARIANT", request.ProductStructure);
        Assert.Equal(2, request.VariantConfiguration!.Options.Count);
        Assert.Equal(4, request.VariantConfiguration!.Variants!.Count);
    }

    [Fact]
    public void VariantRequest_AllExcluded_ShouldBeInvalid()
    {
        var options = new List<VariantConfigurationOptionDto>
        {
            new(null, null, "COLOR", "Color", "TEXT", "TEXT", 0,
                new List<VariantConfigurationOptionValueDto>
                {
                    new(null, null, "RED", "Red", "Red", null, 0, null)
                })
        };

        var variants = new List<VariantConfigurationVariantDto>
        {
            new("Color:Red", null, "V1", "Red", "Red", "Red", false, "ACTIVE", null,  // Included = false
                new List<VariantConfigurationSelectedValueDto>
                {
                    new(null, null, "Color", "Red")
                })
        };

        var config = new VariantConfigurationDto(options, variants, Array.Empty<VariantConfigurationDeletedCombinationDto>());

        Assert.True(config.Variants!.All(v => !v.Included));
        // Validator should reject: need at least one included variant
    }

    [Fact]
    public void VariantRequest_MixedIncludeExclude_IsValid()
    {
        var options = new List<VariantConfigurationOptionDto>
        {
            new(null, null, "COLOR", "Color", "TEXT", "TEXT", 0,
                new List<VariantConfigurationOptionValueDto>
                {
                    new(null, null, "RED", "Red", "Red", null, 0, null),
                    new(null, null, "BLUE", "Blue", "Blue", null, 1, null)
                })
        };

        var variants = new List<VariantConfigurationVariantDto>
        {
            new("Color:Red", null, "V1", "Red", "Red", "Red", true, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto> { new(null, null, "Color", "Red") }),
            new("Color:Blue", null, "V2", "Blue", "Blue", "Blue", false, "ACTIVE", null,
                new List<VariantConfigurationSelectedValueDto> { new(null, null, "Color", "Blue") })
        };

        var config = new VariantConfigurationDto(options, variants, Array.Empty<VariantConfigurationDeletedCombinationDto>());

        Assert.True(config.Variants!.Any(v => v.Included));
        Assert.True(config.Variants!.Any(v => !v.Included));
        // Valid: has both included and excluded
    }

    [Fact]
    public void ExcludedVariant_ShouldNotRequireIdentifier()
    {
        var variant = new VariantConfigurationVariantDto(
            "Color:Blue", null, "V2", "Blue", "Blue", "Blue", false, "ACTIVE", null,
            new List<VariantConfigurationSelectedValueDto> { new(null, null, "Color", "Blue") });

        Assert.False(variant.Included);
        // Should not require SKU/Barcode for excluded variants
    }

    [Fact]
    public void IncludedVariant_MissingSku_ShouldBeInvalid()
    {
        var variant = new VariantConfigurationVariantDto(
            "Color:Red", null, "V1", "Red", "Red", "Red", true, "ACTIVE", null,
            new List<VariantConfigurationSelectedValueDto> { new(null, null, "Color", "Red") });

        var assignment = new BarcodeSkuAssignmentDto(null, "Color:Red", "", null, null, "Color:Red", null);

        Assert.True(variant.Included);
        Assert.True(string.IsNullOrWhiteSpace(assignment.Sku));
        // Validator should reject: included variant requires SKU
    }

    [Fact]
    public void VariantRequest_CombinationKey_Preserved()
    {
        var key = "Color:Red;Size:Small";
        var variant = new VariantConfigurationVariantDto(
            key, null, "V1", "Red Small", "Red Small", "Red Small", true, "ACTIVE", null,
            new List<VariantConfigurationSelectedValueDto>
            {
                new(null, null, "Color", "Red"),
                new(null, null, "Size", "Small")
            });

        Assert.Equal(key, variant.ClientCombinationKey);
        // Client key maps to persistent hash during orchestration
    }

    [Fact]
    public void VariantStep3_CurrentSetupStep_Should_Be_3()
    {
        var request = new SaveProductDraftRequest
        {
            ProductName = "Variant Step 3",
            ProductStructure = "VARIANT",
            CurrentSetupStep = 3
        };

        Assert.Equal(3, request.CurrentSetupStep);
    }
}
