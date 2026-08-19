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
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var normalizedSort = request.Sort?.Trim().ToLowerInvariant();

        var baseQuery = DbContext.Set<Product>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus && x.IsSellable);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var lowerSearch = searchText.ToLower();
            baseQuery = baseQuery.Where(x =>
                x.ProductName.ToLower().Contains(lowerSearch) ||
                x.ProductCode.ToLower().Contains(lowerSearch) ||
                (x.ShortDescription != null && x.ShortDescription.ToLower().Contains(lowerSearch)) ||
                (x.LongDescription != null && x.LongDescription.ToLower().Contains(lowerSearch)));
        }

        if (request.CategoryId.HasValue)
        {
            var catId = request.CategoryId.Value;
            baseQuery = baseQuery.Where(x => DbContext.Set<ProductCategory>().Any(pc => pc.TenantId == tenantId && pc.ProductId == x.Id && pc.CategoryId == catId));
        }

        if (!string.IsNullOrWhiteSpace(request.Colour) || !string.IsNullOrWhiteSpace(request.Size))
        {
            var lowerColour = request.Colour?.ToLower();
            var lowerSize = request.Size?.ToLower();

            baseQuery = baseQuery.Where(x => DbContext.Set<ProductOption>().Any(option => 
                option.TenantId == tenantId && 
                option.ProductId == x.Id && 
                option.Status == ActiveStatus &&
                DbContext.Set<ProductOptionValue>().Any(value => 
                    value.TenantId == tenantId && 
                    value.ProductOptionId == option.Id && 
                    value.Status == ActiveStatus &&
                    ((!string.IsNullOrWhiteSpace(lowerColour) && 
                      (option.OptionName.ToLower().Contains("colour") || option.OptionName.ToLower().Contains("color") || 
                       option.OptionType.ToLower().Contains("colour") || option.OptionType.ToLower().Contains("color")) &&
                      (value.ValueName.ToLower() == lowerColour || (value.DisplayName != null && value.DisplayName.ToLower() == lowerColour)))
                     ||
                     (!string.IsNullOrWhiteSpace(lowerSize) && 
                      (option.OptionName.ToLower().Contains("size") || option.OptionType.ToLower().Contains("size")) &&
                      (value.ValueName.ToLower() == lowerSize || (value.DisplayName != null && value.DisplayName.ToLower() == lowerSize)))
                    ))));
        }

        var queryWithComputedVars = from p in baseQuery
                                    let price = (from item in DbContext.Set<PriceListItem>().AsNoTracking()
                                                 join priceList in DbContext.Set<PriceList>().AsNoTracking()
                                                     on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                                                 where item.TenantId == tenantId &&
                                                       item.ProductId == p.Id &&
                                                       item.Status == ActiveStatus &&
                                                       item.MinQuantity <= 1m &&
                                                       priceList.Status == ActiveStatus &&
                                                       priceList.CurrencyCode == currencyCode &&
                                                       (!priceList.ValidFrom.HasValue || priceList.ValidFrom <= now) &&
                                                       (!priceList.ValidUntil.HasValue || priceList.ValidUntil >= now) &&
                                                       (!item.ValidFrom.HasValue || item.ValidFrom <= now) &&
                                                       (!item.ValidUntil.HasValue || item.ValidUntil >= now)
                                                 orderby priceList.IsDefaultPriceList descending,
                                                         priceList.Priority descending,
                                                         item.ValidFrom ?? DateTimeOffset.MinValue descending,
                                                         item.MinQuantity descending,
                                                         item.SellingPrice ascending
                                                 select (decimal?)item.SellingPrice).FirstOrDefault()
                                    let hasInventory = DbContext.Set<InventoryBalance>().AsNoTracking().Any(i => i.TenantId == tenantId && i.ProductId == p.Id && i.ProductVariantId == null)
                                    let qty = (from i in DbContext.Set<InventoryBalance>().AsNoTracking()
                                               where i.TenantId == tenantId && i.ProductId == p.Id && i.ProductVariantId == null
                                               select (decimal?)i.AvailableQuantity).FirstOrDefault()
                                    let rating = DbContext.Set<ProductRatingSummary>().AsNoTracking()
                                                   .Where(r => r.TenantId == tenantId && r.ProductId == p.Id)
                                                   .FirstOrDefault()
                                    select new { Product = p, Price = price, HasInventory = hasInventory, Qty = qty, Rating = rating };

        var filteredQuery = queryWithComputedVars;

        if (request.MinPrice.HasValue)
        {
            filteredQuery = filteredQuery.Where(x => x.Price != null && x.Price >= request.MinPrice.Value);
        }
        if (request.MaxPrice.HasValue)
        {
            filteredQuery = filteredQuery.Where(x => x.Price != null && x.Price <= request.MaxPrice.Value);
        }
        if (request.InStock.HasValue)
        {
            var wantInStock = request.InStock.Value;
            filteredQuery = filteredQuery.Where(x => (!x.HasInventory || (x.Qty != null && x.Qty > 0m)) == wantInStock);
        }

        var totalProducts = await filteredQuery.CountAsync(cancellationToken);

        IQueryable<Product> orderedQuery;
        if (normalizedSort == "price_asc")
        {
            orderedQuery = filteredQuery
                .OrderBy(x => x.Price == null ? decimal.MaxValue : x.Price)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }
        else if (normalizedSort == "price_desc")
        {
            orderedQuery = filteredQuery
                .OrderByDescending(x => x.Price == null ? decimal.MinValue : x.Price)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }
        else if (normalizedSort == "newest")
        {
            orderedQuery = filteredQuery
                .OrderByDescending(x => x.Product.CreatedAt)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }
        else // default sort
        {
            orderedQuery = filteredQuery
                .OrderByDescending(x => x.Rating != null ? x.Rating.TotalReviews : 0)
                .ThenByDescending(x => x.Rating != null ? x.Rating.AverageRating : 0m)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }

        var pagedProducts = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var pagedProductIds = pagedProducts.Select(x => x.Id).ToList();

        var pricesByProduct = await GetProductPricesByProductAsync(tenantId, pagedProductIds, currencyCode, now, cancellationToken);
        var ratingsByProduct = await GetRatingsByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var inventoryByProduct = await GetInventoryByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var optionsByProduct = await GetVariantOptionsByProductAsync(tenantId, pagedProductIds, cancellationToken);

        var productPage = pagedProducts.Select(product =>
        {
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

        var categoryQuery = from category in DbContext.Set<Category>().AsNoTracking()
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
                            };

        var searchModel = new StorefrontSearchReadModel
        {
            Products = new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = productPage, TotalCount = totalProducts, Page = request.Page, PageSize = request.PageSize
            },
            Categories = [],
            Collections = []
        };

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var lowerSearch = searchText.ToLower();
            categoryQuery = categoryQuery.Where(x => x.Category.CategoryName.ToLower().Contains(lowerSearch) ||
                                                     (x.Category.Description != null && x.Category.Description.ToLower().Contains(lowerSearch)));
            var categoryRows = await categoryQuery.ToListAsync(cancellationToken);

            var collectionQuery = DbContext.Set<Collection>().AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus &&
                            (!x.StartsAt.HasValue || x.StartsAt <= now) &&
                            (!x.EndsAt.HasValue || x.EndsAt >= now))
                .OrderBy(x => x.SortOrder)
                .AsQueryable();

            collectionQuery = collectionQuery.Where(x => x.CollectionName.ToLower().Contains(lowerSearch) ||
                                                         (x.Description != null && x.Description.ToLower().Contains(lowerSearch)));
            var collections = await collectionQuery.ToListAsync(cancellationToken);

            searchModel.Categories = categoryRows.Select(x => new StorefrontSearchMatchReadModel
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
            }).ToList();

            searchModel.Collections = collections.Select(x => new StorefrontSearchMatchReadModel
            {
                Id = x.Id, Name = x.CollectionName, Slug = x.CollectionSlug, Description = x.Description
            }).ToList();
        }

        return searchModel;
    }

}
