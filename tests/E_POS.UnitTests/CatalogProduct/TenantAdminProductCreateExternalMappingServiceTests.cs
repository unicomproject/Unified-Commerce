using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    private static TenantRequestContext CreateFullAdminContext() =>
        new(TenantId, UserId, [
            ProductConstants.CreatePermission,
            ProductConstants.PublishPermission,
            ProductConstants.ViewPermission,
            ProductConstants.UpdatePermission,
            ProductConstants.BarcodesManagePermission,
            ProductConstants.ProductPricingManagePermission,
            ProductConstants.ProductCostViewPermission,
            ProductConstants.ManagePermission,
        ]);

    [Fact]
    public async Task CreateFromWizardAsync_WithInvalidMappingContext_ReturnsValidationErrorWithoutCallingRepository()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("", "en:colas", "Colas"), // Empty provider!
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.CreateProductCallCount);
    }

    [Fact]
    public async Task CreateFromWizardAsync_WithValidMappingContext_NormalizesContextAndCallsRepository()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            WizardCreateResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                Guid.NewGuid(),
                "Test Product",
                "PC-001",
                ProductConstants.ActiveStatus,
                ProductConstants.DesiredPublishActive,
                1,
                DateTimeOffset.UtcNow,
                1,
                Guid.NewGuid(),
                null,
                null,
                null,
                true,
                false,
                false,
                false,
                false,
                ProductStructureConstants.Simple,
                false,
                [])),
        };
        var service = CreateService(repository);

        var categoryId = Guid.NewGuid();
        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = categoryId,
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
                        "SKU-SIMPLE-001",
                        "4006381333931",
                        null,
                        "SIMPLE_DEFAULT",
                        "EAN13")
                ]),
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("  OPENFOODFACTS  ", "  EN:COLAS  ", "  Colas  "),
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess, $"{result.Error?.Code}: {result.Error?.Message} ({string.Join(", ", result.Error?.FieldErrors?.Select(f => f.Message) ?? [])})");
        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.NotNull(repository.LastWizardRequest);
        Assert.NotNull(repository.LastWizardRequest!.ExternalCategoryMappingContext);
        Assert.Equal("openfoodfacts", repository.LastWizardRequest.ExternalCategoryMappingContext!.Provider);
        Assert.Equal("en:colas", repository.LastWizardRequest.ExternalCategoryMappingContext.ExternalCategoryKey);
        Assert.Equal("Colas", repository.LastWizardRequest.ExternalCategoryMappingContext.ExternalCategoryName);
    }

    [Fact]
    public async Task CreateFromWizardAsync_WithoutMappingContext_SucceedsNormally()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            WizardCreateResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                Guid.NewGuid(),
                "Normal Product",
                "PC-001",
                ProductConstants.ActiveStatus,
                ProductConstants.DesiredPublishActive,
                1,
                DateTimeOffset.UtcNow,
                1,
                Guid.NewGuid(),
                null,
                null,
                null,
                true,
                false,
                false,
                false,
                false,
                ProductStructureConstants.Simple,
                false,
                [])),
        };
        var service = CreateService(repository);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Normal Product",
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
                        "SKU-SIMPLE-002",
                        "4006381333932",
                        null,
                        "SIMPLE_DEFAULT",
                        "EAN13")
                ]),
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = null,
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess, $"{result.Error?.Code}: {result.Error?.Message} ({string.Join(", ", result.Error?.FieldErrors?.Select(f => f.Message) ?? [])})");
        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.Null(repository.LastWizardRequest!.ExternalCategoryMappingContext);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidMappingContext_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("", "en:colas", "Colas"), // Empty provider!
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Null(repository.LastCreateRequest);
    }

    [Fact]
    public async Task CreateAsync_WithValidMappingContext_NormalizesContextAndCallsRepository()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = Guid.NewGuid(),
        };
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("  OPENFOODFACTS  ", "  EN:COLAS  ", "  Colas  "),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCreateRequest);
        Assert.NotNull(repository.LastCreateRequest!.ExternalCategoryMappingContext);
        Assert.Equal("openfoodfacts", repository.LastCreateRequest.ExternalCategoryMappingContext!.Provider);
        Assert.Equal("en:colas", repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryKey);
        Assert.Equal("Colas", repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryName);
    }

    [Fact]
    public async Task CreateAsync_ProviderLength51_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext(new string('p', 51), "en:colas", "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("cannot exceed 50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_KeyLength151_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", new string('k', 151), "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_NameLength151_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", new string('n', 151)),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_ExactBoundaries50And150_Succeeds()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = Guid.NewGuid(),
        };
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext(new string('p', 50), new string('k', 150), new string('n', 150)),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCreateRequest);
        Assert.Equal(50, repository.LastCreateRequest!.ExternalCategoryMappingContext!.Provider.Length);
        Assert.Equal(150, repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryKey.Length);
        Assert.Equal(150, repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryName!.Length);
    }
}
