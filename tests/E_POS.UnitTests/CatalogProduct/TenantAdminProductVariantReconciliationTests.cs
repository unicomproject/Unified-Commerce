using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.UnitTests.CatalogProduct;

public class TenantAdminProductVariantReconciliationTests : IDisposable
{
    private readonly EPosDbContext _dbContext;
    private readonly TenantAdminProductRepository _sut;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public TenantAdminProductVariantReconciliationTests()
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

    [Fact]
    public async Task ApplyVariantConfiguration_ShouldRejectExtraOrMissingVariants()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var product = Product.Create(productId, tenantId, "TSHIRT", "T-Shirt", "t-shirt", "GOODS", ProductStructureConstants.Variant, null, null, null, null, null, true, true, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.Products.AddAsync(product);

        var templateId = Guid.NewGuid();
        var template = ProductOptionTemplate.Create(templateId, "COLOR", "Color", "SWATCH", "SELECT", 1, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptionTemplates.AddAsync(template);
        
        var redId = Guid.NewGuid();
        var red = ProductOptionTemplateValue.Create(redId, templateId, "RED", "Red", "Red", "#FF0000", null, 1, "ACTIVE", userId, _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.ProductOptionTemplateValues.AddAsync(red);
        
        var pieceUom = UnitOfMeasure.Create(Guid.NewGuid(), null, "PIECE", "Piece", "UNIT", "Pcs", null, 1m, "ACTIVE", _dateTimeProviderMock.Object.UtcNow);
        await _dbContext.UnitOfMeasures.AddAsync(pieceUom);
        await _dbContext.SaveChangesAsync();

        var config = new VariantConfigurationDto(
            [
                new VariantConfigurationOptionDto(null, templateId, "COLOR", "Color", "SWATCH", "SELECT", 1, [
                    new VariantConfigurationOptionValueDto(null, redId, "RED", "Red", "Red", "#FF0000", 1, null)
                ])
            ],
            [], // MISSING REQUIRED VARIANTS (Expected 1)
            []
        );

        var command = new SaveProductDraftCommand(productId, "T-Shirt", "TSHIRT", "t-shirt", ProductStructureConstants.Variant, null, null, null, null, "ACTIVE", true, true, false, false, false, false, ProductWizardStage.ProductConfiguration, ProductWizardStage.ProductConfiguration, product.RowVersion, [], VariantConfiguration: config);

        // Act
        var result = await _sut.SaveProductDraftAsync(tenantId, userId, command, _dateTimeProviderMock.Object.UtcNow, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("product.validation_failed", result.Error?.Code);
        Assert.Contains(result.Error?.FieldErrors ?? [], e => e.Field == "variants");
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
