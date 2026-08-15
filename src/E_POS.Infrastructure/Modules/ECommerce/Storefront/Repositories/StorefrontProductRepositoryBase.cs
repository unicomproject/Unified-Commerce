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

public abstract class StorefrontProductRepositoryBase
{
    protected const string ActiveStatus = "ACTIVE";

    private readonly IMediaReadUrlResolver? _mediaReadUrlResolver;

    protected StorefrontProductRepositoryBase(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
    {
        DbContext = dbContext;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    protected EPosDbContext DbContext { get; }

    protected string? ResolveActiveMediaReadUrl(
        string? mediaStatus,
        string? containerName,
        string? storageKey,
        string? mediaPublicUrl)
    {
        return mediaStatus == ActiveStatus
            ? _mediaReadUrlResolver?.ResolveReadUrl(containerName, storageKey, mediaPublicUrl)
              ?? mediaPublicUrl?.Trim()
            : null;
    }

    protected static bool HasMediaReference(string? mediaPublicUrl, string? storageKey)
    {
        return !string.IsNullOrWhiteSpace(mediaPublicUrl) ||
               !string.IsNullOrWhiteSpace(storageKey);
    }

    protected async Task<string> ResolveCurrencyCodeAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await DbContext.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.BaseCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken) ?? "LKR";

    protected async Task<Product?> GetProductBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return null;
        }
        var isGuid = Guid.TryParse(normalizedSlug, out var parsedGuid);

        return await DbContext.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     (x.ProductSlug == normalizedSlug || (isGuid && x.Id == parsedGuid)) &&
                     x.Status == ActiveStatus &&
                     x.IsSellable,
                cancellationToken);
    }

    protected async Task<ProductRatingSummary?> GetRatingAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return await DbContext.Set<ProductRatingSummary>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId, cancellationToken);
    }

    protected async Task<Dictionary<Guid, ProductRatingSummary>> GetRatingsByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var ratings = await DbContext.Set<ProductRatingSummary>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        return ratings
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First());
    }

    protected async Task<decimal?> GetProductPriceAsync(Guid tenantId, Guid productId, string currencyCode, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await (from item in DbContext.Set<PriceListItem>().AsNoTracking()
                join priceList in DbContext.Set<PriceList>().AsNoTracking()
                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                where item.TenantId == tenantId &&
                      item.ProductId == productId &&
                      item.ProductVariantId == null &&
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
                        item.MinQuantity descending
                select (decimal?)item.SellingPrice)
            .FirstOrDefaultAsync(cancellationToken);
    }

    protected async Task<Dictionary<Guid, decimal?>> GetProductPricesByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, string currencyCode, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var priceRows = await (from item in DbContext.Set<PriceListItem>().AsNoTracking()
                join priceList in DbContext.Set<PriceList>().AsNoTracking()
                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                where item.TenantId == tenantId &&
                      productIds.Contains(item.ProductId) &&
                      item.Status == ActiveStatus &&
                      item.MinQuantity <= 1m &&
                      priceList.Status == ActiveStatus &&
                      priceList.CurrencyCode == currencyCode &&
                      (!priceList.ValidFrom.HasValue || priceList.ValidFrom <= now) &&
                      (!priceList.ValidUntil.HasValue || priceList.ValidUntil >= now) &&
                      (!item.ValidFrom.HasValue || item.ValidFrom <= now) &&
                      (!item.ValidUntil.HasValue || item.ValidUntil >= now)
                orderby item.ProductVariantId.HasValue,
                        priceList.IsDefaultPriceList descending,
                        priceList.Priority descending,
                        item.ValidFrom ?? DateTimeOffset.MinValue descending,
                        item.MinQuantity descending
                select new { item.ProductId, item.SellingPrice })
            .ToListAsync(cancellationToken);

        return priceRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => (decimal?)x.First().SellingPrice);
    }

    protected async Task<Dictionary<Guid, decimal?>> GetVariantPricesByVariantAsync(Guid tenantId, Guid productId, IReadOnlyCollection<Guid> variantIds, string currencyCode, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        var variantPriceRows = await (from item in DbContext.Set<PriceListItem>().AsNoTracking()
                join priceList in DbContext.Set<PriceList>().AsNoTracking()
                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                where item.TenantId == tenantId &&
                      item.ProductId == productId &&
                      item.ProductVariantId.HasValue &&
                      variantIds.Contains(item.ProductVariantId.Value) &&
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
                        item.MinQuantity descending
                select item)
            .ToListAsync(cancellationToken);

        return variantPriceRows
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(x => x.Key, x => (decimal?)x.First().SellingPrice);
    }

    protected async Task<Dictionary<Guid, string?>> GetPrimaryImagesByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var imageRows = await (from image in DbContext.Set<ProductImage>().AsNoTracking()
                               join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                   on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                   equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                               from mediaAsset in mediaAssets.DefaultIfEmpty()
                               where image.TenantId == tenantId &&
                                     productIds.Contains(image.ProductId) &&
                                     image.ProductVariantId == null &&
                                     image.IsPrimaryImage &&
                                     image.Status == ActiveStatus
                               orderby image.SortOrder
                               select new
                               {
                                   Image = image,
                                   MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                                   MediaContainerName = mediaAsset == null ? null : mediaAsset.ContainerName,
                                   MediaStorageKey = mediaAsset == null ? null : mediaAsset.StorageKey,
                                   MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                               })
            .ToListAsync(cancellationToken);

        return imageRows
            .Where(x => x.MediaStatus == ActiveStatus && HasMediaReference(x.MediaPublicUrl, x.MediaStorageKey))
            .GroupBy(x => x.Image.ProductId)
            .ToDictionary(
                x => x.Key,
                x =>
                {
                    var row = x.First();
                    return ResolveActiveMediaReadUrl(
                        row.MediaStatus,
                        row.MediaContainerName,
                        row.MediaStorageKey,
                        row.MediaPublicUrl);
                });
    }

    protected async Task<IReadOnlyList<StorefrontProductImageReadModel>> GetProductImagesAsync(Guid tenantId, Product product, CancellationToken cancellationToken)
    {
        var imageRows = await (from image in DbContext.Set<ProductImage>().AsNoTracking()
                               join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                   on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                   equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                               from mediaAsset in mediaAssets.DefaultIfEmpty()
                               where image.TenantId == tenantId &&
                                     image.ProductId == product.Id &&
                                     image.Status == ActiveStatus
                               orderby image.IsPrimaryImage descending, image.SortOrder, image.Id
                               select new
                               {
                                   Image = image,
                                   MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                                   MediaContainerName = mediaAsset == null ? null : mediaAsset.ContainerName,
                                   MediaStorageKey = mediaAsset == null ? null : mediaAsset.StorageKey,
                                   MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                               })
            .ToListAsync(cancellationToken);

        return imageRows
            .Select(row => StorefrontProductMapper.ToImageReadModel(
                row.Image,
                product.ProductName,
                ResolveActiveMediaReadUrl(
                    row.MediaStatus,
                    row.MediaContainerName,
                    row.MediaStorageKey,
                    row.MediaPublicUrl)))
            .ToList();
    }

    protected async Task<IReadOnlyList<ProductVariant>> GetProductVariantsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return await DbContext.Set<ProductVariant>()
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

    protected async Task<Dictionary<Guid, decimal>> GetInventoryByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var inventoryRows = await DbContext.Set<InventoryBalance>()
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

    protected async Task<IReadOnlyList<ProductInventoryRow>> GetProductInventoryRowsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        return await DbContext.Set<InventoryBalance>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .GroupBy(x => x.ProductVariantId)
            .Select(group => new ProductInventoryRow(
                group.Key,
                group.Sum(x => x.AvailableQuantity)))
            .ToListAsync(cancellationToken);
    }

    protected async Task<ProductVariantOptions> GetVariantOptionsAsync(Guid tenantId, Guid productId, IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        var options = await DbContext.Set<ProductOption>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == ActiveStatus)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.OptionName)
            .ToListAsync(cancellationToken);

        var optionIds = options.Select(x => x.Id).ToList();
        IReadOnlyList<ProductOptionValueMedia> optionValues;
        if (optionIds.Count == 0)
        {
            optionValues = [];
        }
        else
        {
            var optionValueRows = await (from optionValue in DbContext.Set<ProductOptionValue>().AsNoTracking()
                                         join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                             on new { optionValue.TenantId, MediaAssetId = optionValue.ImageMediaAssetId }
                                             equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                                         from mediaAsset in mediaAssets.DefaultIfEmpty()
                                         where optionValue.TenantId == tenantId &&
                                               optionIds.Contains(optionValue.ProductOptionId) &&
                                               optionValue.Status == ActiveStatus
                                         orderby optionValue.SortOrder, optionValue.ValueName
                                         select new
                                         {
                                             OptionValue = optionValue,
                                             MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                                             MediaContainerName = mediaAsset == null ? null : mediaAsset.ContainerName,
                                             MediaStorageKey = mediaAsset == null ? null : mediaAsset.StorageKey,
                                             MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                                         })
                .ToListAsync(cancellationToken);

            optionValues = optionValueRows
                .Select(x => new ProductOptionValueMedia(
                    x.OptionValue,
                    ResolveActiveMediaReadUrl(
                        x.MediaStatus,
                        x.MediaContainerName,
                        x.MediaStorageKey,
                        x.MediaPublicUrl)))
                .ToList();
        }

        var variantOptionLinks = variantIds.Count == 0
            ? []
            : await DbContext.Set<ProductVariantOptionValue>()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductId == productId &&
                    variantIds.Contains(x.ProductVariantId))
                .ToListAsync(cancellationToken);

        return new ProductVariantOptions(options, optionValues, variantOptionLinks);
    }

    protected async Task<Dictionary<Guid, ProductVariantOptions>> GetVariantOptionsByProductAsync(Guid tenantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var options = await DbContext.Set<ProductOption>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId) && x.Status == ActiveStatus)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.OptionName)
            .ToListAsync(cancellationToken);

        var optionIds = options.Select(x => x.Id).ToList();
        var optionValueRows = optionIds.Count == 0 ? [] : await (from optionValue in DbContext.Set<ProductOptionValue>().AsNoTracking()
                                     join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                         on new { optionValue.TenantId, MediaAssetId = optionValue.ImageMediaAssetId }
                                         equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                                     from mediaAsset in mediaAssets.DefaultIfEmpty()
                                     where optionValue.TenantId == tenantId &&
                                           optionIds.Contains(optionValue.ProductOptionId) &&
                                           optionValue.Status == ActiveStatus
                                     orderby optionValue.SortOrder, optionValue.ValueName
                                     select new
                                     {
                                         OptionValue = optionValue,
                                         MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                                         MediaContainerName = mediaAsset == null ? null : mediaAsset.ContainerName,
                                         MediaStorageKey = mediaAsset == null ? null : mediaAsset.StorageKey,
                                         MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                                     })
            .ToListAsync(cancellationToken);

        var optionValues = optionValueRows
            .Select(x => new ProductOptionValueMedia(
                x.OptionValue,
                ResolveActiveMediaReadUrl(
                    x.MediaStatus,
                    x.MediaContainerName,
                    x.MediaStorageKey,
                    x.MediaPublicUrl)))
            .ToList();

        // For Product Listings, we don't necessarily need the exact variant links, we just need the available option values
        // to show S, M, L or colors on the card.
        // If we do need variant links, we could fetch them, but for the basic list we just need the options and values.
        
        var result = new Dictionary<Guid, ProductVariantOptions>();
        foreach (var productId in productIds)
        {
            result[productId] = new ProductVariantOptions(
                options.Where(o => o.ProductId == productId).ToList(),
                optionValues.Where(v => options.Where(o => o.ProductId == productId).Select(o => o.Id).Contains(v.OptionValue.ProductOptionId)).ToList(),
                [] // No variant links needed for list view
            );
        }

        return result;
    }

    protected static IReadOnlyList<StorefrontProductOptionReadModel> BuildSelectableOptions(
        ProductVariantOptions variantOptions)
    {
        var linkedOptionValueIds = variantOptions.VariantOptionLinks.Select(x => x.ProductOptionValueId).ToHashSet();
        var selectableOptionValues = linkedOptionValueIds.Count == 0
            ? variantOptions.OptionValues
            : variantOptions.OptionValues.Where(x => linkedOptionValueIds.Contains(x.OptionValue.Id)).ToList();

        var valuesByOptionId = selectableOptionValues
            .GroupBy(x => x.OptionValue.Id)
            .Select(x => x.First())
            .GroupBy(x => x.OptionValue.ProductOptionId)
            .ToDictionary(
                x => x.Key, 
                x => x.OrderBy(v => v.OptionValue.SortOrder)
                      .ThenBy(v => StorefrontProductMapper.GetOptionDisplayName(v.OptionValue))
                      .Select(v => StorefrontProductMapper.ToOptionValueReadModel(v.OptionValue, v.MediaPublicUrl))
                      .ToList());

        return variantOptions.Options
            .Where(o => valuesByOptionId.ContainsKey(o.Id))
            .OrderBy(o => o.SortOrder)
            .Select(o => new StorefrontProductOptionReadModel
            {
                OptionName = o.OptionName,
                Values = valuesByOptionId[o.Id]
            })
            .ToList();
    }

    protected static IReadOnlyList<StorefrontProductVariantReadModel> BuildVariantModels(
        IReadOnlyList<ProductVariant> variants,
        decimal? productPrice,
        IReadOnlyDictionary<Guid, decimal?> variantPricesByVariant,
        IReadOnlyDictionary<Guid, decimal> inventoryByVariant,
        ProductVariantOptions variantOptions,
        string currencyCode)
    {
        var optionById = variantOptions.Options.ToDictionary(x => x.Id);
        var optionValueById = variantOptions.OptionValues.ToDictionary(x => x.OptionValue.Id, x => x.OptionValue);
        var variantOptionLinksByVariant = variantOptions.VariantOptionLinks
            .GroupBy(x => x.ProductVariantId)
            .ToDictionary(x => x.Key, x => x.ToList());

        return variants.Select(variant =>
        {
            variantPricesByVariant.TryGetValue(variant.Id, out var variantPrice);
            var variantHasInventory = inventoryByVariant.TryGetValue(variant.Id, out var variantAvailableQuantity);

            var optionValuesDict = new Dictionary<string, string>();
            if (variantOptionLinksByVariant.TryGetValue(variant.Id, out var links))
            {
                foreach (var link in links)
                {
                    if (optionById.TryGetValue(link.ProductOptionId, out var option) &&
                        optionValueById.TryGetValue(link.ProductOptionValueId, out var optionValue))
                    {
                        optionValuesDict[option.OptionName] = StorefrontProductMapper.GetOptionDisplayName(optionValue);
                    }
                }
            }

            return StorefrontProductMapper.ToVariantReadModel(
                variant,
                optionValuesDict,
                variantPrice ?? productPrice ?? 0m,
                !variantHasInventory || variantAvailableQuantity > 0m,
                currencyCode);
        }).ToList();
    }

    protected async Task<IReadOnlyList<string>> GetHighlightsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        var attributeRows = await (
                from attributeValue in DbContext.Set<ProductAttributeValue>().AsNoTracking()
                join attributeDefinition in DbContext.Set<ProductAttributeDefinition>().AsNoTracking()
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

    protected async Task<ReturnPolicy?> GetReturnPolicyAsync(Guid tenantId, Guid? returnPolicyId, CancellationToken cancellationToken)
    {
        if (!returnPolicyId.HasValue)
        {
            return null;
        }

        return await DbContext.Set<ReturnPolicy>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == returnPolicyId.Value &&
                x.Status == ActiveStatus,
                cancellationToken);
    }

    protected static IEnumerable<ProductListingSortItem> SortProductListings(IEnumerable<ProductListingSortItem> items, string? sort)
    {
        var normalizedSort = sort?.Trim().ToLowerInvariant();
        return normalizedSort switch
        {
            "price_asc" => items.OrderBy(x => x.Price ?? decimal.MaxValue).ThenBy(x => x.Product.ProductName),
            "price_desc" => items.OrderByDescending(x => x.Price ?? decimal.MinValue).ThenBy(x => x.Product.ProductName),
            "newest" => items.OrderByDescending(x => x.Product.CreatedAt).ThenBy(x => x.Product.ProductName),
            _ => items.OrderBy(x => x.SortOrder).ThenByDescending(x => x.ReviewCount).ThenByDescending(x => x.Rating).ThenBy(x => x.Product.ProductName)
        };
    }

    protected sealed record ProductListingSortItem(
        Product Product,
        decimal? Price,
        int SortOrder,
        decimal Rating,
        int ReviewCount);

    protected sealed record ProductInventoryRow(Guid? ProductVariantId, decimal AvailableQuantity);

    protected sealed record ProductOptionValueMedia(ProductOptionValue OptionValue, string? MediaPublicUrl);

    protected sealed record ProductVariantOptions(
        IReadOnlyList<ProductOption> Options,
        IReadOnlyList<ProductOptionValueMedia> OptionValues,
        IReadOnlyList<ProductVariantOptionValue> VariantOptionLinks);
}
