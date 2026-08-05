using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontProductSearchRepository : StorefrontProductRepositoryBase, IStorefrontProductSearchRepository
{
    public StorefrontProductSearchRepository(EPosDbContext dbContext) : base(dbContext)
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

        productIds = products.Select(x => x.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var prices = await GetProductPricesByProductAsync(tenantId, productIds, currencyCode, now, cancellationToken);
        var ratings = await GetRatingsByProductAsync(tenantId, productIds, cancellationToken);
        var images = await GetPrimaryImagesByProductAsync(tenantId, productIds, cancellationToken);
        var inventory = await GetInventoryByProductAsync(tenantId, productIds, cancellationToken);

        var listingItems = products.Select(product =>
        {
            prices.TryGetValue(product.Id, out var price);
            ratings.TryGetValue(product.Id, out var rating);
            images.TryGetValue(product.Id, out var image);
            var hasInventory = inventory.TryGetValue(product.Id, out var quantity);
            return new ProductListingSortItem(
                StorefrontProductMapper.ToListReadModel(product, price, image, rating?.AverageRating ?? 0m,
                    rating?.TotalReviews ?? 0, !hasInventory || quantity > 0m, currencyCode),
                0, product.CreatedAt, rating?.AverageRating ?? 0m, rating?.TotalReviews ?? 0);
        }).ToList();

        if (request.MinPrice.HasValue) listingItems = listingItems.Where(x => x.Model.Price >= request.MinPrice.Value).ToList();
        if (request.MaxPrice.HasValue) listingItems = listingItems.Where(x => x.Model.Price <= request.MaxPrice.Value).ToList();
        if (request.InStock.HasValue) listingItems = listingItems.Where(x => x.Model.IsInStock == request.InStock.Value).ToList();

        var totalProducts = listingItems.Count;
        var productPage = SortProductListings(listingItems, request.Sort)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => x.Model)
            .ToList();

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
                ImageUrl = x.MediaPublicUrl
            }).ToList(),
            Collections = collections.Select(x => new StorefrontSearchMatchReadModel
            {
                Id = x.Id, Name = x.CollectionName, Slug = x.CollectionSlug, Description = x.Description
            }).ToList()
        };
    }

}
