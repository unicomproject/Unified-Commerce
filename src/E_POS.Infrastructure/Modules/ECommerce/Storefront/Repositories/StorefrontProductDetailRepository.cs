using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontProductDetailRepository : StorefrontProductRepositoryBase, IStorefrontProductDetailRepository
{
    public StorefrontProductDetailRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(
        Guid tenantId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductBySlugAsync(tenantId, slug, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var productId = product.Id;
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var rating = await GetRatingAsync(tenantId, productId, cancellationToken);
        var productPrice = await GetProductPriceAsync(tenantId, productId, currencyCode, now, cancellationToken);
        var images = await GetProductImagesAsync(tenantId, product, cancellationToken);
        var variants = await GetProductVariantsAsync(tenantId, productId, cancellationToken);
        var variantIds = variants.Select(x => x.Id).ToList();
        var variantPricesByVariant = await GetVariantPricesByVariantAsync(tenantId, productId, variantIds, currencyCode, now, cancellationToken);
        var inventoryRows = await GetProductInventoryRowsAsync(tenantId, productId, cancellationToken);
        var inventoryByVariant = inventoryRows
            .Where(x => x.ProductVariantId.HasValue)
            .ToDictionary(x => x.ProductVariantId!.Value, x => x.AvailableQuantity);
        var variantOptions = await GetVariantOptionsAsync(tenantId, productId, variantIds, cancellationToken);

        var options = BuildSelectableOptions(variantOptions);
        var variantModels = BuildVariantModels(variants, productPrice, variantPricesByVariant, inventoryByVariant, variantOptions, currencyCode);
        var highlights = await GetHighlightsAsync(tenantId, productId, cancellationToken);
        var returnInfo = StorefrontProductMapper.BuildReturnInfo(await GetReturnPolicyAsync(tenantId, product.ReturnPolicyId, cancellationToken));
        var isInStock = variantModels.Count > 0
            ? variantModels.Any(x => x.IsInStock)
            : !inventoryRows.Any() || inventoryRows.Sum(x => x.AvailableQuantity) > 0m;
        var detailPrice = productPrice ?? variantModels.Select(x => (decimal?)x.Price).FirstOrDefault() ?? 0m;

        var categoryQuery = await (
            from pc in DbContext.Set<ProductCategory>().AsNoTracking()
            join c in DbContext.Set<Category>().AsNoTracking() on pc.CategoryId equals c.Id
            where pc.TenantId == tenantId && pc.ProductId == productId
            orderby pc.IsPrimaryCategory descending
            select new { Category = c, ParentId = c.ParentCategoryId }
        ).FirstOrDefaultAsync(cancellationToken);

        Category? primaryCategory = categoryQuery?.Category;
        Category? parentCategory = null;

        if (categoryQuery?.ParentId != null)
        {
            parentCategory = await DbContext.Set<Category>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == categoryQuery.ParentId, cancellationToken);
        }

        Brand? brand = null;
        if (product.BrandId != null)
        {
            brand = await DbContext.Set<Brand>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == product.BrandId && b.Status == "ACTIVE", cancellationToken);
        }

        return StorefrontProductMapper.ToDetailReadModel(
            product,
            detailPrice,
            currencyCode,
            rating,
            isInStock,
            images,
            options,
            variantModels,
            highlights,
            returnInfo,
            primaryCategory,
            parentCategory,
            brand);
    }

}
