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

public sealed class StorefrontProductSearchRepository : StorefrontProductRepositoryBase, IStorefrontProductSearchRepository
{
    public StorefrontProductSearchRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<StorefrontSearchReadModel> SearchAsync(
        Guid tenantId,
        StorefrontSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var searchText = request.SearchText?.Trim();
        var normalizedSearch = searchText?.ToUpperInvariant();
        var products = await DbContext.Set<Product>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus && x.IsSellable)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            products = products.Where(x =>
                x.ProductName.Contains(searchText!, StringComparison.OrdinalIgnoreCase) ||
                x.ProductCode.Contains(searchText!, StringComparison.OrdinalIgnoreCase) ||
                (x.ShortDescription?.Contains(searchText!, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.LongDescription?.Contains(searchText!, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        if (request.CategoryId.HasValue)
        {
            var categoryProductIds = await DbContext.Set<ProductCategory>()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CategoryId == request.CategoryId.Value)
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync(cancellationToken);
            products = products.Where(x => categoryProductIds.Contains(x.Id)).ToList();
        }

        var productIds = products.Select(x => x.Id).ToList();
        if (!string.IsNullOrWhiteSpace(request.Colour) || !string.IsNullOrWhiteSpace(request.Size))
        {
            var optionRows = await (
                from option in DbContext.Set<ProductOption>().AsNoTracking()
                join value in DbContext.Set<ProductOptionValue>().AsNoTracking()
                    on new { option.TenantId, OptionId = option.Id }
                    equals new { value.TenantId, OptionId = value.ProductOptionId }
                where option.TenantId == tenantId && productIds.Contains(option.ProductId) &&
                      option.Status == ActiveStatus && value.Status == ActiveStatus
                select new { option.ProductId, option.OptionName, option.OptionType, value.ValueName, value.DisplayName })
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Colour))
            {
                var colourIds = optionRows.Where(x =>
                        (x.OptionName.Contains("colour", StringComparison.OrdinalIgnoreCase) ||
                         x.OptionName.Contains("color", StringComparison.OrdinalIgnoreCase) ||
                         x.OptionType.Contains("colour", StringComparison.OrdinalIgnoreCase) ||
                         x.OptionType.Contains("color", StringComparison.OrdinalIgnoreCase)) &&
                        (x.ValueName.Equals(request.Colour, StringComparison.OrdinalIgnoreCase) ||
                         (x.DisplayName?.Equals(request.Colour, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .Select(x => x.ProductId).ToHashSet();
                products = products.Where(x => colourIds.Contains(x.Id)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Size))
            {
                var sizeIds = optionRows.Where(x =>
                        (x.OptionName.Contains("size", StringComparison.OrdinalIgnoreCase) ||
                         x.OptionType.Contains("size", StringComparison.OrdinalIgnoreCase)) &&
                        (x.ValueName.Equals(request.Size, StringComparison.OrdinalIgnoreCase) ||
                         (x.DisplayName?.Equals(request.Size, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .Select(x => x.ProductId).ToHashSet();
                products = products.Where(x => sizeIds.Contains(x.Id)).ToList();
            }
        }

        var allProductIds = products.Select(x => x.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var normalizedSort = request.Sort?.Trim().ToLowerInvariant();

        var needsPrices = request.MinPrice.HasValue || request.MaxPrice.HasValue || normalizedSort == "price_asc" || normalizedSort == "price_desc";
        var needsRatings = normalizedSort == "rating_desc" || string.IsNullOrEmpty(normalizedSort) || (normalizedSort != "price_asc" && normalizedSort != "price_desc" && normalizedSort != "newest");
        var needsInventory = request.InStock.HasValue;

        Dictionary<Guid, decimal?>? pricesByProduct = null;
        Dictionary<Guid, ProductRatingSummary>? ratingsByProduct = null;
        Dictionary<Guid, decimal>? inventoryByProduct = null;

        if (needsPrices)
            pricesByProduct = await GetProductPricesByProductAsync(tenantId, allProductIds, currencyCode, now, cancellationToken);
        
        if (needsRatings)
            ratingsByProduct = await GetRatingsByProductAsync(tenantId, allProductIds, cancellationToken);

        if (needsInventory)
            inventoryByProduct = await GetInventoryByProductAsync(tenantId, allProductIds, cancellationToken);

        var filteredProducts = products.AsEnumerable();
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => 
            {
                var price = pricesByProduct != null && pricesByProduct.TryGetValue(p.Id, out var pVal) ? pVal : null;
                if (request.MinPrice.HasValue && (price == null || price.Value < request.MinPrice.Value)) return false;
                if (request.MaxPrice.HasValue && (price == null || price.Value > request.MaxPrice.Value)) return false;
                return true;
            });
        }
        
        if (request.InStock.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => 
            {
                var hasInventory = false;
                var qty = 0m;
                if (inventoryByProduct != null)
                {
                    hasInventory = inventoryByProduct.TryGetValue(p.Id, out qty);
                }
                var inStock = !hasInventory || qty > 0m;
                return inStock == request.InStock.Value;
            });
        }

        var sortItems = filteredProducts.Select(product =>
        {
            var pId = product.Id;
            var price = pricesByProduct != null && pricesByProduct.TryGetValue(pId, out var p) ? p : null;
            var rating = ratingsByProduct != null && ratingsByProduct.TryGetValue(pId, out var r) ? r : null;

            return new ProductListingSortItem(
                product,
                price,
                0,
                rating?.AverageRating ?? 0m,
                rating?.TotalReviews ?? 0);
        }).ToList();

        var totalProducts = sortItems.Count;
        
        var pagedSortItems = SortProductListings(sortItems, request.Sort)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var pagedProductIds = pagedSortItems.Select(x => x.Product.Id).ToList();
        
        pricesByProduct ??= await GetProductPricesByProductAsync(tenantId, pagedProductIds, currencyCode, now, cancellationToken);
        ratingsByProduct ??= await GetRatingsByProductAsync(tenantId, pagedProductIds, cancellationToken);
        inventoryByProduct ??= await GetInventoryByProductAsync(tenantId, pagedProductIds, cancellationToken);
        
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var optionsByProduct = await GetVariantOptionsByProductAsync(tenantId, pagedProductIds, cancellationToken);

        var productPage = pagedSortItems.Select(row =>
        {
            var product = row.Product;
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            pricesByProduct.TryGetValue(product.Id, out var sellingPrice);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            var hasInventory = inventoryByProduct.TryGetValue(product.Id, out var availableQuantity);
            var averageRating = rating?.AverageRating ?? 0m;
            var reviewCount = rating?.TotalReviews ?? 0;
            
            var productOptions = optionsByProduct.TryGetValue(product.Id, out var opts) ? opts : new ProductVariantOptions([], [], []);

            return StorefrontProductMapper.ToListReadModel(
                product,
                sellingPrice,
                primaryImageUrl,
                averageRating,
                reviewCount,
                !hasInventory || availableQuantity > 0m,
                currencyCode,
                BuildSelectableOptions(productOptions));
        }).ToList();

        var categoryRows = await (from category in DbContext.Set<Category>().AsNoTracking()
                                  join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                      on new { category.TenantId, MediaAssetId = category.ImageMediaAssetId }
                                      equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                                  from mediaAsset in mediaAssets.DefaultIfEmpty()
                                  where category.TenantId == tenantId && category.Status == ActiveStatus
                                  orderby category.SortOrder
                                  select new
                                  {
                                      Category = category,
                                      MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                                      MediaContainerName = mediaAsset == null ? null : mediaAsset.ContainerName,
                                      MediaStorageKey = mediaAsset == null ? null : mediaAsset.StorageKey,
                                      MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                                  })
            .ToListAsync(cancellationToken);
        var collections = await DbContext.Set<Collection>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus &&
                        (!x.StartsAt.HasValue || x.StartsAt <= now) &&
                        (!x.EndsAt.HasValue || x.EndsAt >= now))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            categoryRows = categoryRows.Where(x => x.Category.CategoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                                   (x.Category.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            collections = collections.Where(x => x.CollectionName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                                  (x.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        else
        {
            categoryRows = [];
            collections = [];
        }

        return new StorefrontSearchReadModel
        {
            Products = new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = productPage, TotalCount = totalProducts, Page = request.Page, PageSize = request.PageSize
            },
            Categories = categoryRows.Select(x => new StorefrontSearchMatchReadModel
            {
                Id = x.Category.Id,
                Name = x.Category.CategoryName,
                Slug = x.Category.CategorySlug,
                Description = x.Category.Description,
                ImageUrl = ResolveActiveMediaReadUrl(
                    x.MediaStatus,
                    x.MediaContainerName,
                    x.MediaStorageKey,
                    x.MediaPublicUrl)
            }).ToList(),
            Collections = collections.Select(x => new StorefrontSearchMatchReadModel
            {
                Id = x.Id, Name = x.CollectionName, Slug = x.CollectionSlug, Description = x.Description
            }).ToList()
        };
    }

}
