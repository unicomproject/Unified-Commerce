using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class PosProductCatalogRepository : IPosProductCatalogRepository
{
    private const string ActiveImageStatus = "ACTIVE";

    private readonly EPosDbContext _dbContext;
    private readonly IConfiguration? _configuration;

    public PosProductCatalogRepository(
        EPosDbContext dbContext,
        IConfiguration? configuration = null)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<PosProductCatalogRepositoryResult> ListProductsAsync(
        Guid tenantId,
        Guid deviceId,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken,
        Guid? outletId = null,
        string? segment = null)
    {
        var deviceOutletId = await (from device in _dbContext.PosDevices.AsNoTracking()
                                    join outlet in _dbContext.Outlets.AsNoTracking()
                                        on new { device.TenantId, Id = device.OutletId }
                                        equals new { outlet.TenantId, outlet.Id }
                                    where device.TenantId == tenantId && device.Id == deviceId &&
                                          device.Status == "ACTIVE" && device.IsTrusted && outlet.Status == "ACTIVE"
                                    select (Guid?)outlet.Id).FirstOrDefaultAsync(cancellationToken);

        if (!deviceOutletId.HasValue)
        {
            return new PosProductCatalogRepositoryResult("pos_products.device_not_found", []);
        }

        var defaultPriceListId = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Guid? popularCollectionId = null;
        var isPopular = string.Equals(segment, "popular", StringComparison.OrdinalIgnoreCase);
        if (isPopular)
        {
            popularCollectionId = await _dbContext.Collections
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CollectionCode == "POS_POPULAR" && x.Status == "ACTIVE")
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!popularCollectionId.HasValue)
            {
                return new PosProductCatalogRepositoryResult(null, []);
            }
        }

        var isFrequentlySold = string.Equals(segment, "frequently-sold", StringComparison.OrdinalIgnoreCase);
        List<Guid> rankedProductIds = [];
        if (isFrequentlySold)
        {
            var lookbackDays = 30;
            var lookbackStr = _configuration?["PosProducts:FrequentlySold:LookbackDays"];
            if (!string.IsNullOrWhiteSpace(lookbackStr) && int.TryParse(lookbackStr, out var parsedDays) && parsedDays > 0)
            {
                lookbackDays = parsedDays;
            }

            var limit = 20;
            var limitStr = _configuration?["PosProducts:FrequentlySold:Limit"];
            if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsedLimit) && parsedLimit > 0)
            {
                limit = parsedLimit;
            }
            limit = Math.Min(limit, 100);

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-lookbackDays);

            var query = from o in _dbContext.SalesOrders.AsNoTracking()
                        join l in _dbContext.SalesOrderLines.AsNoTracking() on o.Id equals l.SalesOrderId
                        where o.TenantId == tenantId &&
                              o.ReportingOutletId == deviceOutletId.Value &&
                              o.Status == "COMPLETED" &&
                              o.CompletedAt.HasValue &&
                              o.CompletedAt.Value >= cutoffDate &&
                              l.TenantId == tenantId
                        select new { l.ProductId, l.Quantity, l.CancelledQuantity, l.ReturnedQuantity, o.CompletedAt, SalesOrderId = l.SalesOrderId };

            var grouped = from x in query
                          group x by x.ProductId into g
                          select new
                          {
                              ProductId = g.Key,
                              NetQty = g.Sum(i => i.Quantity - i.CancelledQuantity - i.ReturnedQuantity > 0 
                                  ? i.Quantity - i.CancelledQuantity - i.ReturnedQuantity 
                                  : 0),
                              TransactionCount = g.Select(i => i.SalesOrderId).Distinct().Count(),
                              LastCompletedAt = g.Max(i => i.CompletedAt)
                          };

            var rawRanked = await grouped
                .Where(x => x.NetQty > 0)
                .OrderByDescending(x => x.NetQty)
                .ThenByDescending(x => x.TransactionCount)
                .ThenByDescending(x => x.LastCompletedAt)
                .ThenByDescending(x => x.ProductId)
                .Take(limit)
                .ToListAsync(cancellationToken);

            rankedProductIds = rawRanked.Select(x => x.ProductId).ToList();

            if (rankedProductIds.Count == 0)
            {
                return new PosProductCatalogRepositoryResult(null, []);
            }
        }

        var isOffers = string.Equals(segment, "offers", StringComparison.OrdinalIgnoreCase);
        var salesChannelId = await _dbContext.SalesChannels
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PlatformSalesChannelId == E_POS.Infrastructure.Persistence.Seed.PlatformSalesChannelSeedConstants.PhysicalChannelId && x.Status == "ACTIVE")
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (salesChannelId == Guid.Empty)
        {
            salesChannelId = Guid.Parse("bbbbbbbb-0006-4000-8000-000000000001");
        }

        HashSet<Guid>? candidateProductIds = null;
        List<PriceList> eligiblePriceLists = [];
        List<DiscountPolicy> eligiblePolicies = [];
        List<DiscountPolicyTarget> policyTargets = [];
        List<DiscountPolicyCondition> policyConditions = [];
        Dictionary<Guid, string> discountTypes = [];
        List<PriceListItem> priceListItems = [];

        var nowTime = DateTimeOffset.UtcNow;
        if (isOffers)
        {
            var activePls = await _dbContext.PriceLists
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" &&
                            (!x.ValidFrom.HasValue || x.ValidFrom <= nowTime) &&
                            (!x.ValidUntil.HasValue || x.ValidUntil >= nowTime))
                .ToListAsync(cancellationToken);

            foreach (var pl in activePls)
            {
                var hasOutletLimits = await _dbContext.PriceListOutlets.AnyAsync(po => po.PriceListId == pl.Id && po.Status == "ACTIVE", cancellationToken);
                if (hasOutletLimits)
                {
                    var matchesOutlet = await _dbContext.PriceListOutlets.AnyAsync(po => po.PriceListId == pl.Id && po.OutletId == deviceOutletId.Value && po.Status == "ACTIVE", cancellationToken);
                    if (!matchesOutlet) continue;
                }

                var hasChannelLimits = await _dbContext.PriceListChannels.AnyAsync(pc => pc.PriceListId == pl.Id && pc.Status == "ACTIVE", cancellationToken);
                if (hasChannelLimits)
                {
                    var matchesChannel = await _dbContext.PriceListChannels.AnyAsync(pc => pc.PriceListId == pl.Id && pc.SalesChannelId == salesChannelId && pc.Status == "ACTIVE", cancellationToken);
                    if (!matchesChannel) continue;
                }

                eligiblePriceLists.Add(pl);
            }

            var offersEligiblePlIds = eligiblePriceLists.Select(x => x.Id).ToList();
            var specialPriceProductIds = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            offersEligiblePlIds.Contains(x.PriceListId) &&
                            x.Status == "ACTIVE" &&
                            (!x.ValidFrom.HasValue || x.ValidFrom <= nowTime) &&
                            (!x.ValidUntil.HasValue || x.ValidUntil >= nowTime) &&
                            x.CompareAtPrice.HasValue && x.CompareAtPrice > x.SellingPrice)
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var activePols = await _dbContext.DiscountPolicies
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.DiscountScope == "LINE" &&
                            (!x.StartsAt.HasValue || x.StartsAt <= nowTime) &&
                            (!x.EndsAt.HasValue || x.EndsAt >= nowTime))
                .ToListAsync(cancellationToken);

            foreach (var dp in activePols)
            {
                var hasOutletLimits = await _dbContext.DiscountPolicyOutlets.AnyAsync(po => po.DiscountPolicyId == dp.Id && po.Status == "ACTIVE", cancellationToken);
                if (hasOutletLimits)
                {
                    var matchesOutlet = await _dbContext.DiscountPolicyOutlets.AnyAsync(po => po.DiscountPolicyId == dp.Id && po.OutletId == deviceOutletId.Value && po.Status == "ACTIVE", cancellationToken);
                    if (!matchesOutlet) continue;
                }

                var hasChannelLimits = await _dbContext.DiscountPolicyChannels.AnyAsync(pc => pc.DiscountPolicyId == dp.Id && pc.Status == "ACTIVE", cancellationToken);
                if (hasChannelLimits)
                {
                    var matchesChannel = await _dbContext.DiscountPolicyChannels.AnyAsync(pc => pc.DiscountPolicyId == dp.Id && pc.SalesChannelId == salesChannelId && pc.Status == "ACTIVE", cancellationToken);
                    if (!matchesChannel) continue;
                }

                eligiblePolicies.Add(dp);
            }

            var offersEligiblePolIds = eligiblePolicies.Select(x => x.Id).ToList();
            var targetProducts = await _dbContext.DiscountPolicyTargets
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && offersEligiblePolIds.Contains(x.DiscountPolicyId) && x.Status == "ACTIVE")
                .ToListAsync(cancellationToken);

            var hasUntargetedPolicy = false;
            foreach (var policy in eligiblePolicies)
            {
                var hasInclude = targetProducts.Any(t => t.DiscountPolicyId == policy.Id && t.TargetMode == "INCLUDE");
                if (!hasInclude)
                {
                    hasUntargetedPolicy = true;
                    break;
                }
            }

            if (hasUntargetedPolicy)
            {
                candidateProductIds = null;
            }
            else
            {
                candidateProductIds = new HashSet<Guid>();
                foreach (var t in targetProducts)
                {
                    if (t.TargetMode == "INCLUDE")
                    {
                        if (t.TargetType == "PRODUCT" && t.ProductId.HasValue)
                        {
                            candidateProductIds.Add(t.ProductId.Value);
                        }
                        else if (t.TargetType == "PRODUCT_VARIANT" && t.ProductVariantId.HasValue)
                        {
                            var pId = await _dbContext.ProductVariants
                                .Where(v => v.Id == t.ProductVariantId.Value)
                                .Select(v => v.ProductId)
                                .FirstOrDefaultAsync(cancellationToken);
                            if (pId != Guid.Empty) candidateProductIds.Add(pId);
                        }
                        else if (t.TargetType == "CATEGORY" && t.CategoryId.HasValue)
                        {
                            var pIds = await _dbContext.ProductCategories
                                .Where(pc => pc.CategoryId == t.CategoryId.Value && pc.TenantId == tenantId)
                                .Select(pc => pc.ProductId)
                                .ToListAsync(cancellationToken);
                            foreach (var pid in pIds) candidateProductIds.Add(pid);
                        }
                        else if (t.TargetType == "BRAND" && t.BrandId.HasValue)
                        {
                            var pIds = await _dbContext.Products
                                .Where(p => p.BrandId == t.BrandId.Value && p.TenantId == tenantId)
                                .Select(p => p.Id)
                                .ToListAsync(cancellationToken);
                            foreach (var pid in pIds) candidateProductIds.Add(pid);
                        }
                        else if (t.TargetType == "COLLECTION" && t.CollectionId.HasValue)
                        {
                            var pIds = await _dbContext.ProductCollections
                                .Where(pc => pc.CollectionId == t.CollectionId.Value && pc.TenantId == tenantId)
                                .Select(pc => pc.ProductId)
                                .ToListAsync(cancellationToken);
                            foreach (var pid in pIds) candidateProductIds.Add(pid);
                        }
                    }
                }

                foreach (var pid in specialPriceProductIds)
                {
                    candidateProductIds.Add(pid);
                }
            }

            if (candidateProductIds != null && candidateProductIds.Count == 0)
            {
                return new PosProductCatalogRepositoryResult(null, []);
            }
        }

        var productsQuery = _dbContext.Products
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable);

        if (isPopular)
        {
            productsQuery = productsQuery.Where(x =>
                _dbContext.ProductCollections.Any(pc => pc.TenantId == tenantId && pc.CollectionId == popularCollectionId!.Value && pc.ProductId == x.Id));
        }
        else if (isFrequentlySold)
        {
            productsQuery = productsQuery.Where(x => rankedProductIds.Contains(x.Id));
        }
        else if (isOffers)
        {
            productsQuery = productsQuery.Where(x => candidateProductIds == null || candidateProductIds.Contains(x.Id));
        }

        if (categoryId is { } requestedCategoryId && requestedCategoryId != Guid.Empty)
        {
            productsQuery = productsQuery.Where(product =>
                _dbContext.ProductCategories.Any(link =>
                    link.TenantId == tenantId &&
                    link.ProductId == product.Id &&
                    link.CategoryId == requestedCategoryId));
        }

        var searchFilter = await ApplySearchFilterAsync(
            productsQuery,
            tenantId,
            search,
            cancellationToken);
        productsQuery = searchFilter.Products;
        var matchedVariantIds = searchFilter.MatchedVariantIds;

        List<Domain.Modules.Tenant.CatalogProduct.Entities.Product> products;
        if (isPopular)
        {
            products = await (from p in productsQuery
                              join pc in _dbContext.ProductCollections.AsNoTracking()
                                  on p.Id equals pc.ProductId
                              where pc.TenantId == tenantId && pc.CollectionId == popularCollectionId!.Value
                              orderby pc.SortOrder
                              select p)
                             .ToListAsync(cancellationToken);
        }
        else if (isFrequentlySold)
        {
            var rawProducts = await productsQuery.ToListAsync(cancellationToken);
            products = rawProducts
                .OrderBy(x => rankedProductIds.IndexOf(x.Id))
                .ToList();
        }
        else
        {
            products = await productsQuery
                .OrderBy(x => x.ProductName)
                .ToListAsync(cancellationToken);
        }

        if (products.Count == 0)
        {
            return new PosProductCatalogRepositoryResult(null, []);
        }

        var productIds = products.Select(x => x.Id).ToList();
        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                productIds.Contains(x.ProductId) &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .ToListAsync(cancellationToken);

        var variantsByProduct = variants
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var variantIds = variants.Select(x => x.Id).ToList();
        var pricesByVariant = new Dictionary<Guid, decimal>();
        if (defaultPriceListId.HasValue && variantIds.Count > 0)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == defaultPriceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
                .ToListAsync(cancellationToken);

            pricesByVariant = priceRows.ToDictionary(x => x.VariantId, x => x.SellingPrice);
        }

        var categoryRows = await (
                from link in _dbContext.ProductCategories.AsNoTracking()
                join category in _dbContext.Categories.AsNoTracking()
                    on link.CategoryId equals category.Id
                where link.TenantId == tenantId && productIds.Contains(link.ProductId)
                orderby link.IsPrimaryCategory descending, link.SortOrder
                select new { link.ProductId, link.CategoryId, category.CategoryName })
            .ToListAsync(cancellationToken);

        var categoryByProduct = categoryRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First());

        var inventoryByVariant = new Dictionary<Guid, decimal>();
        if (variantIds.Count > 0)
        {
            var scopedOutletId = outletId is { } requestedOutletId && requestedOutletId != Guid.Empty
                ? requestedOutletId
                : deviceOutletId.Value;
            var inventoryRows = await (
                        from balance in _dbContext.InventoryBalances.AsNoTracking()
                        join location in _dbContext.InventoryLocations.AsNoTracking()
                            on balance.InventoryLocationId equals location.Id
                        where balance.TenantId == tenantId &&
                              location.TenantId == tenantId &&
                              location.OutletId == scopedOutletId &&
                              location.IsSellableLocation &&
                              location.Status == "ACTIVE" &&
                              balance.ProductVariantId.HasValue &&
                              variantIds.Contains(balance.ProductVariantId.Value)
                        group balance by balance.ProductVariantId!.Value
                        into groupRows
                        select new
                        {
                            VariantId = groupRows.Key,
                            AvailableQuantity = groupRows.Sum(x => x.AvailableQuantity),
                        })
                .ToListAsync(cancellationToken);

            inventoryByVariant = inventoryRows.ToDictionary(
                x => x.VariantId,
                x => x.AvailableQuantity);
        }

        var barcodeByVariant = new Dictionary<Guid, string>();
        if (variantIds.Count > 0)
        {
            var barcodeRows = await _dbContext.ProductBarcodes
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .OrderByDescending(x => x.IsPrimaryBarcode)
                .ThenBy(x => x.Id)
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.Barcode })
                .ToListAsync(cancellationToken);
            barcodeByVariant = barcodeRows
                .GroupBy(x => x.VariantId)
                .ToDictionary(g => g.Key, g => g.First().Barcode);
        }

        var imageRows = await (from image in _dbContext.ProductImages.AsNoTracking()
                               join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                                   on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                   equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                               from mediaAsset in mediaAssets.DefaultIfEmpty()
                               where image.TenantId == tenantId &&
                                     productIds.Contains(image.ProductId) &&
                                     image.Status == ActiveImageStatus
                               orderby image.IsPrimaryImage ? 0 : 1, image.SortOrder
                               select new
                               {
                                   image.ProductId,
                                   MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                               })
            .ToListAsync(cancellationToken);

        var imageByProduct = imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                x => x.Key,
                x => x.First().MediaPublicUrl);
        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            productIds,
            cancellationToken);

        var reorderRulesByProduct = new Dictionary<Guid, decimal?>();
        if (productIds.Count > 0)
        {
            var rules = await _dbContext.InventoryReorderRules
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId) && x.Status == "ACTIVE")
                .Select(x => new { x.ProductId, x.MinStockQuantity })
                .ToListAsync(cancellationToken);

            reorderRulesByProduct = rules
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault(x => x.MinStockQuantity.HasValue)?.MinStockQuantity);
        }

        var collectionRows = await _dbContext.ProductCollections
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.CollectionId })
            .ToListAsync(cancellationToken);

        if (eligiblePriceLists.Count == 0 && eligiblePolicies.Count == 0)
        {
            var activePls = await _dbContext.PriceLists
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" &&
                            (!x.ValidFrom.HasValue || x.ValidFrom <= nowTime) &&
                            (!x.ValidUntil.HasValue || x.ValidUntil >= nowTime))
                .ToListAsync(cancellationToken);

            foreach (var pl in activePls)
            {
                var hasOutletLimits = await _dbContext.PriceListOutlets.AnyAsync(po => po.PriceListId == pl.Id && po.Status == "ACTIVE", cancellationToken);
                if (hasOutletLimits)
                {
                    var matchesOutlet = await _dbContext.PriceListOutlets.AnyAsync(po => po.PriceListId == pl.Id && po.OutletId == deviceOutletId.Value && po.Status == "ACTIVE", cancellationToken);
                    if (!matchesOutlet) continue;
                }

                var hasChannelLimits = await _dbContext.PriceListChannels.AnyAsync(pc => pc.PriceListId == pl.Id && pc.Status == "ACTIVE", cancellationToken);
                if (hasChannelLimits)
                {
                    var matchesChannel = await _dbContext.PriceListChannels.AnyAsync(pc => pc.PriceListId == pl.Id && pc.SalesChannelId == salesChannelId && pc.Status == "ACTIVE", cancellationToken);
                    if (!matchesChannel) continue;
                }

                eligiblePriceLists.Add(pl);
            }

            var activePols = await _dbContext.DiscountPolicies
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.DiscountScope == "LINE" &&
                            (!x.StartsAt.HasValue || x.StartsAt <= nowTime) &&
                            (!x.EndsAt.HasValue || x.EndsAt >= nowTime))
                .ToListAsync(cancellationToken);

            foreach (var dp in activePols)
            {
                var hasOutletLimits = await _dbContext.DiscountPolicyOutlets.AnyAsync(po => po.DiscountPolicyId == dp.Id && po.Status == "ACTIVE", cancellationToken);
                if (hasOutletLimits)
                {
                    var matchesOutlet = await _dbContext.DiscountPolicyOutlets.AnyAsync(po => po.DiscountPolicyId == dp.Id && po.OutletId == deviceOutletId.Value && po.Status == "ACTIVE", cancellationToken);
                    if (!matchesOutlet) continue;
                }

                var hasChannelLimits = await _dbContext.DiscountPolicyChannels.AnyAsync(pc => pc.DiscountPolicyId == dp.Id && pc.Status == "ACTIVE", cancellationToken);
                if (hasChannelLimits)
                {
                    var matchesChannel = await _dbContext.DiscountPolicyChannels.AnyAsync(pc => pc.DiscountPolicyId == dp.Id && pc.SalesChannelId == salesChannelId && pc.Status == "ACTIVE", cancellationToken);
                    if (!matchesChannel) continue;
                }

                eligiblePolicies.Add(dp);
            }
        }

        var eligiblePlIds = eligiblePriceLists.Select(x => x.Id).ToList();
        priceListItems = await _dbContext.PriceListItems
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        eligiblePlIds.Contains(x.PriceListId) &&
                        x.ProductVariantId.HasValue &&
                        variantIds.Contains(x.ProductVariantId.Value) &&
                        x.Status == "ACTIVE" &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= nowTime) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= nowTime))
            .ToListAsync(cancellationToken);

        var eligiblePolIds = eligiblePolicies.Select(x => x.Id).ToList();
        policyTargets = await _dbContext.DiscountPolicyTargets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && eligiblePolIds.Contains(x.DiscountPolicyId) && x.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        policyConditions = await _dbContext.DiscountPolicyConditions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && eligiblePolIds.Contains(x.DiscountPolicyId) && x.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        var discountTypeIds = eligiblePolicies.Select(x => x.DiscountTypeId).Distinct().ToList();
        discountTypes = await _dbContext.DiscountTypes
            .AsNoTracking()
            .Where(x => x.Status == "ACTIVE" && discountTypeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.CalculationMethod, cancellationToken);

        var summaries = new List<PosProductSummaryResponseDto>(products.Count);
        foreach (var product in products)
        {
            if (hiddenProductIds.Contains(product.Id))
            {
                continue;
            }

            variantsByProduct.TryGetValue(product.Id, out var productVariants);
            productVariants ??= [];

            var hasVariants =
                !string.Equals(product.ProductStructure, "SIMPLE", StringComparison.OrdinalIgnoreCase) ||
                productVariants.Count > 1;

            var defaultVariant =
                productVariants.FirstOrDefault(x => x.IsDefaultVariant) ??
                productVariants.FirstOrDefault();

            decimal? minPrice = null;
            foreach (var variant in productVariants)
            {
                if (!pricesByVariant.TryGetValue(variant.Id, out var price))
                {
                    continue;
                }

                minPrice = minPrice.HasValue ? Math.Min(minPrice.Value, price) : price;
            }

            categoryByProduct.TryGetValue(product.Id, out var categoryInfo);
            imageByProduct.TryGetValue(product.Id, out var imageStorageKey);

            var availableQuantities = productVariants
                .Where(variant => inventoryByVariant.ContainsKey(variant.Id))
                .Select(variant => inventoryByVariant[variant.Id])
                .ToList();
            decimal? availableQuantity = availableQuantities.Count == 0
                ? null
                : availableQuantities.Sum(quantity => Math.Max(0m, quantity));

            reorderRulesByProduct.TryGetValue(product.Id, out var minStockQuantity);
            var stockStatus = ResolveProductStockStatus(
                availableQuantities,
                availableQuantity,
                minStockQuantity);

            var productCategoryIds = categoryRows.Where(x => x.ProductId == product.Id).Select(x => x.CategoryId).ToList();
            var productCollectionIds = collectionRows.Where(x => x.ProductId == product.Id).Select(x => x.CollectionId).ToList();

            var searchMatchedVariants = productVariants
                .Where(variant => matchedVariantIds.Contains(variant.Id))
                .ToList();

            if (searchMatchedVariants.Count > 0)
            {
                foreach (var matchedVariant in searchMatchedVariants)
                {
                    var matchedAvailableQuantity = inventoryByVariant.GetValueOrDefault(matchedVariant.Id);
                    var hasInventory = inventoryByVariant.ContainsKey(matchedVariant.Id);
                    var matchedStockStatus = ResolveStockStatus(
                        hasInventory ? matchedAvailableQuantity : null,
                        minStockQuantity);

                    var variantBasePrice = (int)Math.Round(
                        pricesByVariant.GetValueOrDefault(matchedVariant.Id),
                        MidpointRounding.AwayFromZero);

                    var offer = ResolveOfferForVariant(
                        product.Id,
                        matchedVariant.Id,
                        variantBasePrice,
                        productCategoryIds,
                        product.BrandId,
                        productCollectionIds,
                        priceListItems,
                        eligiblePriceLists,
                        eligiblePolicies,
                        policyTargets,
                        policyConditions,
                        discountTypes);

                    summaries.Add(new PosProductSummaryResponseDto(
                        product.Id,
                        matchedVariant.Id,
                        product.ProductName,
                        product.ShortDescription,
                        imageStorageKey,
                        categoryInfo?.CategoryId,
                        string.IsNullOrWhiteSpace(categoryInfo?.CategoryName) ? "General" : categoryInfo!.CategoryName,
                        variantBasePrice,
                        hasVariants,
                        matchedStockStatus,
                        hasInventory ? matchedAvailableQuantity : null,
                        matchedVariant.Sku,
                        barcodeByVariant.GetValueOrDefault(matchedVariant.Id),
                        matchedVariant.VariantName,
                        HasOffer: offer != null,
                        OfferType: offer?.OfferType,
                        OfferPolicyId: offer?.OfferPolicyId,
                        OfferName: offer?.OfferName,
                        OriginalPrice: offer?.OriginalPrice,
                        SellingPrice: offer?.SellingPrice,
                        OfferPrice: offer?.OfferPrice,
                        DiscountLabel: offer?.DiscountLabel,
                        RequiresCartValidation: offer?.RequiresCartValidation ?? false,
                        RequiresManagerApproval: offer?.RequiresManagerApproval ?? false));
                }

                continue;
            }

            OfferCandidate? bestOffer = null;
            if (hasVariants)
            {
                var variantOffers = new List<OfferCandidate>();
                foreach (var variant in productVariants)
                {
                    if (pricesByVariant.TryGetValue(variant.Id, out var priceDec))
                    {
                        var vBasePrice = (int)Math.Round(priceDec, MidpointRounding.AwayFromZero);
                        var o = ResolveOfferForVariant(
                            product.Id,
                            variant.Id,
                            vBasePrice,
                            productCategoryIds,
                            product.BrandId,
                            productCollectionIds,
                            priceListItems,
                            eligiblePriceLists,
                            eligiblePolicies,
                            policyTargets,
                            policyConditions,
                            discountTypes);
                        if (o != null) variantOffers.Add(o);
                    }
                }
                bestOffer = ResolveBestOffer(variantOffers);
            }
            else if (defaultVariant != null && pricesByVariant.TryGetValue(defaultVariant.Id, out var priceDec))
            {
                var vBasePrice = (int)Math.Round(priceDec, MidpointRounding.AwayFromZero);
                bestOffer = ResolveOfferForVariant(
                    product.Id,
                    defaultVariant.Id,
                    vBasePrice,
                    productCategoryIds,
                    product.BrandId,
                    productCollectionIds,
                    priceListItems,
                    eligiblePriceLists,
                    eligiblePolicies,
                    policyTargets,
                    policyConditions,
                    discountTypes);
            }

            var productBasePrice = (int)Math.Round(minPrice ?? 0m, MidpointRounding.AwayFromZero);

            summaries.Add(new PosProductSummaryResponseDto(
                product.Id,
                hasVariants ? null : defaultVariant?.Id,
                product.ProductName,
                product.ShortDescription,
                imageStorageKey,
                categoryInfo?.CategoryId,
                string.IsNullOrWhiteSpace(categoryInfo?.CategoryName) ? "General" : categoryInfo!.CategoryName,
                productBasePrice,
                hasVariants,
                stockStatus,
                availableQuantity,
                defaultVariant?.Sku,
                defaultVariant is null
                    ? null
                    : barcodeByVariant.GetValueOrDefault(defaultVariant.Id),
                VariantName: null,
                HasOffer: bestOffer != null,
                OfferType: bestOffer?.OfferType,
                OfferPolicyId: bestOffer?.OfferPolicyId,
                OfferName: bestOffer?.OfferName,
                OriginalPrice: bestOffer?.OriginalPrice,
                SellingPrice: bestOffer?.SellingPrice,
                OfferPrice: bestOffer?.OfferPrice,
                DiscountLabel: bestOffer?.DiscountLabel,
                RequiresCartValidation: bestOffer?.RequiresCartValidation ?? false,
                RequiresManagerApproval: bestOffer?.RequiresManagerApproval ?? false));
        }

        if (isOffers)
        {
            summaries = summaries
                .Where(x => x.HasOffer)
                .OrderBy(x => x.OfferPrice.HasValue ? 0 : 1)
                .ThenByDescending(x => x.OfferPrice.HasValue ? (x.OriginalPrice - x.OfferPrice.Value) : 0)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        return new PosProductCatalogRepositoryResult(null, summaries);
    }

    public async Task<PosBarcodeProductRepositoryResult> GetProductByBarcodeAsync(
        Guid tenantId,
        Guid deviceId,
        string barcode,
        CancellationToken cancellationToken)
    {
        var device = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Id == deviceId &&
                x.Status == PosDeviceConstants.ActiveStatus &&
                x.IsTrusted)
            .Select(x => new { x.OutletId })
            .SingleOrDefaultAsync(cancellationToken);

        if (device is null)
        {
            return new PosBarcodeProductRepositoryResult("pos_device.invalid", null);
        }

        var outletIsActive = await _dbContext.Outlets
            .AsNoTracking()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Id == device.OutletId &&
                x.Status == OutletConstants.ActiveStatus,
                cancellationToken);
        if (!outletIsActive)
        {
            return new PosBarcodeProductRepositoryResult("pos_device.invalid", null);
        }

        var matchedBarcode = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Barcode == barcode &&
                x.Status == ProductConstants.ActiveStatus)
            .Select(x => new
            {
                x.ProductId,
                x.ProductVariantId,
                x.Barcode,
                x.BarcodeType,
                x.QuantityPerScan,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (matchedBarcode is null)
        {
            return new PosBarcodeProductRepositoryResult("pos_barcode.not_found", null);
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == matchedBarcode.ProductId,
                cancellationToken);
        if (product is null || product.Status != ProductConstants.ActiveStatus || !product.IsSellable)
        {
            return new PosBarcodeProductRepositoryResult("pos_product.unavailable", null);
        }

        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            [product.Id],
            cancellationToken);
        if (hiddenProductIds.Contains(product.Id))
        {
            return new PosBarcodeProductRepositoryResult("pos_product.unavailable", null);
        }

        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == product.Id &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .ToListAsync(cancellationToken);

        var resolvedVariants = matchedBarcode.ProductVariantId.HasValue
            ? variants.Where(x => x.Id == matchedBarcode.ProductVariantId.Value).ToList()
            : variants;
        if (resolvedVariants.Count == 0)
        {
            return new PosBarcodeProductRepositoryResult("pos_variant.unavailable", null);
        }
        if (resolvedVariants.Count > 1)
        {
            return new PosBarcodeProductRepositoryResult("pos_barcode.ambiguous", null);
        }

        var variant = resolvedVariants[0];
        var defaultPriceListId = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!defaultPriceListId.HasValue)
        {
            return new PosBarcodeProductRepositoryResult("pos_price.unavailable", null);
        }

        var price = await _dbContext.PriceListItems
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.PriceListId == defaultPriceListId.Value &&
                x.ProductVariantId == variant.Id &&
                x.Status == "ACTIVE")
            .Select(x => (decimal?)x.SellingPrice)
            .SingleOrDefaultAsync(cancellationToken);
        if (!price.HasValue)
        {
            return new PosBarcodeProductRepositoryResult("pos_price.unavailable", null);
        }

        var availableQuantity = await (
                from balance in _dbContext.InventoryBalances.AsNoTracking()
                join location in _dbContext.InventoryLocations.AsNoTracking()
                    on balance.InventoryLocationId equals location.Id
                where balance.TenantId == tenantId &&
                      location.TenantId == tenantId &&
                      location.OutletId == device.OutletId &&
                      location.IsSellableLocation &&
                      location.Status == "ACTIVE" &&
                      balance.ProductVariantId == variant.Id
                select (decimal?)balance.AvailableQuantity)
            .SumAsync(cancellationToken);

        var minStockQuantity = await _dbContext.InventoryReorderRules
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == product.Id && x.Status == "ACTIVE")
            .Select(x => x.MinStockQuantity)
            .FirstOrDefaultAsync(cancellationToken);

        var imageUrl = await (from image in _dbContext.ProductImages.AsNoTracking()
                               join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                                   on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                   equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                               from mediaAsset in mediaAssets.DefaultIfEmpty()
                               where image.TenantId == tenantId &&
                                     image.ProductId == product.Id &&
                                     image.Status == "ACTIVE"
                               orderby image.IsPrimaryImage ? 0 : 1, image.SortOrder
                               select mediaAsset == null ? null : mediaAsset.PublicUrl)
            .FirstOrDefaultAsync(cancellationToken);

        return new PosBarcodeProductRepositoryResult(
            null,
            new PosBarcodeProductResponseDto(
                product.Id,
                variant.Id,
                matchedBarcode.Barcode,
                matchedBarcode.BarcodeType,
                product.ProductName,
                variant.VariantName,
                variant.Sku,
                matchedBarcode.QuantityPerScan,
                (int)Math.Round(price.Value, MidpointRounding.AwayFromZero),
                availableQuantity,
                ResolveStockStatus(availableQuantity, minStockQuantity),
                imageUrl));
    }

    public async Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var deviceOutletId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == deviceId)
            .Select(x => (Guid?)x.OutletId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!deviceOutletId.HasValue)
        {
            return new PosProductCatalogCategoriesRepositoryResult("pos_products.device_not_found", []);
        }

        var categories = await (
                from category in _dbContext.Categories.AsNoTracking()
                where category.TenantId == tenantId &&
                      category.Status == CategoryConstants.ActiveStatus &&
                      _dbContext.ProductCategories.Any(link =>
                          link.TenantId == tenantId &&
                          link.CategoryId == category.Id &&
                          _dbContext.Products.Any(product =>
                              product.TenantId == tenantId &&
                              product.Id == link.ProductId &&
                              product.Status == ProductConstants.ActiveStatus &&
                              product.IsSellable))
                orderby category.SortOrder, category.CategoryName
                select new PosCatalogCategoryResponseDto(category.Id, category.CategoryName))
            .ToListAsync(cancellationToken);

        return new PosProductCatalogCategoriesRepositoryResult(null, categories);
    }

    public async Task<PosProductDetailRepositoryResult> GetProductDetailAsync(
        Guid tenantId,
        Guid deviceId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var deviceOutletId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == deviceId)
            .Select(x => (Guid?)x.OutletId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!deviceOutletId.HasValue)
        {
            return new PosProductDetailRepositoryResult("pos_products.device_not_found", null);
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == productId &&
                    x.Status == ProductConstants.ActiveStatus &&
                    x.IsSellable,
                cancellationToken);

        if (product is null)
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            [productId],
            cancellationToken);

        if (hiddenProductIds.Contains(productId))
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .OrderByDescending(x => x.IsDefaultVariant)
            .ThenBy(x => x.VariantName)
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        var hasVariants =
            !string.Equals(product.ProductStructure, "SIMPLE", StringComparison.OrdinalIgnoreCase) ||
            variants.Count > 1;

        var defaultPriceListId = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var variantIds = variants.Select(x => x.Id).ToList();
        var pricesByVariant = new Dictionary<Guid, decimal>();
        if (defaultPriceListId.HasValue)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == defaultPriceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
                .ToListAsync(cancellationToken);

            pricesByVariant = priceRows.ToDictionary(x => x.VariantId, x => x.SellingPrice);
        }

        var inventoryByVariant = new Dictionary<Guid, decimal>();
        var inventoryRows = await (
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking()
                on balance.InventoryLocationId equals location.Id
            where balance.TenantId == tenantId &&
                  location.TenantId == tenantId &&
                  location.OutletId == deviceOutletId.Value &&
                  location.IsSellableLocation &&
                  location.Status == "ACTIVE" &&
                  balance.ProductVariantId.HasValue &&
                  variantIds.Contains(balance.ProductVariantId.Value)
            group balance by balance.ProductVariantId!.Value
            into groupRows
            select new
            {
                VariantId = groupRows.Key,
                AvailableQuantity = groupRows.Sum(x => x.AvailableQuantity),
            })
            .ToListAsync(cancellationToken);

        inventoryByVariant = inventoryRows.ToDictionary(x => x.VariantId, x => x.AvailableQuantity);

        var categoryName = await (
                from link in _dbContext.ProductCategories.AsNoTracking()
                join category in _dbContext.Categories.AsNoTracking()
                    on link.CategoryId equals category.Id
                where link.TenantId == tenantId && link.ProductId == productId
                orderby link.IsPrimaryCategory descending, link.SortOrder
                select category.CategoryName)
            .FirstOrDefaultAsync(cancellationToken) ?? "General";

        var imageStorageRow = await (from image in _dbContext.ProductImages.AsNoTracking()
                                     join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                                         on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                         equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                                     from mediaAsset in mediaAssets.DefaultIfEmpty()
                                     where image.TenantId == tenantId &&
                                           image.ProductId == productId &&
                                           image.Status == ActiveImageStatus
                                     orderby image.IsPrimaryImage ? 0 : 1, image.SortOrder
                                     select new
                                     {
                                         MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                                     })
            .FirstOrDefaultAsync(cancellationToken);

        var imageStorageKey = imageStorageRow is null
            ? null
            : imageStorageRow.MediaPublicUrl;
        var posSalesChannelId = await (from channel in _dbContext.SalesChannels.AsNoTracking()
                                       join platform in _dbContext.PlatformSalesChannels.AsNoTracking()
                                           on channel.PlatformSalesChannelId equals platform.Id
                                       where channel.TenantId == tenantId && channel.Status == "ACTIVE" &&
                                             platform.ChannelCode == "POS"
                                       select (Guid?)channel.Id).FirstOrDefaultAsync(cancellationToken);
        var imageRows = await (from image in _dbContext.ProductImages.AsNoTracking()
                               join media in _dbContext.Set<MediaAsset>().AsNoTracking()
                                   on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                   equals new { media.TenantId, MediaAssetId = (Guid?)media.Id } into mediaRows
                               from media in mediaRows.DefaultIfEmpty()
                               where image.TenantId == tenantId && image.ProductId == productId &&
                                     image.Status == ActiveImageStatus && image.IsPrimaryImage
                               select new { image.ProductVariantId, image.SalesChannelId,
                                   Url = media == null ? null : media.PublicUrl, image.SortOrder })
            .ToListAsync(cancellationToken);
        var productOptions = await _dbContext.ProductOptions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == "ACTIVE")
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var reorderRule = await _dbContext.InventoryReorderRules
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == "ACTIVE")
            .Select(x => x.MinStockQuantity)
            .FirstOrDefaultAsync(cancellationToken);

        var optionIds = productOptions.Select(x => x.Id).ToList();
        var optionValues = optionIds.Count == 0
            ? []
            : await _dbContext.ProductOptionValues
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    optionIds.Contains(x.ProductOptionId) &&
                    x.Status == "ACTIVE")
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

        var variantOptionLinks = await _dbContext.ProductVariantOptionValues
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);

        var optionNameById = productOptions.ToDictionary(x => x.Id, x => x.OptionName);
        var optionValueNameById = optionValues.ToDictionary(x => x.Id, x => x.ValueName);
        var salesUomIds = variants.Select(x => x.SalesUomId).Distinct().ToList();
        var uomCodes = await _dbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => (x.TenantId == null || x.TenantId == tenantId) && salesUomIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UomCode, cancellationToken);
        var currency = defaultPriceListId.HasValue
            ? await _dbContext.PriceLists.AsNoTracking().Where(x => x.Id == defaultPriceListId.Value)
                .Select(x => x.CurrencyCode).FirstAsync(cancellationToken)
            : string.Empty;

        var variantGroups = productOptions
            .Select(option => new PosProductVariantGroupResponseDto(
                option.OptionName,
                optionValues
                    .Where(value => value.ProductOptionId == option.Id)
                    .Select(value => value.ValueName)
                    .ToList(),
                option.Id,
                option.OptionCode,
                option.InputType,
                option.IsRequired,
                option.SortOrder,
                optionValues.Where(value => value.ProductOptionId == option.Id)
                    .Select(value => new PosProductOptionValueResponseDto(
                        value.Id, value.ValueCode, value.DisplayName ?? value.ValueName,
                        value.ColorHex, value.SortOrder)).ToList()))
            .Where(group => group.Options.Count > 0)
            .ToList();

        var pricedVariants = variants
            .Where(variant => pricesByVariant.ContainsKey(variant.Id))
            .ToList();

        if (pricedVariants.Count == 0)
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        decimal? minPrice = null;
        var variantDetails = new List<PosProductVariantDetailResponseDto>(pricedVariants.Count);
        foreach (var variant in pricedVariants)
        {
            var price = pricesByVariant[variant.Id];
            minPrice = minPrice.HasValue ? Math.Min(minPrice.Value, price) : price;

            decimal? availableQuantity = inventoryByVariant.TryGetValue(variant.Id, out var qty) ? qty : null;
            var stockStatus = ResolveStockStatus(availableQuantity, reorderRule);
            var variantImageUrl = imageRows
                .Where(x => x.ProductVariantId == variant.Id && x.SalesChannelId == posSalesChannelId)
                .OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault()
                ?? imageRows.Where(x => x.ProductVariantId == variant.Id && x.SalesChannelId == null)
                    .OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault()
                ?? imageRows.Where(x => x.ProductVariantId == null && x.SalesChannelId == posSalesChannelId)
                    .OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault()
                ?? imageRows.Where(x => x.ProductVariantId == null && x.SalesChannelId == null)
                    .OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault();

            var attributes = variantOptionLinks
                .Where(link => link.ProductVariantId == variant.Id)
                .Select(link =>
                {
                    optionNameById.TryGetValue(link.ProductOptionId, out var optionName);
                    optionValueNameById.TryGetValue(link.ProductOptionValueId, out var valueName);
                    return new KeyValuePair<string, string>(
                        optionName ?? string.Empty,
                        valueName ?? string.Empty);
                })
                .Where(pair => pair.Key.Length > 0 && pair.Value.Length > 0)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            variantDetails.Add(new PosProductVariantDetailResponseDto(
                variant.Id,
                variant.Sku ?? string.Empty,
                (int)Math.Round(price, MidpointRounding.AwayFromZero),
                availableQuantity,
                stockStatus,
                attributes,
                variant.VariantCode,
                variant.VariantName,
                variantOptionLinks.Where(x => x.ProductVariantId == variant.Id)
                    .Select(x => x.ProductOptionValueId).ToList(),
                variant.IsDefaultVariant,
                !availableQuantity.HasValue || availableQuantity > 0,
                availableQuantity is <= 0 ? "out_of_stock" : null,
                variant.SalesUomId,
                uomCodes.GetValueOrDefault(variant.SalesUomId),
                variant.AllowFractionalQuantity,
                price,
                currency,
                availableQuantity.HasValue,
                variantImageUrl));
        }

        var productAvailableQuantities = variantDetails
            .Where(x => x.StockQty.HasValue)
            .Select(x => x.StockQty!.Value)
            .ToList();
        var productAvailableQuantity = productAvailableQuantities.Count > 0
            ? productAvailableQuantities.Sum(quantity => Math.Max(0m, quantity))
            : (decimal?)null;
        var detail = new PosProductDetailResponseDto(
            product.Id,
            product.ProductName,
            product.ShortDescription,
            imageStorageKey,
            categoryName,
            (int)Math.Round(minPrice ?? 0m, MidpointRounding.AwayFromZero),
            hasVariants,
            variantGroups,
            variantDetails,
            ResolveProductStockStatus(
                productAvailableQuantities,
                productAvailableQuantity,
                reorderRule),
            productAvailableQuantity)
        {
            ProductCode = product.ProductCode,
            Currency = currency,
            RequiresConfiguration = productOptions.Any(x => x.IsRequired) && variantDetails.Count > 1
        };

        return new PosProductDetailRepositoryResult(null, detail);
    }

    public async Task<PosProductRecommendationsRepositoryResult> GetRecommendationsAsync(
        Guid tenantId, Guid deviceId, Guid productId, Guid? sourceVariantId,
        string recommendationType, int limit, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var context = await (from device in _dbContext.PosDevices.AsNoTracking()
                             join outlet in _dbContext.Outlets.AsNoTracking()
                                 on new { device.TenantId, Id = device.OutletId }
                                 equals new { outlet.TenantId, outlet.Id }
                             where device.TenantId == tenantId && device.Id == deviceId &&
                                   device.Status == "ACTIVE" && device.IsTrusted && outlet.Status == "ACTIVE"
                             select new { outlet.Id }).FirstOrDefaultAsync(cancellationToken);
        if (context is null)
            return new("pos_products.device_not_found", []);

        var posChannelId = await (from channel in _dbContext.SalesChannels.AsNoTracking()
                                  join platformChannel in _dbContext.PlatformSalesChannels.AsNoTracking()
                                      on channel.PlatformSalesChannelId equals platformChannel.Id
                                  where channel.TenantId == tenantId && channel.Status == "ACTIVE" &&
                                        platformChannel.ChannelCode == "POS"
                                  select (Guid?)channel.Id).FirstOrDefaultAsync(cancellationToken);

        var links = await _dbContext.ProductRecommendationLinks.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SourceProductId == productId &&
                        (x.SourceVariantId == null || x.SourceVariantId == sourceVariantId) &&
                        x.RecommendationType == recommendationType && x.Status == "ACTIVE" &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now) &&
                        (!x.OutletId.HasValue || x.OutletId == context.Id) &&
                        (!x.SalesChannelId.HasValue || x.SalesChannelId == posChannelId))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Take(Math.Clamp(limit, 1, 3))
            .Select(x => new { x.Id, x.RecommendedProductId, x.RecommendedVariantId })
            .ToListAsync(cancellationToken);
        if (links.Count == 0) return new(null, []);

        var productIds = links.Select(x => x.RecommendedProductId).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id) &&
                        x.Status == ProductConstants.ActiveStatus && x.IsSellable)
            .Select(x => new { x.Id, x.ProductName, x.ProductStructure })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var hidden = await ResolveHiddenProductIdsAsync(tenantId, productIds, cancellationToken);

        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId) &&
                        x.Status == ProductConstants.ActiveStatus && x.IsSellable)
            .Select(x => new { x.Id, x.ProductId, x.VariantName, x.IsDefaultVariant })
            .ToListAsync(cancellationToken);
        var variantIds = variants.Select(x => x.Id).ToList();
        var priceListId = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.IsDefaultPriceList &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) && (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        var prices = priceListId is null ? new Dictionary<Guid, decimal>() :
            await _dbContext.PriceListItems.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.PriceListId == priceListId &&
                            x.ProductVariantId.HasValue && variantIds.Contains(x.ProductVariantId.Value) &&
                            x.Status == "ACTIVE" && (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                            (!x.ValidUntil.HasValue || x.ValidUntil >= now))
                .GroupBy(x => x.ProductVariantId!.Value)
                .ToDictionaryAsync(x => x.Key, x => x.OrderBy(y => y.MinQuantity).First().SellingPrice, cancellationToken);
        var stock = await (from balance in _dbContext.InventoryBalances.AsNoTracking()
                           join location in _dbContext.InventoryLocations.AsNoTracking()
                               on new { balance.TenantId, Id = balance.InventoryLocationId }
                               equals new { location.TenantId, location.Id }
                           where balance.TenantId == tenantId && location.OutletId == context.Id &&
                                 location.Status == "ACTIVE" && location.IsSellableLocation &&
                                 balance.ProductVariantId.HasValue && variantIds.Contains(balance.ProductVariantId.Value)
                           group balance by balance.ProductVariantId!.Value into g
                           select new { Id = g.Key, Qty = g.Sum(x => x.AvailableQuantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty, cancellationToken);

        var currency = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.Id == priceListId).Select(x => x.CurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);
        var result = new List<PosProductRecommendationResponseDto>();
        foreach (var link in links)
        {
            if (!products.TryGetValue(link.RecommendedProductId, out var product) || hidden.Contains(product.Id)) continue;
            var candidates = variants.Where(x => x.ProductId == product.Id).ToList();
            var resolved = link.RecommendedVariantId.HasValue
                ? candidates.SingleOrDefault(x => x.Id == link.RecommendedVariantId.Value)
                : candidates.Count == 1 ? candidates[0] : null;
            var requiresConfiguration = resolved is null && candidates.Count > 1;
            var price = resolved is not null && prices.TryGetValue(resolved.Id, out var amount) ? amount : (decimal?)null;
            var available = resolved is not null && stock.TryGetValue(resolved.Id, out var qty) ? qty : (decimal?)null;
            var selectable = resolved is not null && price.HasValue && (!available.HasValue || available > 0);
            result.Add(new(link.Id, product.Id, resolved?.Id, product.ProductName, resolved?.VariantName,
                null, candidates.Count > 1, requiresConfiguration, price, currency, available,
                available is <= 0 ? "out_of_stock" : available.HasValue ? "in_stock" : "unknown",
                selectable, requiresConfiguration ? "variant_configuration_required" :
                    !price.HasValue ? "price_unavailable" : available is <= 0 ? "out_of_stock" : null));
        }
        return new(null, result);
    }

    private async Task<PosCatalogSearchFilter> ApplySearchFilterAsync(
        IQueryable<Domain.Modules.Tenant.CatalogProduct.Entities.Product> productsQuery,
        Guid tenantId,
        string? search,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new PosCatalogSearchFilter(productsQuery, []);
        }

        var term = search.Trim();
        var normalizedTerm = term.ToUpperInvariant();

        var directMatches = await (
                from variant in _dbContext.ProductVariants.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking()
                    on variant.ProductId equals product.Id
                where variant.TenantId == tenantId &&
                      product.TenantId == tenantId &&
                      variant.Status == ProductConstants.ActiveStatus &&
                      variant.IsSellable
                let skuMatch = variant.Sku != null && variant.Sku.ToUpper().Contains(normalizedTerm)
                let barcodeMatch = _dbContext.ProductBarcodes.Any(barcode =>
                    barcode.TenantId == tenantId &&
                    barcode.ProductVariantId == variant.Id &&
                    barcode.Status == ProductConstants.ActiveStatus &&
                    barcode.Barcode.ToUpper().Contains(normalizedTerm))
                let productVariantNameMatch =
                    (product.ProductName + " " + variant.VariantName).ToUpper() == normalizedTerm
                where skuMatch || barcodeMatch || productVariantNameMatch
                select new { variant.ProductId, VariantId = variant.Id })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (directMatches.Count > 0)
        {
            var directMatchProductIds = directMatches.Select(x => x.ProductId).Distinct().ToList();
            return new PosCatalogSearchFilter(
                productsQuery.Where(product => directMatchProductIds.Contains(product.Id)),
                directMatches.Select(x => x.VariantId).ToHashSet());
        }

        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var pattern = $"%{term}%";
            return new PosCatalogSearchFilter(
                productsQuery.Where(product => EF.Functions.ILike(product.ProductName, pattern)),
                []);
        }

        return new PosCatalogSearchFilter(
            productsQuery.Where(product => product.ProductName.ToUpper().Contains(normalizedTerm)),
            []);
    }

    private sealed record PosCatalogSearchFilter(
        IQueryable<Domain.Modules.Tenant.CatalogProduct.Entities.Product> Products,
        HashSet<Guid> MatchedVariantIds);

    private async Task<HashSet<Guid>> ResolveHiddenProductIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var posChannelId = await (from s in _dbContext.SalesChannels.AsNoTracking()
                                  join p in _dbContext.PlatformSalesChannels.AsNoTracking() on s.PlatformSalesChannelId equals p.Id
                                  where s.TenantId == tenantId &&
                                        s.Status == "ACTIVE" &&
                                        (p.ChannelCode.ToUpper() == "POS" || p.ChannelType.ToUpper() == "PHYSICAL")
                                  orderby s.SortOrder
                                  select (Guid?)s.Id)
                                 .FirstOrDefaultAsync(cancellationToken);

        if (!posChannelId.HasValue)
        {
            return [];
        }

        var visibilityRows = await _dbContext.ProductChannelVisibilities
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        if (visibilityRows.Count == 0)
        {
            return [];
        }

        var hiddenProductIds = new HashSet<Guid>();
        foreach (var productId in visibilityRows.Select(x => x.ProductId).Distinct())
        {
            var productVisibility = visibilityRows
                .Where(x => x.ProductId == productId && x.SalesChannelId == posChannelId.Value)
                .ToList();

            if (productVisibility.Count == 0)
            {
                continue;
            }

            if (!productVisibility.Any(x => x.IsVisible && x.Status == ActiveImageStatus))
            {
                hiddenProductIds.Add(productId);
            }
        }

        return hiddenProductIds;
    }

    private static string ResolveStockStatus(decimal? availableQuantity, decimal? minStockQuantity)
    {
        if (!availableQuantity.HasValue)
        {
            return "in_stock";
        }

        if (availableQuantity.Value <= 0m)
        {
            return "out_of_stock";
        }

        var threshold = minStockQuantity ?? 5m;

        if (availableQuantity.Value <= threshold)
        {
            return "low_stock";
        }

        return "in_stock";
    }

    private static string ResolveProductStockStatus(
        IReadOnlyCollection<decimal> variantAvailableQuantities,
        decimal? totalAvailableQuantity,
        decimal? minStockQuantity)
    {
        if (variantAvailableQuantities.Count == 0)
        {
            return ResolveStockStatus(totalAvailableQuantity, minStockQuantity);
        }

        if (!variantAvailableQuantities.Any(quantity => quantity > 0m))
        {
            return "out_of_stock";
        }

        return ResolveStockStatus(totalAvailableQuantity, minStockQuantity);
    }

    private sealed class OfferCandidate
    {
        public string OfferType { get; set; } = string.Empty;
        public Guid OfferPolicyId { get; set; }
        public string OfferName { get; set; } = string.Empty;
        public int OriginalPrice { get; set; }
        public int SellingPrice { get; set; }
        public int? OfferPrice { get; set; }
        public string DiscountLabel { get; set; } = string.Empty;
        public bool RequiresCartValidation { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public int Priority { get; set; }
        public DateTimeOffset? EndsAt { get; set; }
    }

    private static bool MatchesTarget(
        DiscountPolicyTarget target,
        Guid productId,
        Guid variantId,
        List<Guid> categoryIds,
        Guid? brandId,
        List<Guid> collectionIds)
    {
        return target.TargetType switch
        {
            "PRODUCT" => target.ProductId == productId,
            "PRODUCT_VARIANT" => target.ProductVariantId == variantId,
            "CATEGORY" => target.CategoryId.HasValue && categoryIds.Contains(target.CategoryId.Value),
            "BRAND" => target.BrandId.HasValue && brandId == target.BrandId.Value,
            "COLLECTION" => target.CollectionId.HasValue && collectionIds.Contains(target.CollectionId.Value),
            _ => false
        };
    }

    private static OfferCandidate? ResolveBestOffer(List<OfferCandidate> candidates)
    {
        if (candidates.Count == 0) return null;

        var immediate = candidates.Where(c => !c.RequiresCartValidation && c.OfferPrice.HasValue).ToList();
        if (immediate.Count > 0)
        {
            return immediate
                .OrderBy(c => c.OfferPrice!.Value)
                .ThenByDescending(c => c.Priority)
                .ThenBy(c => c.EndsAt.HasValue ? c.EndsAt.Value.Ticks : long.MaxValue)
                .ThenBy(c => c.OfferPolicyId.ToString())
                .First();
        }

        return candidates
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.EndsAt.HasValue ? c.EndsAt.Value.Ticks : long.MaxValue)
            .ThenBy(c => c.OfferPolicyId.ToString())
            .First();
    }

    private static OfferCandidate? ResolveOfferForVariant(
        Guid productId,
        Guid variantId,
        int basePrice,
        List<Guid> categoryIds,
        Guid? brandId,
        List<Guid> collectionIds,
        List<PriceListItem> priceListItems,
        List<PriceList> eligiblePriceLists,
        List<DiscountPolicy> eligiblePolicies,
        List<DiscountPolicyTarget> policyTargets,
        List<DiscountPolicyCondition> policyConditions,
        Dictionary<Guid, string> discountTypes)
    {
        var candidates = new List<OfferCandidate>();

        // 1. Special Price
        var variantSpecialPrices = priceListItems.Where(x => x.ProductVariantId == variantId).ToList();
        foreach (var item in variantSpecialPrices)
        {
            if (item.CompareAtPrice.HasValue && item.CompareAtPrice.Value > item.SellingPrice)
            {
                var pl = eligiblePriceLists.First(x => x.Id == item.PriceListId);
                var originalPrice = (int)Math.Round(item.CompareAtPrice.Value, MidpointRounding.AwayFromZero);
                var sellingPrice = (int)Math.Round(item.SellingPrice, MidpointRounding.AwayFromZero);

                var isConditional = item.MinQuantity > 1;

                var pct = (originalPrice - sellingPrice) * 100.0 / originalPrice;
                var label = isConditional ? "Offer available" : $"{(int)Math.Round(pct, MidpointRounding.AwayFromZero)}% OFF";

                candidates.Add(new OfferCandidate
                {
                    OfferType = "SPECIAL_PRICE",
                    OfferPolicyId = item.PriceListId,
                    OfferName = pl.PriceListName,
                    OriginalPrice = originalPrice,
                    SellingPrice = sellingPrice,
                    OfferPrice = isConditional ? null : sellingPrice,
                    DiscountLabel = label,
                    RequiresCartValidation = isConditional,
                    RequiresManagerApproval = false,
                    Priority = pl.Priority,
                    EndsAt = pl.ValidUntil
                });
            }
        }

        // 2. Discount Policies
        foreach (var policy in eligiblePolicies)
        {
            var policyTargetsList = policyTargets.Where(x => x.DiscountPolicyId == policy.Id).ToList();
            var excludes = policyTargetsList.Where(x => x.TargetMode == "EXCLUDE").ToList();
            var includes = policyTargetsList.Where(x => x.TargetMode == "INCLUDE").ToList();

            var isExcluded = excludes.Any(t => MatchesTarget(t, productId, variantId, categoryIds, brandId, collectionIds));
            if (isExcluded) continue;

            if (includes.Count > 0)
            {
                var isIncluded = includes.Any(t => MatchesTarget(t, productId, variantId, categoryIds, brandId, collectionIds));
                if (!isIncluded) continue;
            }

            var policyConds = policyConditions.Where(x => x.DiscountPolicyId == policy.Id).ToList();
            var isConditional = policyConds.Count > 0 ||
                                policy.MinOrderAmount.HasValue && policy.MinOrderAmount.Value > 0 ||
                                policy.MinQuantity.HasValue && policy.MinQuantity.Value > 1;

            if (!discountTypes.TryGetValue(policy.DiscountTypeId, out var calcMethod)) continue;

            int? offerPrice = null;
            if (!isConditional)
            {
                if (calcMethod == "PERCENTAGE")
                {
                    var discountAmt = basePrice * (policy.DiscountValue / 100m);
                    if (policy.MaxDiscountAmount.HasValue)
                    {
                        discountAmt = Math.Min(discountAmt, policy.MaxDiscountAmount.Value);
                    }
                    var calculatedOfferPrice = Math.Max(basePrice - discountAmt, 0m);
                    offerPrice = (int)Math.Round(calculatedOfferPrice, MidpointRounding.AwayFromZero);
                }
                else if (calcMethod == "FIXED_AMOUNT")
                {
                    var discountValue = policy.DiscountValue;
                    if (policy.MaxDiscountAmount.HasValue)
                    {
                        discountValue = Math.Min(discountValue, policy.MaxDiscountAmount.Value);
                    }
                    var calculatedOfferPrice = Math.Max(basePrice - discountValue, 0m);
                    offerPrice = (int)Math.Round(calculatedOfferPrice, MidpointRounding.AwayFromZero);
                }
            }

            var label = "Offer available";
            if (!isConditional)
            {
                if (calcMethod == "PERCENTAGE")
                {
                    label = $"{(int)Math.Round(policy.DiscountValue, MidpointRounding.AwayFromZero)}% OFF";
                }
                else if (calcMethod == "FIXED_AMOUNT")
                {
                    label = $"LKR {(int)Math.Round(policy.DiscountValue, MidpointRounding.AwayFromZero)} OFF";
                }
            }

            candidates.Add(new OfferCandidate
            {
                OfferType = calcMethod,
                OfferPolicyId = policy.Id,
                OfferName = policy.DiscountPolicyName,
                OriginalPrice = basePrice,
                SellingPrice = basePrice,
                OfferPrice = offerPrice,
                DiscountLabel = label,
                RequiresCartValidation = isConditional,
                RequiresManagerApproval = policy.RequiresManagerApproval,
                Priority = policy.Priority,
                EndsAt = policy.EndsAt
            });
        }

        return ResolveBestOffer(candidates);
    }
}
