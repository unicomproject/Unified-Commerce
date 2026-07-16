using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontProductRepository : IStorefrontProductRepository
{
    private const string ActiveStatus = "ACTIVE";
    private readonly EPosDbContext _dbContext;

    public StorefrontProductRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(
        Guid tenantId,
        Guid categoryId,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var productCategoryRows = await (
                from productCategory in _dbContext.Set<ProductCategory>().AsNoTracking()
                join product in _dbContext.Set<Product>().AsNoTracking()
                    on new { productCategory.TenantId, productCategory.ProductId }
                    equals new { product.TenantId, ProductId = product.Id }
                where productCategory.TenantId == tenantId &&
                      productCategory.CategoryId == categoryId &&
                      product.Status == ActiveStatus &&
                      product.IsSellable
                select new
                {
                    Product = product,
                    productCategory.SortOrder,
                    productCategory.IsPrimaryCategory
                })
            .ToListAsync(cancellationToken);

        var products = productCategoryRows
            .GroupBy(x => x.Product.Id)
            .Select(g => g.OrderByDescending(x => x.IsPrimaryCategory).ThenBy(x => x.SortOrder).First())
            .ToList();

        if (products.Count == 0)
        {
            return new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        var productIds = products.Select(x => x.Product.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        var ratingsByProduct = await GetRatingsByProductAsync(tenantId, productIds, cancellationToken);
        var pricesByProduct = await GetProductPricesByProductAsync(tenantId, productIds, now, cancellationToken);
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, productIds, cancellationToken);
        var inventoryByProduct = await GetInventoryByProductAsync(tenantId, productIds, cancellationToken);

        var productModels = products.Select(row =>
        {
            var product = row.Product;
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            pricesByProduct.TryGetValue(product.Id, out var sellingPrice);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            var hasInventory = inventoryByProduct.TryGetValue(product.Id, out var availableQuantity);
            var averageRating = rating?.AverageRating ?? 0m;
            var reviewCount = rating?.TotalReviews ?? 0;

            return new ProductListingSortItem(
                StorefrontProductMapper.ToListReadModel(
                    product,
                    sellingPrice,
                    primaryImageUrl,
                    averageRating,
                    reviewCount,
                    !hasInventory || availableQuantity > 0m),
                row.SortOrder,
                product.CreatedAt,
                averageRating,
                reviewCount);
        }).ToList();

        var items = SortProductListings(productModels, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Model)
            .ToList();

        return new StorefrontPagedReadModel<StorefrontProductListReadModel>
        {
            Items = items,
            TotalCount = productModels.Count,
            Page = page,
            PageSize = pageSize
        };
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
        var rating = await GetRatingAsync(tenantId, productId, cancellationToken);
        var productPrice = await GetProductPriceAsync(tenantId, productId, now, cancellationToken);
        var images = await GetProductImagesAsync(tenantId, product, cancellationToken);
        var variants = await GetProductVariantsAsync(tenantId, productId, cancellationToken);
        var variantIds = variants.Select(x => x.Id).ToList();
        var variantPricesByVariant = await GetVariantPricesByVariantAsync(tenantId, productId, variantIds, now, cancellationToken);
        var inventoryRows = await GetProductInventoryRowsAsync(tenantId, productId, cancellationToken);
        var inventoryByVariant = inventoryRows
            .Where(x => x.ProductVariantId.HasValue)
            .ToDictionary(x => x.ProductVariantId!.Value, x => x.AvailableQuantity);
        var variantOptions = await GetVariantOptionsAsync(tenantId, productId, variantIds, cancellationToken);

        var colours = BuildSelectableOptionValues(variantOptions, StorefrontProductMapper.IsColourOption);
        var sizes = BuildSelectableOptionValues(variantOptions, StorefrontProductMapper.IsSizeOption);
        var variantModels = BuildVariantModels(variants, productPrice, variantPricesByVariant, inventoryByVariant, variantOptions);
        var highlights = await GetHighlightsAsync(tenantId, productId, cancellationToken);
        var returnInfo = StorefrontProductMapper.BuildReturnInfo(await GetReturnPolicyAsync(tenantId, product.ReturnPolicyId, cancellationToken));
        var isInStock = variantModels.Count > 0
            ? variantModels.Any(x => x.IsInStock)
            : !inventoryRows.Any() || inventoryRows.Sum(x => x.AvailableQuantity) > 0m;
        var detailPrice = productPrice ?? variantModels.Select(x => (decimal?)x.Price).FirstOrDefault() ?? 0m;

        return StorefrontProductMapper.ToDetailReadModel(
            product,
            detailPrice,
            rating,
            isInStock,
            images,
            colours,
            sizes,
            variantModels,
            highlights,
            returnInfo);
    }

    public async Task<StorefrontSearchReadModel> SearchAsync(
        Guid tenantId,
        StorefrontSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var searchText = request.SearchText?.Trim();
        var normalizedSearch = searchText?.ToUpperInvariant();
        var products = await _dbContext.Set<Product>()
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
            var categoryProductIds = await _dbContext.Set<ProductCategory>()
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
                from option in _dbContext.Set<ProductOption>().AsNoTracking()
                join value in _dbContext.Set<ProductOptionValue>().AsNoTracking()
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
        var prices = await GetProductPricesByProductAsync(tenantId, productIds, now, cancellationToken);
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
                    rating?.TotalReviews ?? 0, !hasInventory || quantity > 0m),
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

        var categories = await _dbContext.Set<Category>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var collections = await _dbContext.Set<Collection>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == ActiveStatus &&
                        (!x.StartsAt.HasValue || x.StartsAt <= now) &&
                        (!x.EndsAt.HasValue || x.EndsAt >= now))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            categories = categories.Where(x => x.CategoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                               (x.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            collections = collections.Where(x => x.CollectionName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                                  (x.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        else
        {
            categories = [];
            collections = [];
        }

        return new StorefrontSearchReadModel
        {
            Products = new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = productPage, TotalCount = totalProducts, Page = request.Page, PageSize = request.PageSize
            },
            Categories = categories.Select(x => new StorefrontSearchMatchReadModel
            {
                Id = x.Id, Name = x.CategoryName, Slug = x.CategorySlug, Description = x.Description, ImageUrl = x.ImageUrl
            }).ToList(),
            Collections = collections.Select(x => new StorefrontSearchMatchReadModel
            {
                Id = x.Id, Name = x.CollectionName, Slug = x.CollectionSlug, Description = x.Description
            }).ToList()
        };
    }

    public async Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string? PrimaryImageUrl)>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var products = await _dbContext.Set<Product>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == ActiveStatus && p.IsSellable)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return [];
        }

        var productIds = products.Select(p => p.Id).ToList();
        var ratingsByProduct = await GetRatingsByProductAsync(tenantId, productIds, cancellationToken);
        var pricesByProduct = await GetProductPricesByProductAsync(tenantId, productIds, now, cancellationToken);
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, productIds, cancellationToken);

        return products.Select(product =>
        {
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            pricesByProduct.TryGetValue(product.Id, out var sellingPrice);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            return (product, rating, sellingPrice, primaryImageUrl);
        });
    }

    private async Task<Product?> GetProductBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return null;
        }

        return await _dbContext.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ProductSlug == normalizedSlug &&
                     x.Status == ActiveStatus &&
                     x.IsSellable,
                cancellationToken);
    }

    private async Task<ProductRatingSummary?> GetRatingAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ProductRatingSummary>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId, cancellationToken);
    }

    private async Task<Dictionary<Guid, ProductRatingSummary>> GetRatingsByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var ratings = await _dbContext.Set<ProductRatingSummary>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        return ratings
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First());
    }

    private async Task<decimal?> GetProductPriceAsync(Guid tenantId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<PriceListItem>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.ProductVariantId == null &&
                x.Status == ActiveStatus &&
                x.MinQuantity <= 1m &&
                (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .OrderByDescending(x => x.ValidFrom ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.MinQuantity)
            .Select(x => (decimal?)x.SellingPrice)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal?>> GetProductPricesByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var priceRows = await _dbContext.Set<PriceListItem>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                productIds.Contains(x.ProductId) &&
                x.Status == ActiveStatus &&
                x.ProductVariantId == null &&
                x.MinQuantity <= 1m &&
                (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .OrderByDescending(x => x.ValidFrom ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.MinQuantity)
            .ToListAsync(cancellationToken);

        return priceRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => (decimal?)x.First().SellingPrice);
    }

    private async Task<Dictionary<Guid, decimal?>> GetVariantPricesByVariantAsync(Guid tenantId, Guid productId, IReadOnlyCollection<Guid> variantIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        var variantPriceRows = await _dbContext.Set<PriceListItem>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.ProductVariantId.HasValue &&
                variantIds.Contains(x.ProductVariantId.Value) &&
                x.Status == ActiveStatus &&
                x.MinQuantity <= 1m &&
                (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .OrderByDescending(x => x.ValidFrom ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.MinQuantity)
            .ToListAsync(cancellationToken);

        return variantPriceRows
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(x => x.Key, x => (decimal?)x.First().SellingPrice);
    }

    private async Task<Dictionary<Guid, string?>> GetPrimaryImagesByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var imageRows = await _dbContext.Set<ProductImage>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                productIds.Contains(x.ProductId) &&
                x.ProductVariantId == null &&
                x.IsPrimaryImage &&
                x.Status == ActiveStatus)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        return imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First().ImageUrl);
    }

    private async Task<IReadOnlyList<StorefrontProductImageReadModel>> GetProductImagesAsync(Guid tenantId, Product product, CancellationToken cancellationToken)
    {
        var imageRows = await _dbContext.Set<ProductImage>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == product.Id &&
                x.Status == ActiveStatus)
            .OrderByDescending(x => x.IsPrimaryImage)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return imageRows
            .Select(image => StorefrontProductMapper.ToImageReadModel(image, product.ProductName))
            .ToList();
    }

    private async Task<IReadOnlyList<ProductVariant>> GetProductVariantsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ProductVariant>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ActiveStatus &&
                x.IsSellable)
            .OrderByDescending(x => x.IsDefaultVariant)
            .ThenBy(x => x.VariantName)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal>> GetInventoryByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var inventoryRows = await _dbContext.Set<InventoryBalance>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                AvailableQuantity = group.Sum(x => x.AvailableQuantity)
            })
            .ToListAsync(cancellationToken);

        return inventoryRows.ToDictionary(x => x.ProductId, x => x.AvailableQuantity);
    }

    private async Task<IReadOnlyList<ProductInventoryRow>> GetProductInventoryRowsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<InventoryBalance>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .GroupBy(x => x.ProductVariantId)
            .Select(group => new ProductInventoryRow(
                group.Key,
                group.Sum(x => x.AvailableQuantity)))
            .ToListAsync(cancellationToken);
    }

    private async Task<ProductVariantOptions> GetVariantOptionsAsync(Guid tenantId, Guid productId, IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        var options = await _dbContext.Set<ProductOption>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == ActiveStatus)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.OptionName)
            .ToListAsync(cancellationToken);

        var optionIds = options.Select(x => x.Id).ToList();
        var optionValues = optionIds.Count == 0
            ? []
            : await _dbContext.Set<ProductOptionValue>()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    optionIds.Contains(x.ProductOptionId) &&
                    x.Status == ActiveStatus)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.ValueName)
                .ToListAsync(cancellationToken);

        var variantOptionLinks = variantIds.Count == 0
            ? []
            : await _dbContext.Set<ProductVariantOptionValue>()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductId == productId &&
                    variantIds.Contains(x.ProductVariantId))
                .ToListAsync(cancellationToken);

        return new ProductVariantOptions(options, optionValues, variantOptionLinks);
    }

    private static IReadOnlyList<StorefrontProductOptionValueReadModel> BuildSelectableOptionValues(
        ProductVariantOptions variantOptions,
        Func<ProductOption, bool> optionPredicate)
    {
        var optionIds = variantOptions.Options.Where(optionPredicate).Select(x => x.Id).ToHashSet();
        var linkedOptionValueIds = variantOptions.VariantOptionLinks.Select(x => x.ProductOptionValueId).ToHashSet();
        var selectableOptionValues = linkedOptionValueIds.Count == 0
            ? variantOptions.OptionValues
            : variantOptions.OptionValues.Where(x => linkedOptionValueIds.Contains(x.Id)).ToList();

        return selectableOptionValues
            .Where(x => optionIds.Contains(x.ProductOptionId))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(StorefrontProductMapper.GetOptionDisplayName)
            .Select(StorefrontProductMapper.ToOptionValueReadModel)
            .ToList();
    }

    private static IReadOnlyList<StorefrontProductVariantReadModel> BuildVariantModels(
        IReadOnlyList<ProductVariant> variants,
        decimal? productPrice,
        IReadOnlyDictionary<Guid, decimal?> variantPricesByVariant,
        IReadOnlyDictionary<Guid, decimal> inventoryByVariant,
        ProductVariantOptions variantOptions)
    {
        var optionById = variantOptions.Options.ToDictionary(x => x.Id);
        var optionValueById = variantOptions.OptionValues.ToDictionary(x => x.Id);
        var variantOptionLinksByVariant = variantOptions.VariantOptionLinks
            .GroupBy(x => x.ProductVariantId)
            .ToDictionary(x => x.Key, x => x.ToList());

        return variants.Select(variant =>
        {
            variantPricesByVariant.TryGetValue(variant.Id, out var variantPrice);
            var variantHasInventory = inventoryByVariant.TryGetValue(variant.Id, out var variantAvailableQuantity);
            var colour = StorefrontProductMapper.GetVariantOptionValue(variant.Id, variantOptionLinksByVariant, optionById, optionValueById, StorefrontProductMapper.IsColourOption);
            var size = StorefrontProductMapper.GetVariantOptionValue(variant.Id, variantOptionLinksByVariant, optionById, optionValueById, StorefrontProductMapper.IsSizeOption);

            return StorefrontProductMapper.ToVariantReadModel(
                variant,
                colour,
                size,
                variantPrice ?? productPrice ?? 0m,
                !variantHasInventory || variantAvailableQuantity > 0m);
        }).ToList();
    }

    private async Task<IReadOnlyList<string>> GetHighlightsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        var attributeRows = await (
                from attributeValue in _dbContext.Set<ProductAttributeValue>().AsNoTracking()
                join attributeDefinition in _dbContext.Set<ProductAttributeDefinition>().AsNoTracking()
                    on new { attributeValue.TenantId, AttributeDefinitionId = attributeValue.AttributeDefinitionId }
                    equals new { attributeDefinition.TenantId, AttributeDefinitionId = attributeDefinition.Id }
                where attributeValue.TenantId == tenantId &&
                      attributeValue.ProductId == productId &&
                      attributeValue.ProductVariantId == null &&
                      attributeValue.Status == ActiveStatus &&
                      attributeDefinition.Status == ActiveStatus
                orderby attributeDefinition.SortOrder, attributeDefinition.AttributeName
                select new
                {
                    attributeDefinition.AttributeName,
                    attributeValue.AttributeValueText,
                    attributeValue.AttributeValueNumber,
                    attributeValue.AttributeValueBoolean,
                    attributeValue.AttributeValueDate
                })
            .ToListAsync(cancellationToken);

        return attributeRows
            .Select(x => StorefrontProductMapper.FormatAttributeValue(x.AttributeName, x.AttributeValueText, x.AttributeValueNumber, x.AttributeValueBoolean, x.AttributeValueDate))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Take(10)
            .ToList();
    }

    private async Task<ReturnPolicy?> GetReturnPolicyAsync(Guid tenantId, Guid? returnPolicyId, CancellationToken cancellationToken)
    {
        if (!returnPolicyId.HasValue)
        {
            return null;
        }

        return await _dbContext.Set<ReturnPolicy>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == returnPolicyId.Value &&
                x.Status == ActiveStatus,
                cancellationToken);
    }

    private static IEnumerable<ProductListingSortItem> SortProductListings(IReadOnlyList<ProductListingSortItem> productModels, string? sort)
    {
        var normalizedSort = sort?.Trim().ToLowerInvariant();
        return normalizedSort switch
        {
            "price_asc" => productModels.OrderBy(x => x.Model.Price).ThenBy(x => x.Model.Name),
            "price_desc" => productModels.OrderByDescending(x => x.Model.Price).ThenBy(x => x.Model.Name),
            "newest" => productModels.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Model.Name),
            _ => productModels.OrderBy(x => x.SortOrder).ThenByDescending(x => x.ReviewCount).ThenByDescending(x => x.Rating).ThenBy(x => x.Model.Name)
        };
    }

    private sealed record ProductListingSortItem(
        StorefrontProductListReadModel Model,
        int SortOrder,
        DateTimeOffset CreatedAt,
        decimal Rating,
        int ReviewCount);

    private sealed record ProductInventoryRow(Guid? ProductVariantId, decimal AvailableQuantity);

    private sealed record ProductVariantOptions(
        IReadOnlyList<ProductOption> Options,
        IReadOnlyList<ProductOptionValue> OptionValues,
        IReadOnlyList<ProductVariantOptionValue> VariantOptionLinks);
}
