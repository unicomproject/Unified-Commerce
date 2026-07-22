using E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;

public sealed class CustomerWishlistRepository : ICustomerWishlistRepository
{
    private const string ActiveStatus = "ACTIVE";
    private const string DefaultWishlistName = "My Wishlist";
    private readonly EPosDbContext _dbContext;

    public CustomerWishlistRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerWishlistRepositoryResult> GetAsync(
        Guid tenantId,
        Guid customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var wishlist = await FindWishlistAsync(tenantId, customerId, cancellationToken);
        return CustomerWishlistRepositoryResult.Success(
            wishlist is null
                ? EmptyWishlist(customerId)
                : await BuildReadModelAsync(wishlist, now, cancellationToken));
    }

    public async Task<CustomerWishlistRepositoryResult> AddItemAsync(
        Guid tenantId,
        Guid customerId,
        AddCustomerWishlistItemRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var customerExists = await _dbContext.Set<CustomerEntity>()
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == customerId &&
                     x.Status == ActiveStatus,
                cancellationToken);
        if (!customerExists)
            return CustomerWishlistRepositoryResult.Failure("customer_wishlist.customer_not_found");

        var productExists = await _dbContext.Set<Product>()
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == request.ProductId &&
                     x.Status == ActiveStatus &&
                     x.IsSellable,
                cancellationToken);
        if (!productExists)
            return CustomerWishlistRepositoryResult.Failure("customer_wishlist.product_not_found");

        if (request.ProductVariantId.HasValue)
        {
            var variantExists = await _dbContext.Set<ProductVariant>()
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId &&
                         x.ProductId == request.ProductId &&
                         x.Id == request.ProductVariantId.Value &&
                         x.Status == ActiveStatus &&
                         x.IsSellable,
                    cancellationToken);
            if (!variantExists)
                return CustomerWishlistRepositoryResult.Failure("customer_wishlist.variant_not_found");
        }

        var wishlist = await FindWishlistAsync(tenantId, customerId, cancellationToken);
        if (wishlist is null)
        {
            wishlist = CustomerWishlist.Create(
                tenantId,
                customerId,
                DefaultWishlistName,
                now);
            _dbContext.CustomerWishlists.Add(wishlist);
        }

        var item = wishlist.AddItem(request.ProductId, request.ProductVariantId, now);
        if (_dbContext.Entry(item).State == EntityState.Detached)
            _dbContext.CustomerWishlistItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CustomerWishlistRepositoryResult.Success(
            await BuildReadModelAsync(wishlist, now, cancellationToken));
    }

    public async Task<CustomerWishlistRepositoryResult> RemoveItemAsync(
        Guid tenantId,
        Guid customerId,
        Guid itemId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var wishlist = await FindWishlistAsync(tenantId, customerId, cancellationToken);
        var item = wishlist?.Items.FirstOrDefault(x => x.Id == itemId);
        if (wishlist is null || item is null || !wishlist.RemoveItem(itemId, now))
            return CustomerWishlistRepositoryResult.Failure("customer_wishlist.item_not_found");

        _dbContext.CustomerWishlistItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CustomerWishlistRepositoryResult.Success(
            await BuildReadModelAsync(wishlist, now, cancellationToken));
    }

    public async Task<CustomerWishlistRepositoryResult> ClearAsync(
        Guid tenantId,
        Guid customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var wishlist = await FindWishlistAsync(tenantId, customerId, cancellationToken);
        if (wishlist is null)
            return CustomerWishlistRepositoryResult.Success(EmptyWishlist(customerId));

        var items = wishlist.Items.ToList();
        wishlist.Clear(now);
        _dbContext.CustomerWishlistItems.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CustomerWishlistRepositoryResult.Success(
            await BuildReadModelAsync(wishlist, now, cancellationToken));
    }

    private Task<CustomerWishlist?> FindWishlistAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        _dbContext.CustomerWishlists
            .Include(x => x.Items)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.CustomerId == customerId,
                cancellationToken);

    private async Task<CustomerWishlistReadModel> BuildReadModelAsync(
        CustomerWishlist wishlist,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var productIds = wishlist.Items.Select(x => x.ProductId).Distinct().ToList();
        var currencyCode = await ResolveCurrencyAsync(wishlist.TenantId, cancellationToken);
        if (productIds.Count == 0)
        {
            return new CustomerWishlistReadModel
            {
                Id = wishlist.Id,
                CustomerId = wishlist.CustomerId,
                Name = wishlist.Name,
                Items = []
            };
        }

        var products = await _dbContext.Set<Product>()
            .AsNoTracking()
            .Where(x => x.TenantId == wishlist.TenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var variantIds = wishlist.Items
            .Where(x => x.ProductVariantId.HasValue)
            .Select(x => x.ProductVariantId!.Value)
            .Distinct()
            .ToList();
        var variants = variantIds.Count == 0
            ? new Dictionary<Guid, ProductVariant>()
            : await _dbContext.Set<ProductVariant>()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == wishlist.TenantId &&
                    variantIds.Contains(x.Id) &&
                    productIds.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var priceRows = await (from item in _dbContext.Set<PriceListItem>().AsNoTracking()
                join priceList in _dbContext.Set<PriceList>().AsNoTracking()
                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                where item.TenantId == wishlist.TenantId &&
                      productIds.Contains(item.ProductId) &&
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
        var productPrices = priceRows
            .Where(x => !x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First().SellingPrice);
        var variantPrices = priceRows
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(x => x.Key, x => x.First().SellingPrice);

        var imageRows = await _dbContext.Set<ProductImage>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == wishlist.TenantId &&
                productIds.Contains(x.ProductId) &&
                x.Status == ActiveStatus)
            .OrderByDescending(x => x.IsPrimaryImage)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var productImages = imageRows
            .Where(x => !x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First().ImageUrl);
        var variantImages = imageRows
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(x => x.Key, x => x.First().ImageUrl);

        var inventoryRows = await _dbContext.Set<InventoryBalance>()
            .AsNoTracking()
            .Where(x => x.TenantId == wishlist.TenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        var items = wishlist.Items
            .OrderByDescending(x => x.AddedAt)
            .Select(item =>
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    return null;

                ProductVariant? variant = null;
                if (item.ProductVariantId.HasValue)
                    variants.TryGetValue(item.ProductVariantId.Value, out variant);

                var price = item.ProductVariantId.HasValue &&
                            variantPrices.TryGetValue(item.ProductVariantId.Value, out var variantPrice)
                    ? variantPrice
                    : productPrices.GetValueOrDefault(item.ProductId);
                var imageUrl = item.ProductVariantId.HasValue &&
                               variantImages.TryGetValue(item.ProductVariantId.Value, out var variantImage)
                    ? variantImage
                    : productImages.GetValueOrDefault(item.ProductId);
                var matchingInventory = item.ProductVariantId.HasValue
                    ? inventoryRows.Where(x =>
                        x.ProductId == item.ProductId &&
                        x.ProductVariantId == item.ProductVariantId)
                    : inventoryRows.Where(x => x.ProductId == item.ProductId);
                var inventory = matchingInventory.ToList();
                var productAvailable = product.Status == ActiveStatus && product.IsSellable;
                var variantAvailable = !item.ProductVariantId.HasValue ||
                                       variant is { Status: ActiveStatus, IsSellable: true };

                return new CustomerWishlistItemReadModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = product.ProductName,
                    ProductSlug = product.ProductSlug,
                    VariantName = variant?.VariantName,
                    Price = price,
                    CurrencyCode = currencyCode,
                    ImageUrl = imageUrl,
                    IsInStock = inventory.Count == 0 || inventory.Sum(x => x.AvailableQuantity) > 0m,
                    IsAvailable = productAvailable && variantAvailable,
                    AddedAt = item.AddedAt
                };
            })
            .Where(x => x is not null)
            .Cast<CustomerWishlistItemReadModel>()
            .ToList();

        return new CustomerWishlistReadModel
        {
            Id = wishlist.Id,
            CustomerId = wishlist.CustomerId,
            Name = wishlist.Name,
            Items = items
        };
    }

    private static CustomerWishlistReadModel EmptyWishlist(Guid customerId) => new()
    {
        Id = Guid.Empty,
        CustomerId = customerId,
        Name = DefaultWishlistName,
        Items = []
    };

    private async Task<string> ResolveCurrencyAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.BaseCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken) ?? "LKR";
}
