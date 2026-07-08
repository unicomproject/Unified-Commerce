using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Application.Modules.Tenant.PricingTax.Services;
using E_POS.Application.Modules.Tenant.PricingTax.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.PricingTax;

public sealed class ProductTaxAssignmentServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid TaxClassId = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenPermissionDenied()
    {
        var context = new TenantRequestContext(TenantId, UserId, []);
        var service = CreateService();

        var result = await service.CreateAsync(context, new ProductTaxAssignmentCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.product_tax_assignment.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenProductNotFound()
    {
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.ProductTaxAssignments.Create]);
        var repository = new FakeProductTaxAssignmentRepository();
        var productRepo = new FakeProductRepository { ProductExists = false };
        var service = CreateService(repository, productRepo: productRepo);

        var request = new ProductTaxAssignmentCreateRequest { ProductId = ProductId, TaxClassId = TaxClassId };

        var result = await service.CreateAsync(context, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.product_tax_assignment.product_not_found", result.Error?.Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenOverlapExists()
    {
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.ProductTaxAssignments.Create]);
        var repository = new FakeProductTaxAssignmentRepository { OverlapExists = true };
        var service = CreateService(repository);

        var request = new ProductTaxAssignmentCreateRequest { ProductId = ProductId, TaxClassId = TaxClassId };

        var result = await service.CreateAsync(context, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pricing.product_tax_assignment.overlap", result.Error?.Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccess_WhenValid()
    {
        var context = new TenantRequestContext(TenantId, UserId, [PricingTaxPermissions.ProductTaxAssignments.Manage]); // Testing Manage perm
        var repository = new FakeProductTaxAssignmentRepository();
        var service = CreateService(repository);

        var request = new ProductTaxAssignmentCreateRequest { ProductId = ProductId, TaxClassId = TaxClassId };

        var result = await service.CreateAsync(context, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedAssignment);
        Assert.Equal(ProductId, repository.AddedAssignment.ProductId);
    }

    private static ProductTaxAssignmentService CreateService(
        IProductTaxAssignmentRepository? repository = null,
        ITaxSetupRepository? taxRepo = null,
        IProductRepository? productRepo = null)
    {
        return new ProductTaxAssignmentService(
            repository ?? new FakeProductTaxAssignmentRepository(),
            taxRepo ?? new FakeTaxSetupRepository(),
            productRepo ?? new FakeProductRepository(),
            new ProductTaxAssignmentRequestValidator(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class FakeProductTaxAssignmentRepository : IProductTaxAssignmentRepository
    {
        public bool OverlapExists { get; set; } = false;
        public ProductTaxAssignment? AddedAssignment { get; private set; }

        public Task<ProductTaxAssignment?> GetByIdAsync(Guid tenantId, Guid id) => Task.FromResult<ProductTaxAssignment?>(null);
        public Task<(IEnumerable<ProductTaxAssignment> Items, int TotalCount)> GetByProductAsync(Guid tenantId, Guid productId, int page, int pageSize) => Task.FromResult((Enumerable.Empty<ProductTaxAssignment>(), 0));
        public Task AddAsync(ProductTaxAssignment assignment)
        {
            AddedAssignment = assignment;
            return Task.CompletedTask;
        }
        public void Update(ProductTaxAssignment assignment) { }
        public Task<bool> HasOverlappingAssignmentAsync(Guid tenantId, Guid productId, Guid? productVariantId, Guid taxClassId, DateTimeOffset? appliesFrom, DateTimeOffset? appliesUntil, Guid? excludeAssignmentId = null) => Task.FromResult(OverlapExists);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeTaxSetupRepository : ITaxSetupRepository
    {
        public Task<TaxClass?> GetTaxClassByIdAsync(Guid tenantId, Guid taxClassId) => Task.FromResult<TaxClass?>(TaxClass.Create(tenantId, "C", "N", null, true, null, DateTimeOffset.UtcNow));
        public Task<TaxClass?> GetTaxClassByCodeAsync(Guid tenantId, string taxClassCode) => Task.FromResult<TaxClass?>(null);
        public Task<(IEnumerable<TaxClass> Items, int TotalCount)> GetTaxClassesAsync(Guid tenantId, int page, int pageSize) => throw new NotImplementedException();
        public Task AddTaxClassAsync(TaxClass taxClass) => Task.CompletedTask;
        public void UpdateTaxClass(TaxClass taxClass) { }
        public Task ClearDefaultTaxClassAsync(Guid tenantId, Guid? excludeTaxClassId) => Task.CompletedTask;
        public Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid taxRateId) => Task.FromResult<TaxRate?>(null);
        public Task<TaxRate?> GetTaxRateByCodeAsync(Guid tenantId, string taxRateCode) => Task.FromResult<TaxRate?>(null);
        public Task<(IEnumerable<TaxRate> Items, int TotalCount)> GetTaxRatesAsync(Guid tenantId, int page, int pageSize) => throw new NotImplementedException();
        public Task AddTaxRateAsync(TaxRate taxRate) => Task.CompletedTask;
        public void UpdateTaxRate(TaxRate taxRate) { }
        public Task<List<TaxClassRate>> GetTaxClassRatesAsync(Guid tenantId, Guid taxClassId) => Task.FromResult(new List<TaxClassRate>());
        public Task<List<TaxRate>> GetRatesForClassAsync(Guid tenantId, Guid taxClassId) => Task.FromResult(new List<TaxRate>());
        public Task AddTaxClassRatesAsync(IEnumerable<TaxClassRate> taxClassRates) => Task.CompletedTask;
        public void RemoveTaxClassRates(IEnumerable<TaxClassRate> taxClassRates) { }
        public Task<bool> JurisdictionExistsAsync(Guid tenantId, Guid jurisdictionId) => Task.FromResult(true);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public bool ProductExists { get; set; } = true;
        
        public Task<bool> ProductExistsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken = default) 
            => Task.FromResult(ProductExists);

        public Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ProductListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ProductResponse?> GetByIdAsync(Guid tenantId, Guid productId, bool includeDeleted, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Product?> GetEditableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddAsync(Product product, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddCategoryLinksAsync(IEnumerable<ProductCategory> links, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddCollectionLinksAsync(IEnumerable<ProductCollection> links, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddChannelVisibilitiesAsync(IEnumerable<ProductChannelVisibility> visibilities, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task ClearProductMappingsAsync(Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductVariant?> GetDefaultVariantAsync(Guid productId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PriceListItem?> GetPriceListItemAsync(Guid priceListId, Guid variantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> ProductVariantExistsAsync(Guid tenantId, Guid productId, Guid variantId, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}


