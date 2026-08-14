using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace E_POS.UnitTests.CatalogProduct;

public class TenantAdminProductOptionReconciliationTests : IDisposable
{
    private readonly EPosDbContext _dbContext;
    private readonly TenantAdminProductRepository _sut;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public TenantAdminProductOptionReconciliationTests()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new EPosDbContext(options);

        var codeSequenceMock = new Mock<ICodeSequenceRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _sut = new TenantAdminProductRepository(_dbContext, codeSequenceMock.Object, null);
    }

    private async Task SeedTemplateAsync(Guid templateId, Guid redValueId, Guid blueValueId)
    {
        var template = ProductOptionTemplate.Create(
            templateId,
            "COLOR",
            "Color",
            "SWATCH",
            "SELECT",
            1,
            "ACTIVE",
            Guid.NewGuid(),
            _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptionTemplates.AddAsync(template);

        var red = ProductOptionTemplateValue.Create(
            redValueId,
            templateId,
            "RED",
            "Red",
            "Red",
            "#FF0000",
            null,
            1,
            "ACTIVE",
            Guid.NewGuid(),
            _dateTimeProviderMock.Object.UtcNow);

        var blue = ProductOptionTemplateValue.Create(
            blueValueId,
            templateId,
            "BLU",
            "Blue",
            "Blue",
            "#0000FF",
            null,
            2,
            "ACTIVE",
            Guid.NewGuid(),
            _dateTimeProviderMock.Object.UtcNow);

        var pieceUom = UnitOfMeasure.Create(Guid.NewGuid(), null, "PIECE", "Piece", "UNIT", "Pcs", null, 1m, "ACTIVE", _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.UnitOfMeasures.AddAsync(pieceUom);

        await _dbContext.ProductOptionTemplateValues.AddRangeAsync(red, blue);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ApplyVariantConfiguration_ShouldCreateNewOptionsAndValues_WhenNoneExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var templateId = Guid.NewGuid();
        var redId = Guid.NewGuid();
        var blueId = Guid.NewGuid();
        await SeedTemplateAsync(templateId, redId, blueId);

        var product = Product.Create(
            productId,
            tenantId,
            "TSHIRT",
            "T-Shirt",
            "t-shirt",
            "GOODS",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            "ACTIVE",
            userId,
            _dateTimeProviderMock.Object.UtcNow);
        
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();

        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(
                    null,
                    templateId,
                    "FAKE",
                    "Fake",
                    "FAKE",
                    "FAKE",
                    1,
                    [
                        new VariantConfigurationOptionValueDto(null, redId, "FAKE", "Fake", "Fake", "#000", 1, null),
                        new VariantConfigurationOptionValueDto(null, blueId, "FAKE", "Fake", "Fake", "#000", 2, null)
                    ])
            ],
            [
                new VariantConfigurationVariantDto(
                    $"{templateId:D}:{redId:D}",
                    null, null, null, null, "Red Variant", true, null, null,
                    [new VariantConfigurationSelectedValueDto(templateId, redId)]
                ),
                new VariantConfigurationVariantDto(
                    $"{templateId:D}:{blueId:D}",
                    null, null, null, null, "Blue Variant", true, null, null,
                    [new VariantConfigurationSelectedValueDto(templateId, blueId)]
                )
            ], []);

        var command = new SaveProductDraftCommand(
            productId,
            "T-Shirt",
            "TSHIRT",
            "t-shirt",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            "ACTIVE",
            true,
            true,
            false,
            false,
            false,
            false,
            ProductWizardStage.ProductConfiguration,
            ProductWizardStage.ProductConfiguration,
            product.RowVersion,
            [],
            VariantConfiguration: config);

        // Act
        var result = await _sut.SaveProductDraftAsync(tenantId, userId, command, _dateTimeProviderMock.Object.UtcNow, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        var options = await _dbContext.ProductOptions.Where(x => x.ProductId == productId).ToListAsync();
        Assert.Single(options);
        Assert.Equal("COLOR", options[0].OptionCode); // Proves authoritative master data overriding
        Assert.Equal("ACTIVE", options[0].Status);

        var values = await _dbContext.ProductOptionValues.Where(x => x.ProductOptionId == options[0].Id).ToListAsync();
        Assert.Equal(2, values.Count);
        Assert.Contains(values, v => v.ValueCode == "RED" && v.Status == "ACTIVE");
        Assert.Contains(values, v => v.ValueCode == "BLU" && v.Status == "ACTIVE");
    }

    [Fact]
    public async Task ApplyVariantConfiguration_ShouldInactiveRemovedOptionsAndValues()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var templateId = Guid.NewGuid();
        var redId = Guid.NewGuid();
        var blueId = Guid.NewGuid();
        await SeedTemplateAsync(templateId, redId, blueId);

        var product = Product.Create(
            productId,
            tenantId,
            "TSHIRT",
            "T-Shirt",
            "t-shirt",
            "GOODS",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            "ACTIVE",
            userId,
            _dateTimeProviderMock.Object.UtcNow);
        
        await _dbContext.Products.AddAsync(product);

        var existingOption = ProductOption.Create(Guid.NewGuid(), tenantId, productId, templateId, "COLOR", "Color", "COLOR", "SWATCH", true, 1, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptions.AddAsync(existingOption);

        var existingRed = ProductOptionValue.Create(Guid.NewGuid(), tenantId, existingOption.Id, redId, "RED", "Red", "Red", "#FF0000", null, 1, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        var existingBlue = ProductOptionValue.Create(Guid.NewGuid(), tenantId, existingOption.Id, blueId, "BLU", "Blue", "Blue", "#0000FF", null, 2, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptionValues.AddRangeAsync(existingRed, existingBlue);

        await _dbContext.SaveChangesAsync();

        // payload only keeps RED, drops BLUE
        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(
                    existingOption.Id,
                    templateId,
                    "COLOR",
                    "Color",
                    "COLOR",
                    "SWATCH",
                    1,
                    [
                        new VariantConfigurationOptionValueDto(existingRed.Id, redId, "RED", "Red", "Red", "#FF0000", 1, null)
                    ])
            ],
            [
                new VariantConfigurationVariantDto(
                    $"{templateId:D}:{redId:D}",
                    null, null, null, null, "Red Variant", true, null, null,
                    [new VariantConfigurationSelectedValueDto(templateId, redId)]
                )
            ], []);

        var command = new SaveProductDraftCommand(
            productId,
            "T-Shirt",
            "TSHIRT",
            "t-shirt",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            "ACTIVE",
            true,
            true,
            false,
            false,
            false,
            false,
            ProductWizardStage.ProductConfiguration,
            ProductWizardStage.ProductConfiguration,
            product.RowVersion,
            [],
            VariantConfiguration: config);

        // Act
        var result = await _sut.SaveProductDraftAsync(tenantId, userId, command, _dateTimeProviderMock.Object.UtcNow, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        var options = await _dbContext.ProductOptions.Where(x => x.ProductId == productId).ToListAsync();
        Assert.Single(options);
        Assert.Equal("ACTIVE", options[0].Status);

        var values = await _dbContext.ProductOptionValues.Where(x => x.ProductOptionId == options[0].Id).ToListAsync();
        Assert.Equal(2, values.Count);
        
        var red = values.First(x => x.Id == existingRed.Id);
        Assert.Equal("ACTIVE", red.Status);

        var blue = values.First(x => x.Id == existingBlue.Id);
        Assert.Equal("INACTIVE", blue.Status); // Proves INACTIVE status
    }

    [Fact]
    public async Task ApplyVariantConfiguration_ShouldReactivateInactiveOptions_WhenReselected()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var templateId = Guid.NewGuid();
        var redId = Guid.NewGuid();
        var blueId = Guid.NewGuid();
        await SeedTemplateAsync(templateId, redId, blueId);

        var product = Product.Create(
            productId,
            tenantId,
            "TSHIRT",
            "T-Shirt",
            "t-shirt",
            "GOODS",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            "ACTIVE",
            userId,
            _dateTimeProviderMock.Object.UtcNow);
        
        await _dbContext.Products.AddAsync(product);

        // Initially inactive
        var existingOption = ProductOption.Create(Guid.NewGuid(), tenantId, productId, templateId, "COLOR", "Color", "COLOR", "SWATCH", true, 1, "INACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptions.AddAsync(existingOption);

        var existingRed = ProductOptionValue.Create(Guid.NewGuid(), tenantId, existingOption.Id, redId, "RED", "Red", "Red", "#FF0000", null, 1, "INACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptionValues.AddAsync(existingRed);

        await _dbContext.SaveChangesAsync();

        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(
                    null, // Client doesn't know the ID since it was inactive
                    templateId,
                    "COLOR",
                    "Color",
                    "COLOR",
                    "SWATCH",
                    1,
                    [
                        new VariantConfigurationOptionValueDto(null, redId, "RED", "Red", "Red", "#FF0000", 1, null)
                    ])
            ],
            [
                new VariantConfigurationVariantDto(
                    $"{templateId:D}:{redId:D}",
                    null, null, null, null, "Red Variant", true, null, null,
                    [new VariantConfigurationSelectedValueDto(templateId, redId)]
                )
            ], []);

        var command = new SaveProductDraftCommand(
            productId,
            "T-Shirt",
            "TSHIRT",
            "t-shirt",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            "ACTIVE",
            true,
            true,
            false,
            false,
            false,
            false,
            ProductWizardStage.ProductConfiguration,
            ProductWizardStage.ProductConfiguration,
            product.RowVersion,
            [],
            VariantConfiguration: config);

        // Act
        var result = await _sut.SaveProductDraftAsync(tenantId, userId, command, _dateTimeProviderMock.Object.UtcNow, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        var options = await _dbContext.ProductOptions.Where(x => x.ProductId == productId).ToListAsync();
        Assert.Single(options);
        Assert.Equal(existingOption.Id, options[0].Id); // Identity reuse
        Assert.Equal("ACTIVE", options[0].Status);

        var values = await _dbContext.ProductOptionValues.Where(x => x.ProductOptionId == options[0].Id).ToListAsync();
        Assert.Single(values);
        Assert.Equal(existingRed.Id, values[0].Id); // Identity reuse
        Assert.Equal("ACTIVE", values[0].Status);
    }

    [Fact]
    public async Task SaveDraft_ShouldNotWipeExistingData_WhenConfigurationIsNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var templateId = Guid.NewGuid();
        var redId = Guid.NewGuid();
        var blueId = Guid.NewGuid();
        await SeedTemplateAsync(templateId, redId, blueId);

        var product = Product.Create(
            productId,
            tenantId,
            "TSHIRT",
            "T-Shirt",
            "t-shirt",
            "GOODS",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            "ACTIVE",
            userId,
            _dateTimeProviderMock.Object.UtcNow);
        
        await _dbContext.Products.AddAsync(product);

        var existingOption = ProductOption.Create(Guid.NewGuid(), tenantId, productId, templateId, "COLOR", "Color", "COLOR", "SWATCH", true, 1, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptions.AddAsync(existingOption);

        var existingRed = ProductOptionValue.Create(Guid.NewGuid(), tenantId, existingOption.Id, redId, "RED", "Red", "Red", "#FF0000", null, 1, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptionValues.AddAsync(existingRed);

        await _dbContext.SaveChangesAsync();

        var command = new SaveProductDraftCommand(
            productId,
            "T-Shirt",
            "TSHIRT",
            "t-shirt",
            ProductStructureConstants.Variant,
            null,
            null,
            null,
            null,
            "ACTIVE",
            true,
            true,
            false,
            false,
            false,
            false,
            ProductWizardStage.ProductConfiguration,
            ProductWizardStage.ProductConfiguration,
            product.RowVersion,
            [],
            VariantConfiguration: null);

        // Act
        var result = await _sut.SaveProductDraftAsync(tenantId, userId, command, _dateTimeProviderMock.Object.UtcNow, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        var options = await _dbContext.ProductOptions.Where(x => x.ProductId == productId).ToListAsync();
        Assert.Single(options);
        Assert.Equal("ACTIVE", options[0].Status);

        var values = await _dbContext.ProductOptionValues.Where(x => x.ProductOptionId == options[0].Id).ToListAsync();
        Assert.Single(values);
        Assert.Equal("ACTIVE", values[0].Status);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
