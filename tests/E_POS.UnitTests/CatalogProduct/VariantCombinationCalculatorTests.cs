using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class VariantCombinationCalculatorTests
{
    [Fact]
    public void TryCalculateCombinationCount_ColorOnly3_Returns3()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([3], out var count, out var failure);

        Assert.True(success);
        Assert.Equal(3, count);
        Assert.Null(failure);
    }

    [Fact]
    public void TryCalculateCombinationCount_Color3Capacity2_Returns6()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([3, 2], out var count, out _);

        Assert.True(success);
        Assert.Equal(6, count);
    }

    [Fact]
    public void TryCalculateCombinationCount_Color3Size4Material2_Returns24()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([3, 4, 2], out var count, out _);

        Assert.True(success);
        Assert.Equal(24, count);
    }

    [Fact]
    public void TryCalculateCombinationCount_SingleValueEach_Returns1()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([1, 1], out var count, out _);

        Assert.True(success);
        Assert.Equal(1, count);
    }

    [Fact]
    public void TryCalculateCombinationCount_10x10_Returns100()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([10, 10], out var count, out _);

        Assert.True(success);
        Assert.Equal(100, count);
        Assert.Equal(ProductConstants.MaxVariantCombinationsPerProduct, count);
    }

    [Fact]
    public void TryCalculateCombinationCount_10x11_FailsMaximum()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([10, 11], out _, out var failure);

        Assert.False(success);
        Assert.Equal(VariantCombinationCalculator.CombinationCountFailure.ExceedsMaximum, failure);
    }

    [Fact]
    public void TryCalculateCombinationCount_ZeroValues_FailsIncomplete()
    {
        var success = VariantCombinationCalculator.TryCalculateCombinationCount([3, 0], out _, out var failure);

        Assert.False(success);
        Assert.Equal(VariantCombinationCalculator.CombinationCountFailure.IncompleteConfiguration, failure);
    }
}

public class VariantConfigurationCombinationGeneratorTests
{
    private static VariantConfigurationOptionDto BuildOption(
        string code,
        params string[] values)
    {
        return new VariantConfigurationOptionDto(
            null,
            Guid.NewGuid(),
            code,
            code,
            "SELECT",
            "SELECT",
            0,
            values.Select((value, index) => new VariantConfigurationOptionValueDto(
                null,
                Guid.NewGuid(),
                value.ToUpperInvariant(),
                value,
                value,
                null,
                index,
                null)).ToList());
    }

    [Fact]
    public void GenerateAndReconcile_ColorCapacityMatrix_GeneratesSixUniqueCombinations()
    {
        var colorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var capacityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var colors = new[] { "Red", "Blue", "Black" };
        var capacities = new[] { "128GB", "256GB" };

        var colorOption = new VariantConfigurationOptionDto(
            null,
            colorId,
            "COLOR",
            "Color",
            "SWATCH",
            "SELECT",
            0,
            colors.Select((value, index) => new VariantConfigurationOptionValueDto(
                null,
                Guid.NewGuid(),
                value.ToUpperInvariant(),
                value,
                value,
                null,
                index,
                null)).ToList());

        var capacityOption = new VariantConfigurationOptionDto(
            null,
            capacityId,
            "CAPACITY",
            "Capacity",
            "SELECT",
            "SELECT",
            1,
            capacities.Select((value, index) => new VariantConfigurationOptionValueDto(
                null,
                Guid.NewGuid(),
                value.ToUpperInvariant(),
                value,
                value,
                null,
                index,
                null)).ToList());

        var input = new VariantConfigurationDto(
            [colorOption, capacityOption],
            [],
            []);

        var result = VariantConfigurationCombinationGenerator.GenerateAndReconcile(input);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Configuration);
        Assert.Equal(6, result.Configuration!.Variants.Count);
        var expectedLabels = new[]
        {
            "Red / 128GB",
            "Red / 256GB",
            "Blue / 128GB",
            "Blue / 256GB",
            "Black / 128GB",
            "Black / 256GB",
        };
        Assert.Equal(
            expectedLabels.OrderBy(x => x).ToArray(),
            result.Configuration.Variants.Select(v => v.CombinationLabel).OrderBy(x => x).ToArray());
        Assert.Equal(result.Configuration.Variants.Count, result.Configuration.Variants.Select(v => v.ClientCombinationKey).Distinct().Count());
    }

    [Fact]
    public void GenerateAndReconcile_IncompleteConfiguration_Fails()
    {
        var input = new VariantConfigurationDto(
            [BuildOption("Color", "Red", "Blue", "Black"), BuildOption("Capacity")],
            [],
            []);

        var result = VariantConfigurationCombinationGenerator.GenerateAndReconcile(input);

        Assert.False(result.Succeeded);
        Assert.Equal("product.option_values_required", result.ErrorCode);
    }

    [Fact]
    public void GenerateAndReconcile_ExceedsMaximum_Fails()
    {
        var optionAValues = Enumerable.Range(1, 10).Select(i => $"C{i}").ToArray();
        var optionBValues = Enumerable.Range(1, 11).Select(i => $"V{i}").ToArray();
        var input = new VariantConfigurationDto(
            [BuildOption("Color", optionAValues), BuildOption("Size", optionBValues)],
            [],
            []);

        var result = VariantConfigurationCombinationGenerator.GenerateAndReconcile(input);

        Assert.False(result.Succeeded);
        Assert.Equal("product.max_variants_exceeded", result.ErrorCode);
    }
}

public class TenantAdminProductVariantConfigurationValidatorTests
{
    private static SaveProductDraftRequest BuildVariantContinueRequest(
        IReadOnlyList<VariantConfigurationOptionDto> options,
        IReadOnlyList<VariantConfigurationVariantDto>? variants = null)
    {
        return new SaveProductDraftRequest
        {
            ProductStructure = "VARIANT",
            CurrentSetupStep = 4,
            WizardAction = "SAVE_AND_CONTINUE",
            VariantConfiguration = new VariantConfigurationDto(
                options,
                variants ?? [],
                []),
        };
    }

    [Fact]
    public void ValidateProductConfigurationContinue_ZeroValues_FailsIncomplete()
    {
        var request = BuildVariantContinueRequest([
            new VariantConfigurationOptionDto(
                null,
                Guid.NewGuid(),
                "COLOR",
                "Color",
                "SWATCH",
                "SELECT",
                0,
                []),
        ]);

        var error = TenantAdminProductRequestValidator.ValidateProductConfigurationContinue(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Code == "product.option_values_required");
    }

    [Fact]
    public void ValidateProductConfigurationContinue_ExceedsMaximum_Fails()
    {
        var optionA = new VariantConfigurationOptionDto(
            null,
            Guid.NewGuid(),
            "A",
            "A",
            "SELECT",
            "SELECT",
            0,
            Enumerable.Range(1, 10)
                .Select(i => new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), $"A{i}", $"A{i}", $"A{i}", null, i, null))
                .ToList());
        var optionB = new VariantConfigurationOptionDto(
            null,
            Guid.NewGuid(),
            "B",
            "B",
            "SELECT",
            "SELECT",
            1,
            Enumerable.Range(1, 11)
                .Select(i => new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), $"B{i}", $"B{i}", $"B{i}", null, i, null))
                .ToList());

        var request = BuildVariantContinueRequest([optionA, optionB]);

        var error = TenantAdminProductRequestValidator.ValidateProductConfigurationContinue(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Code == "product.max_variants_exceeded");
    }

    [Fact]
    public void ValidateProductConfigurationContinue_ValueNotOwnedByAttribute_Fails()
    {
        var colorTemplateId = Guid.NewGuid();
        var redValueId = Guid.NewGuid();
        var foreignValueId = Guid.NewGuid();

        var options = new[]
        {
            new VariantConfigurationOptionDto(
                null,
                colorTemplateId,
                "COLOR",
                "Color",
                "SWATCH",
                "SELECT",
                0,
                [
                    new VariantConfigurationOptionValueDto(null, redValueId, "RED", "Red", "Red", null, 0, null),
                ]),
        };

        var variants = new[]
        {
            new VariantConfigurationVariantDto(
                "key",
                null,
                null,
                "hash",
                "Red",
                "Red",
                true,
                null,
                null,
                [
                    new VariantConfigurationSelectedValueDto(colorTemplateId, foreignValueId, "Color", "Red"),
                ]),
        };

        var request = BuildVariantContinueRequest(options, variants);
        var error = TenantAdminProductRequestValidator.ValidateProductConfigurationContinue(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Code == "product.option_value_not_owned_by_attribute");
    }

    [Fact]
    public void ValidateProductConfigurationContinue_SimpleProduct_DoesNotRunVariantRules()
    {
        var request = new SaveProductDraftRequest
        {
            ProductStructure = "SIMPLE",
            CurrentSetupStep = 4,
            WizardAction = "SAVE_AND_CONTINUE",
        };

        var error = TenantAdminProductRequestValidator.ValidateProductConfigurationContinue(request);

        Assert.Null(error);
    }
}

public class ProductVariantGenerationServiceTests
{
    [Fact]
    public async Task GenerateAndReconcileVariantsAsync_IgnoresClientVariantListAndGeneratesFromOptions()
    {
        var colorId = Guid.NewGuid();
        var redId = Guid.NewGuid();
        var blueId = Guid.NewGuid();
        var capacityId = Guid.NewGuid();
        var gb128Id = Guid.NewGuid();
        var gb256Id = Guid.NewGuid();

        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(
                    null,
                    colorId,
                    "COLOR",
                    "Color",
                    "SWATCH",
                    "SELECT",
                    0,
                    [
                        new VariantConfigurationOptionValueDto(null, redId, "RED", "Red", "Red", null, 0, null),
                        new VariantConfigurationOptionValueDto(null, blueId, "BLUE", "Blue", "Blue", null, 1, null),
                    ]),
                new VariantConfigurationOptionDto(
                    null,
                    capacityId,
                    "CAPACITY",
                    "Capacity",
                    "SELECT",
                    "SELECT",
                    1,
                    [
                        new VariantConfigurationOptionValueDto(null, gb128Id, "128GB", "128GB", "128GB", null, 0, null),
                        new VariantConfigurationOptionValueDto(null, gb256Id, "256GB", "256GB", "256GB", null, 1, null),
                    ]),
            ],
            [
                new VariantConfigurationVariantDto(
                    "fake",
                    null,
                    null,
                    "fake",
                    "Only One",
                    "Only One",
                    true,
                    null,
                    null,
                    []),
            ],
            []);

        var service = new ProductVariantGenerationService();
        var result = await service.GenerateAndReconcileVariantsAsync(Guid.NewGuid(), Guid.NewGuid(), config, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Variants.Count);
    }
}
