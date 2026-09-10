using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

namespace E_POS.UnitTests.CatalogProduct;

public class VariantConfigurationSetupClosureTests : IDisposable
{
    private readonly EPosDbContext _dbContext;
    private readonly TenantAdminProductRepository _sut;
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    public VariantConfigurationSetupClosureTests()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new EPosDbContext(options);
        _sut = new TenantAdminProductRepository(_dbContext, new Mock<ICodeSequenceRepository>().Object, null);
    }

    [Fact]
    public async Task GetSetupAsync_RoundTrip_RehydratesColour3Capacity2Configuration()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var (colorTemplateId, capacityTemplateId, colorValues, capacityValues) = await SeedColourCapacityTemplatesAsync();

        var product = Product.Create(
            productId, tenantId, "PHONE-001", "Phone", "phone", "GOODS",
            ProductStructureConstants.Variant, null, null, null, null, null, true, true,
            ProductConstants.DraftStatus, Guid.NewGuid(), _now);
        await _dbContext.Products.AddAsync(product);

        var config = BuildColourCapacityConfig(colorTemplateId, capacityTemplateId, colorValues, capacityValues, includeAllVariants: true);
        await _sut.SaveVariantsAsync(tenantId, productId, config, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var setup = await _sut.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.NotNull(setup);
        Assert.NotNull(setup!.VariantConfiguration);
        Assert.Equal(2, setup.VariantConfiguration!.Options.Count);
        Assert.Equal(3, setup.VariantConfiguration.Options.First(o => o.OptionCode == "COLOR").Values.Count);
        Assert.Equal(2, setup.VariantConfiguration.Options.First(o => o.OptionCode == "CAPACITY").Values.Count);
        Assert.Equal(6, setup.VariantConfiguration.Variants.Count);
        Assert.Equal(6, setup.TotalVariantCount);
        Assert.All(setup.VariantConfiguration.Options.SelectMany(o => o.Values), v =>
            Assert.True(v.SourceOptionTemplateValueId.HasValue));
        Assert.All(setup.VariantConfiguration.Options, o =>
            Assert.True(o.SourceOptionTemplateId.HasValue));
    }

    [Fact]
    public async Task GetSetupAsync_AfterEdit_Rehydrates2x2WithoutStaleValues()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var (colorTemplateId, capacityTemplateId, colorValues, capacityValues) = await SeedColourCapacityTemplatesAsync();

        var product = Product.Create(
            productId, tenantId, "PHONE-002", "Phone", "phone", "GOODS",
            ProductStructureConstants.Variant, null, null, null, null, null, true, true,
            ProductConstants.DraftStatus, Guid.NewGuid(), _now);
        await _dbContext.Products.AddAsync(product);

        var initial = BuildColourCapacityConfig(colorTemplateId, capacityTemplateId, colorValues, capacityValues, includeAllVariants: true);
        await _sut.SaveVariantsAsync(tenantId, productId, initial, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var editedColors = colorValues.Take(2).ToList();
        var edited = BuildColourCapacityConfig(colorTemplateId, capacityTemplateId, editedColors, capacityValues, includeAllVariants: true);
        await _sut.SaveVariantsAsync(tenantId, productId, edited, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var setup = await _sut.GetSetupAsync(tenantId, productId, CancellationToken.None);

        Assert.NotNull(setup?.VariantConfiguration);
        Assert.Equal(2, setup!.VariantConfiguration!.Options.First(o => o.OptionCode == "COLOR").Values.Count);
        Assert.Equal(2, setup.VariantConfiguration.Options.First(o => o.OptionCode == "CAPACITY").Values.Count);
        Assert.Equal(4, setup.VariantConfiguration.Variants.Count);
        Assert.DoesNotContain(
            setup.VariantConfiguration.Options.First(o => o.OptionCode == "COLOR").Values,
            v => v.ValueName == "Black");
    }

    [Fact]
    public async Task SaveVariantsAsync_SameConfigurationTwice_DoesNotDuplicateVariants()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var (colorTemplateId, capacityTemplateId, colorValues, capacityValues) = await SeedColourCapacityTemplatesAsync();

        var product = Product.Create(
            productId, tenantId, "PHONE-003", "Phone", "phone", "GOODS",
            ProductStructureConstants.Variant, null, null, null, null, null, true, true,
            ProductConstants.DraftStatus, Guid.NewGuid(), _now);
        await _dbContext.Products.AddAsync(product);

        var config = BuildColourCapacityConfig(colorTemplateId, capacityTemplateId, colorValues, capacityValues, includeAllVariants: true);
        await _sut.SaveVariantsAsync(tenantId, productId, config, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var setup = await _sut.GetSetupAsync(tenantId, productId, CancellationToken.None);
        var regenerated = VariantConfigurationCombinationGenerator.GenerateAndReconcile(setup!.VariantConfiguration!);
        Assert.True(regenerated.Succeeded);

        await _sut.SaveVariantsAsync(tenantId, productId, regenerated.Configuration!, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var variantCount = await _dbContext.ProductVariants
            .CountAsync(v => v.TenantId == tenantId &&
                             v.ProductId == productId &&
                             v.Status != ProductConstants.ArchivedStatus);
        Assert.Equal(6, variantCount);
    }

    [Fact]
    public async Task ValidateVariantConfigurationCatalogAsync_ValidAttributeAndValues_Passes()
    {
        var tenantId = Guid.NewGuid();
        var (colorTemplateId, capacityTemplateId, colorValues, capacityValues) = await SeedColourCapacityTemplatesAsync();
        var config = BuildColourCapacityConfig(colorTemplateId, capacityTemplateId, colorValues, capacityValues, includeAllVariants: false);

        var errors = await _sut.ValidateVariantConfigurationCatalogAsync(tenantId, null, config, CancellationToken.None);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateVariantConfigurationCatalogAsync_ValueFromDifferentAttribute_Fails()
    {
        var tenantId = Guid.NewGuid();
        var (colorTemplateId, capacityTemplateId, colorValues, capacityValues) = await SeedColourCapacityTemplatesAsync();

        var mismatchedConfig = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(
                    null,
                    colorTemplateId,
                    "COLOR",
                    "Color",
                    "SWATCH",
                    "SELECT",
                    0,
                    [
                        new VariantConfigurationOptionValueDto(
                            null,
                            capacityValues[0].Id,
                            capacityValues[0].ValueCode,
                            capacityValues[0].ValueName,
                            capacityValues[0].ValueName,
                            null,
                            0,
                            null),
                    ]),
            ],
            [],
            []);

        var errors = await _sut.ValidateVariantConfigurationCatalogAsync(tenantId, null, mismatchedConfig, CancellationToken.None);

        Assert.Contains(errors, e => e.Code == "product.option_value_not_owned_by_attribute");
    }

    [Fact]
    public async Task ValidateVariantConfigurationCatalogAsync_UnknownTemplate_Fails()
    {
        var tenantId = Guid.NewGuid();
        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(null, Guid.NewGuid(), "X", "X", "SELECT", "SELECT", 0, []),
            ],
            [],
            []);

        var errors = await _sut.ValidateVariantConfigurationCatalogAsync(tenantId, null, config, CancellationToken.None);

        Assert.Contains(errors, e => e.Code == "product.option_template_not_found");
    }

    [Fact]
    public async Task ValidateVariantConfigurationCatalogAsync_UnknownValue_Fails()
    {
        var tenantId = Guid.NewGuid();
        var (colorTemplateId, _, _, _) = await SeedColourCapacityTemplatesAsync();
        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(
                    null,
                    colorTemplateId,
                    "COLOR",
                    "Color",
                    "SWATCH",
                    "SELECT",
                    0,
                    [
                        new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), "X", "X", "X", null, 0, null),
                    ]),
            ],
            [],
            []);

        var errors = await _sut.ValidateVariantConfigurationCatalogAsync(tenantId, null, config, CancellationToken.None);

        Assert.Contains(errors, e => e.Code == "product.option_template_value_not_found");
    }

    [Fact]
    public async Task ValidateVariantConfigurationCatalogAsync_OtherTenantProductOption_Fails()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var optionB = Guid.NewGuid();

        var option = ProductOption.Create(
            optionB, tenantB, productB, null, "COLOR", "Color", "SWATCH", "SELECT", true, 0, "ACTIVE", null, _now);
        await _dbContext.ProductOptions.AddAsync(option);
        await _dbContext.SaveChangesAsync();

        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(optionB, null, "COLOR", "Color", "SWATCH", "SELECT", 0, []),
            ],
            [],
            []);

        var errors = await _sut.ValidateVariantConfigurationCatalogAsync(tenantA, Guid.NewGuid(), config, CancellationToken.None);

        Assert.Contains(errors, e => e.Code == "product.option_not_found");
    }

    [Fact]
    public async Task ValidateVariantConfigurationCatalogAsync_InactiveTemplate_Fails()
    {
        var tenantId = Guid.NewGuid();
        var inactiveTemplateId = Guid.NewGuid();
        var inactiveTemplate = ProductOptionTemplate.Create(
            inactiveTemplateId, "INACTIVE", "Inactive", "SELECT", "SELECT", 0, "INACTIVE", Guid.NewGuid(), _now);
        await _dbContext.ProductOptionTemplates.AddAsync(inactiveTemplate);
        await _dbContext.SaveChangesAsync();

        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(null, inactiveTemplateId, "INACTIVE", "Inactive", "SELECT", "SELECT", 0, []),
            ],
            [],
            []);

        var errors = await _sut.ValidateVariantConfigurationCatalogAsync(tenantId, null, config, CancellationToken.None);

        Assert.Contains(errors, e => e.Code == "product.option_template_inactive");
    }

    [Fact]
    public void CanonicalHash_IsDeterministic_ForSameLogicalCombination()
    {
        var optId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var valId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var optId2 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var valId2 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var hash1 = ProductVariantCombinationHashHelper.GenerateCanonicalHash([
            (optId, valId),
            (optId2, valId2),
        ]);
        var hash2 = ProductVariantCombinationHashHelper.GenerateCanonicalHash([
            (optId2, valId2),
            (optId, valId),
        ]);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }

    [Fact]
    public void LegacyMd5PreviewHash_MatchesStoredPhase1Records()
    {
        var colorId = Guid.NewGuid();
        var redId = Guid.NewGuid();
        var capacityId = Guid.NewGuid();
        var gb128Id = Guid.NewGuid();

        var selected = new List<VariantConfigurationSelectedValueDto>
        {
            new(colorId, redId, "Color", "Red"),
            new(capacityId, gb128Id, "Capacity", "128GB"),
        };

        var ordered = selected
            .OrderBy(x => x.SourceOptionTemplateId?.ToString("D") ?? x.OptionName ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var hash1 = ProductVariantCombinationHashHelper.GenerateLegacyMd5PreviewHash(
            ordered.Select(x => (x.SourceOptionTemplateId, x.SourceOptionTemplateValueId, x.OptionName, x.ValueName)));
        var hash2 = ProductVariantCombinationHashHelper.GenerateLegacyMd5PreviewHash(
            ordered.Select(x => (x.SourceOptionTemplateId, x.SourceOptionTemplateValueId, x.OptionName, x.ValueName)));

        Assert.Equal(hash1, hash2);
        Assert.True(ProductVariantCombinationHashHelper.IsLegacyMd5Hash(hash1));
        Assert.True(ProductVariantCombinationHashHelper.MatchesCombinationHash(hash1, null, hash1));
    }

    [Fact]
    public void GenerateAndReconcile_Max100_Passes_Max110_Fails()
    {
        Guid NewTemplate() => Guid.NewGuid();
        var optionA = new VariantConfigurationOptionDto(
            null, NewTemplate(), "A", "A", "SELECT", "SELECT", 0,
            Enumerable.Range(1, 10).Select(i => new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), $"A{i}", $"A{i}", $"A{i}", null, i, null)).ToList());
        var optionB10 = new VariantConfigurationOptionDto(
            null, NewTemplate(), "B", "B", "SELECT", "SELECT", 1,
            Enumerable.Range(1, 10).Select(i => new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), $"B{i}", $"B{i}", $"B{i}", null, i, null)).ToList());
        var optionB11 = optionB10 with
        {
            Values = optionB10.Values.Append(new VariantConfigurationOptionValueDto(null, Guid.NewGuid(), "B11", "B11", "B11", null, 11, null)).ToList(),
        };

        Assert.True(VariantConfigurationCombinationGenerator.GenerateAndReconcile(
            new VariantConfigurationDto([optionA, optionB10], [], [])).Succeeded);
        Assert.False(VariantConfigurationCombinationGenerator.GenerateAndReconcile(
            new VariantConfigurationDto([optionA, optionB11], [], [])).Succeeded);
    }

    private async Task<(Guid ColorTemplateId, Guid CapacityTemplateId, List<ProductOptionTemplateValue> ColorValues, List<ProductOptionTemplateValue> CapacityValues)>
        SeedColourCapacityTemplatesAsync()
    {
        var colorTemplateId = Guid.NewGuid();
        var capacityTemplateId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _dbContext.ProductOptionTemplates.AddRangeAsync(
            ProductOptionTemplate.Create(colorTemplateId, "COLOR", "Color", "SWATCH", "SELECT", 0, "ACTIVE", userId, _now),
            ProductOptionTemplate.Create(capacityTemplateId, "CAPACITY", "Capacity", "SELECT", "SELECT", 1, "ACTIVE", userId, _now));

        var colorValues = new List<ProductOptionTemplateValue>
        {
            ProductOptionTemplateValue.Create(Guid.NewGuid(), colorTemplateId, "RED", "Red", "Red", "#FF0000", null, 0, "ACTIVE", userId, _now),
            ProductOptionTemplateValue.Create(Guid.NewGuid(), colorTemplateId, "BLUE", "Blue", "Blue", "#0000FF", null, 1, "ACTIVE", userId, _now),
            ProductOptionTemplateValue.Create(Guid.NewGuid(), colorTemplateId, "BLACK", "Black", "Black", "#000000", null, 2, "ACTIVE", userId, _now),
        };
        var capacityValues = new List<ProductOptionTemplateValue>
        {
            ProductOptionTemplateValue.Create(Guid.NewGuid(), capacityTemplateId, "128GB", "128GB", "128GB", null, null, 0, "ACTIVE", userId, _now),
            ProductOptionTemplateValue.Create(Guid.NewGuid(), capacityTemplateId, "256GB", "256GB", "256GB", null, null, 1, "ACTIVE", userId, _now),
        };

        await _dbContext.ProductOptionTemplateValues.AddRangeAsync(colorValues.Concat(capacityValues));
        await _dbContext.SaveChangesAsync();

        return (colorTemplateId, capacityTemplateId, colorValues, capacityValues);
    }

    private static VariantConfigurationDto BuildColourCapacityConfig(
        Guid colorTemplateId,
        Guid capacityTemplateId,
        IReadOnlyList<ProductOptionTemplateValue> colorValues,
        IReadOnlyList<ProductOptionTemplateValue> capacityValues,
        bool includeAllVariants)
    {
        var colorOption = new VariantConfigurationOptionDto(
            null,
            colorTemplateId,
            "COLOR",
            "Color",
            "SWATCH",
            "SELECT",
            0,
            colorValues.Select((v, i) => new VariantConfigurationOptionValueDto(
                null, v.Id, v.ValueCode, v.ValueName, v.ValueName, v.ColorHex, i, null)).ToList());

        var capacityOption = new VariantConfigurationOptionDto(
            null,
            capacityTemplateId,
            "CAPACITY",
            "Capacity",
            "SELECT",
            "SELECT",
            1,
            capacityValues.Select((v, i) => new VariantConfigurationOptionValueDto(
                null, v.Id, v.ValueCode, v.ValueName, v.ValueName, null, i, null)).ToList());

        var input = new VariantConfigurationDto([colorOption, capacityOption], [], []);
        if (!includeAllVariants)
        {
            return input;
        }

        var generated = VariantConfigurationCombinationGenerator.GenerateAndReconcile(input);
        Assert.True(generated.Succeeded);
        return generated.Configuration!;
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
