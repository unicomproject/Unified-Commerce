using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IProductRepository
{
    Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductId, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductId, CancellationToken cancellationToken);
    Task<ProductListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(Guid tenantId, Guid productId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Product?> GetEditableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken);
    Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken);
    Task AddCategoryLinksAsync(IEnumerable<ProductCategory> links, CancellationToken cancellationToken);
    Task AddCollectionLinksAsync(IEnumerable<ProductCollection> links, CancellationToken cancellationToken);
    Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken);
    Task AddChannelVisibilitiesAsync(IEnumerable<ProductChannelVisibility> visibilities, CancellationToken cancellationToken);
    Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken);
    Task ClearProductMappingsAsync(Guid productId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    // Helper to get the tenant default active price list
    Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken);
    // Helper to get the default variant of a product
    Task<ProductVariant?> GetDefaultVariantAsync(Guid productId, CancellationToken cancellationToken);
    // Helper to get price list item for a variant
    Task<PriceListItem?> GetPriceListItemAsync(Guid priceListId, Guid variantId, CancellationToken cancellationToken);
    // Helper to get barcode for a variant
    Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken);
}
