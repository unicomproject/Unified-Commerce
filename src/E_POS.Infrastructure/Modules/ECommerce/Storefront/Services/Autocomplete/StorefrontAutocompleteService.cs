using System.Collections.Concurrent;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using E_POS.Application.Modules.Shared.Media.Contracts;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Services.Autocomplete;

public sealed class StorefrontAutocompleteService : IStorefrontAutocompleteService
{
    private const string ActiveStatus = "ACTIVE";

    private readonly ConcurrentDictionary<Guid, Trie> _tenantTries = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StorefrontAutocompleteService> _logger;

    public StorefrontAutocompleteService(IServiceProvider serviceProvider, ILogger<StorefrontAutocompleteService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public StorefrontSearchReadModel GetSuggestions(Guid tenantId, string query, int limit = 10)
    {
        var safeLimit = Math.Clamp(limit, 1, 20);
        if (!_tenantTries.TryGetValue(tenantId, out var trie))
        {
            return EmptySearchResult(safeLimit);
        }

        var results = trie.Search(query, safeLimit);

        var products = results
            .Where(x => x.Type == "Product")
            .Select(x => new StorefrontProductListReadModel
            {
                Id = Guid.Parse(x.Id),
                Name = x.Name,
                Slug = x.Slug,
                ImageUrl = x.ImageUrl ?? string.Empty,
                Price = x.Price,
                CurrencyCode = "LKR",
                IsInStock = true,
                Rating = 0m,
                ReviewCount = 0,
                ShortDescription = string.Empty,
                Options = []
            })
            .ToList();

        var categories = results
            .Where(x => x.Type == "Category")
            .Select(x => new StorefrontSearchMatchReadModel
            {
                Id = Guid.Parse(x.Id),
                Name = x.Name,
                Slug = x.Slug
            })
            .ToList();

        return new StorefrontSearchReadModel
        {
            Products = new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = products,
                TotalCount = products.Count,
                Page = 1,
                PageSize = safeLimit
            },
            Categories = categories,
            Collections = []
        };
    }

    public async Task LoadAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EPosDbContext>();

        var tenantIds = await dbContext.Tenants
            .AsNoTracking()
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await LoadDataAsync(tenantId, cancellationToken);
        }
    }

    public async Task LoadDataAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var mediaReadUrlResolver = scope.ServiceProvider.GetService<IMediaReadUrlResolver>();

        try
        {
            _logger.LogInformation("Loading autocomplete data for Tenant {TenantId}.", tenantId);

            var products = await dbContext.Set<Product>()
                .AsNoTracking()
                .Where(product => product.TenantId == tenantId && product.Status == ActiveStatus && product.IsSellable)
                .Select(product => new { product.Id, product.ProductName, product.ProductSlug })
                .ToListAsync(cancellationToken);

            var productIds = products.Select(product => product.Id).ToList();
            var currencyCode = await ResolveCurrencyCodeAsync(dbContext, tenantId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var primaryImages = await GetPrimaryImagesByProductAsync(dbContext, tenantId, productIds, cancellationToken);
            var prices = await GetProductPricesByProductAsync(dbContext, tenantId, productIds, currencyCode, now, cancellationToken);

            var trie = new Trie();
            foreach (var product in products)
            {
                primaryImages.TryGetValue(product.Id, out var imageUrl);
                prices.TryGetValue(product.Id, out var price);

                var item = new AutocompleteItem
                {
                    Id = product.Id.ToString(),
                    Name = product.ProductName,
                    Slug = BuildSlug(product.ProductSlug, product.ProductName),
                    ImageUrl = imageUrl,
                    Price = price ?? 0m,
                    Type = "Product",
                    Popularity = 1
                };

                InsertItemTerms(trie, item);
            }

            var categories = await dbContext.Set<Category>()
                .AsNoTracking()
                .Where(category => category.TenantId == tenantId && category.Status == ActiveStatus)
                .Select(category => new { category.Id, category.CategoryName, category.CategorySlug })
                .ToListAsync(cancellationToken);

            foreach (var category in categories)
            {
                var item = new AutocompleteItem
                {
                    Id = category.Id.ToString(),
                    Name = category.CategoryName,
                    Slug = BuildSlug(category.CategorySlug, category.CategoryName),
                    Type = "Category",
                    Popularity = 2
                };

                InsertItemTerms(trie, item);
            }

            _tenantTries[tenantId] = trie;
            _logger.LogInformation(
                "Loaded {ProductCount} products and {CategoryCount} categories into autocomplete for Tenant {TenantId}.",
                products.Count,
                categories.Count,
                tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load autocomplete data for Tenant {TenantId}.", tenantId);
        }
    }

    private static StorefrontSearchReadModel EmptySearchResult(int pageSize) => new()
    {
        Products = new StorefrontPagedReadModel<StorefrontProductListReadModel>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = pageSize
        },
        Categories = [],
        Collections = []
    };

    private static void InsertItemTerms(Trie trie, AutocompleteItem item)
    {
        trie.Insert(item.Name, item);

        foreach (var word in item.Name.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            trie.Insert(word, item);
        }
    }

    private static string BuildSlug(string? slug, string name)
    {
        if (!string.IsNullOrWhiteSpace(slug))
        {
            return slug.Trim().ToLowerInvariant();
        }

        return name.Trim().ToLowerInvariant().Replace(" ", "-");
    }

    private static async Task<string> ResolveCurrencyCodeAsync(EPosDbContext dbContext, Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.BaseCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken) ?? "LKR";
    }

    private static async Task<Dictionary<Guid, string?>> GetPrimaryImagesByProductAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var imageRows = await (from image in dbContext.Set<ProductImage>().AsNoTracking()
                join mediaAsset in dbContext.Set<MediaAsset>().AsNoTracking()
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
                    MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                })
            .ToListAsync(cancellationToken);

        return imageRows
            .Where(row => row.MediaStatus == ActiveStatus && !string.IsNullOrWhiteSpace(row.MediaPublicUrl))
            .GroupBy(row => row.Image.ProductId)
            .ToDictionary(row => row.Key, row => (string?)row.First().MediaPublicUrl);
    }

    private static async Task<Dictionary<Guid, decimal?>> GetProductPricesByProductAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        string currencyCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var priceRows = await (from item in dbContext.Set<PriceListItem>().AsNoTracking()
                join priceList in dbContext.Set<PriceList>().AsNoTracking()
                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                where item.TenantId == tenantId &&
                      productIds.Contains(item.ProductId) &&
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
                select new { item.ProductId, item.SellingPrice })
            .ToListAsync(cancellationToken);

        return priceRows
            .GroupBy(row => row.ProductId)
            .ToDictionary(row => row.Key, row => (decimal?)row.First().SellingPrice);
    }
}