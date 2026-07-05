using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly EPosDbContext _dbContext;

    public ProductRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProductCode == productCode &&
                     x.Status != ProductConstants.DeletedStatus &&
                     (!excludeProductId.HasValue || x.Id != excludeProductId.Value),
                cancellationToken);
    }

    public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductVariantId, CancellationToken cancellationToken)
    {
        return _dbContext.ProductVariants
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Sku == sku &&
                     x.Status != ProductConstants.DeletedStatus &&
                     (!excludeProductVariantId.HasValue || x.Id != excludeProductVariantId.Value),
                cancellationToken);
    }

    public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductVariantId, CancellationToken cancellationToken)
    {
        return _dbContext.ProductBarcodes
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Barcode == barcodeValue &&
                     (!excludeProductVariantId.HasValue || x.ProductVariantId != excludeProductVariantId.Value),
                cancellationToken);
    }

    public async Task<ProductListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var products = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != ProductConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                products = products.Where(x => EF.Functions.ILike(x.ProductName, pattern) || EF.Functions.ILike(x.ProductCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                products = products.Where(x => x.ProductName.ToUpper().Contains(normalizedTerm) || x.ProductCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var defaultPriceListId = await GetDefaultPriceListIdAsync(tenantId, cancellationToken);

        var rows = await (from product in products
                          join variant in _dbContext.ProductVariants.AsNoTracking()
                              on product.Id equals variant.ProductId into variantJoin
                          from variant in variantJoin.Where(v => v.Status != ProductConstants.DeletedStatus).DefaultIfEmpty()
                          join barcode in _dbContext.ProductBarcodes.AsNoTracking()
                              on variant.Id equals barcode.ProductVariantId into barcodeJoin
                          from barcode in barcodeJoin.DefaultIfEmpty()
                          join price in _dbContext.PriceListItems.AsNoTracking()
                              on new { VariantId = (Guid?)variant.Id, PriceListId = defaultPriceListId }
                              equals new { VariantId = (Guid?)price.ProductVariantId, PriceListId = (Guid?)price.PriceListId } into priceJoin
                          from price in priceJoin.DefaultIfEmpty()
                          select new
                          {
                              Product = product,
                              Sku = variant != null ? variant.Sku : string.Empty,
                              Barcode = barcode != null ? barcode.Barcode : null,
                              Price = price != null ? (decimal?)price.SellingPrice : null
                          })
            .OrderBy(x => x.Product.ProductName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductSummaryResponse(
                x.Product.Id,
                x.Product.ProductCode,
                x.Product.ProductName,
                x.Product.Status,
                x.Sku,
                x.Barcode,
                x.Price,
                x.Product.CreatedAt,
                x.Product.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new ProductListResponse(rows, pageNumber, pageSize, totalCount);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid tenantId, Guid productId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == productId && (includeDeleted || x.Status != ProductConstants.DeletedStatus), cancellationToken);

        if (product is null) return null;

        var defaultPriceListId = await GetDefaultPriceListIdAsync(tenantId, cancellationToken);

        var variant = await _dbContext.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.Status != ProductConstants.DeletedStatus, cancellationToken);

        var barcodeVal = variant != null
            ? await _dbContext.ProductBarcodes.AsNoTracking().Where(x => x.ProductVariantId == variant.Id).Select(x => x.Barcode).FirstOrDefaultAsync(cancellationToken)
            : null;

        var priceVal = (variant != null && defaultPriceListId.HasValue)
            ? await _dbContext.PriceListItems.AsNoTracking().Where(x => x.PriceListId == defaultPriceListId.Value && x.ProductVariantId == variant.Id).Select(x => (decimal?)x.SellingPrice).FirstOrDefaultAsync(cancellationToken)
            : null;

        var categoryIds = await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToArrayAsync(cancellationToken);

        var collectionIds = await _dbContext.ProductCollections
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CollectionId)
            .ToArrayAsync(cancellationToken);

        var imageUrls = await _dbContext.ProductImages
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ImageUrl ?? string.Empty)
            .ToArrayAsync(cancellationToken);

        var salesChannelIds = await _dbContext.ProductChannelVisibilities
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.SalesChannelId)
            .ToArrayAsync(cancellationToken);

        return new ProductResponse(
            product.Id,
            product.ProductCode,
            product.ProductName,
            product.ShortDescription,
            product.Status,
            variant != null ? (variant.Sku ?? string.Empty) : string.Empty,
            barcodeVal,
            priceVal,
            categoryIds,
            collectionIds,
            imageUrls,
            salesChannelIds,
            product.CreatedAt,
            product.UpdatedAt);
    }

    public Task<Product?> GetEditableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == productId && x.Status != ProductConstants.DeletedStatus, cancellationToken);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _dbContext.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken)
    {
        _dbContext.ProductVariants.Add(variant);
        return Task.CompletedTask;
    }

    public Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken)
    {
        _dbContext.ProductBarcodes.Add(barcode);
        return Task.CompletedTask;
    }

    public Task AddCategoryLinksAsync(IEnumerable<ProductCategory> links, CancellationToken cancellationToken)
    {
        _dbContext.ProductCategories.AddRange(links);
        return Task.CompletedTask;
    }

    public Task AddCollectionLinksAsync(IEnumerable<ProductCollection> links, CancellationToken cancellationToken)
    {
        _dbContext.ProductCollections.AddRange(links);
        return Task.CompletedTask;
    }

    public Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken)
    {
        _dbContext.ProductImages.AddRange(images);
        return Task.CompletedTask;
    }

    public Task AddChannelVisibilitiesAsync(IEnumerable<ProductChannelVisibility> visibilities, CancellationToken cancellationToken)
    {
        _dbContext.ProductChannelVisibilities.AddRange(visibilities);
        return Task.CompletedTask;
    }

    public Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken)
    {
        _dbContext.PriceListItems.Add(priceListItem);
        return Task.CompletedTask;
    }

    public async Task ClearProductMappingsAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categories = await _dbContext.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _dbContext.ProductCategories.RemoveRange(categories);

        var collections = await _dbContext.ProductCollections.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _dbContext.ProductCollections.RemoveRange(collections);

        var images = await _dbContext.ProductImages.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _dbContext.ProductImages.RemoveRange(images);

        var barcodes = await _dbContext.ProductBarcodes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _dbContext.ProductBarcodes.RemoveRange(barcodes);

        var channels = await _dbContext.ProductChannelVisibilities.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _dbContext.ProductChannelVisibilities.RemoveRange(channels);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductVariant?> GetDefaultVariantAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductVariants
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.Status != ProductConstants.DeletedStatus, cancellationToken);
    }

    public async Task<PriceListItem?> GetPriceListItemAsync(Guid priceListId, Guid variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceListItems
            .FirstOrDefaultAsync(x => x.PriceListId == priceListId && x.ProductVariantId == variantId, cancellationToken);
    }

    public async Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductBarcodes
            .FirstOrDefaultAsync(x => x.ProductVariantId == variantId, cancellationToken);
    }
}
