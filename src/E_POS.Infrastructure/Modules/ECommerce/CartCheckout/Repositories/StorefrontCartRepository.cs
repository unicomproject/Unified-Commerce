using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;

public sealed class StorefrontCartRepository : IStorefrontCartRepository
{
    private const string Active = "ACTIVE";
    private readonly EPosDbContext _dbContext;

    public StorefrontCartRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StorefrontCartRepositoryResult> GetAsync(
        Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cart = await FindActiveCartAsync(tenantId, sessionId, now, false, cancellationToken);
        return cart is null
            ? StorefrontCartRepositoryResult.Success(await CreateEmptyReadModelAsync(tenantId, now, cancellationToken))
            : StorefrontCartRepositoryResult.Success(await BuildReadModelAsync(cart, cancellationToken));
    }

    public async Task<StorefrontCartRepositoryResult> AddItemAsync(
        Guid tenantId,
        string sessionId,
        AddStorefrontCartItemRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var selection = await ResolveSelectionAsync(
            tenantId, request.ProductId, request.ProductVariantId, request.Quantity, now, cancellationToken);
        if (selection.ErrorCode is not null)
            return StorefrontCartRepositoryResult.Failure(selection.ErrorCode);

        var cart = await FindActiveCartAsync(tenantId, sessionId, now, true, cancellationToken)
                   ?? await CreateCartAsync(tenantId, sessionId, now, cancellationToken);

        var item = await _dbContext.Set<ShoppingCartItem>()
            .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.ShoppingCartId == cart.Id &&
                    x.ProductId == request.ProductId &&
                    x.ProductVariantId == request.ProductVariantId &&
                    x.LineStatus == Active,
                cancellationToken);

        var newQuantity = (item?.Quantity ?? 0m) + request.Quantity;
        if (selection.HasInventory && selection.AvailableQuantity < newQuantity)
            return StorefrontCartRepositoryResult.Failure("storefront_cart.insufficient_stock");

        if (item is null)
        {
            var nextLine = await _dbContext.Set<ShoppingCartItem>()
                .Where(x => x.TenantId == tenantId && x.ShoppingCartId == cart.Id)
                .Select(x => (int?)x.LineNumber)
                .MaxAsync(cancellationToken) ?? 0;
            item = ShoppingCartItem.Create(
                tenantId, cart.Id, nextLine + 1, selection.Product!.Id, selection.Variant?.Id,
                selection.Variant?.Sku, selection.Product.ProductName, selection.Product.ProductStructure,
                request.Quantity, selection.UnitPrice, selection.TaxPercent, cart.IsTaxInclusive, now);
            _dbContext.Set<ShoppingCartItem>().Add(item);
        }
        else
        {
            item.UpdateQuantityAndPrice(newQuantity, selection.UnitPrice, selection.TaxPercent, cart.IsTaxInclusive, now);
        }

        await RecalculateCartAsync(cart, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return StorefrontCartRepositoryResult.Success(await BuildReadModelAsync(cart, cancellationToken));
    }

    public async Task<StorefrontCartRepositoryResult> UpdateItemAsync(
        Guid tenantId,
        string sessionId,
        Guid itemId,
        decimal quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cart = await FindActiveCartAsync(tenantId, sessionId, now, true, cancellationToken);
        if (cart is null)
            return StorefrontCartRepositoryResult.Failure("storefront_cart.item_not_found");

        var item = await _dbContext.Set<ShoppingCartItem>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ShoppingCartId == cart.Id &&
                                      x.Id == itemId && x.LineStatus == Active, cancellationToken);
        if (item is null)
            return StorefrontCartRepositoryResult.Failure("storefront_cart.item_not_found");

        var selection = await ResolveSelectionAsync(
            tenantId, item.ProductId, item.ProductVariantId, quantity, now, cancellationToken);
        if (selection.ErrorCode is not null)
            return StorefrontCartRepositoryResult.Failure(selection.ErrorCode);
        if (selection.HasInventory && selection.AvailableQuantity < quantity)
            return StorefrontCartRepositoryResult.Failure("storefront_cart.insufficient_stock");

        item.UpdateQuantityAndPrice(quantity, selection.UnitPrice, selection.TaxPercent, cart.IsTaxInclusive, now);
        await RecalculateCartAsync(cart, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return StorefrontCartRepositoryResult.Success(await BuildReadModelAsync(cart, cancellationToken));
    }

    public async Task<StorefrontCartRepositoryResult> RemoveItemAsync(
        Guid tenantId,
        string sessionId,
        Guid itemId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cart = await FindActiveCartAsync(tenantId, sessionId, now, true, cancellationToken);
        if (cart is null)
            return StorefrontCartRepositoryResult.Failure("storefront_cart.item_not_found");

        var item = await _dbContext.Set<ShoppingCartItem>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ShoppingCartId == cart.Id &&
                                      x.Id == itemId && x.LineStatus == Active, cancellationToken);
        if (item is null)
            return StorefrontCartRepositoryResult.Failure("storefront_cart.item_not_found");

        item.Remove(now);
        await RecalculateCartAsync(cart, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return StorefrontCartRepositoryResult.Success(await BuildReadModelAsync(cart, cancellationToken));
    }

    public async Task<StorefrontCartRepositoryResult> ClearAsync(
        Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cart = await FindActiveCartAsync(tenantId, sessionId, now, true, cancellationToken);
        if (cart is null)
            return StorefrontCartRepositoryResult.Success(await CreateEmptyReadModelAsync(tenantId, now, cancellationToken));

        var items = await _dbContext.Set<ShoppingCartItem>()
            .Where(x => x.TenantId == tenantId && x.ShoppingCartId == cart.Id && x.LineStatus == Active)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
            item.Remove(now);

        cart.UpdateTotals(0m, 0m, 0m, 0m, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return StorefrontCartRepositoryResult.Success(await BuildReadModelAsync(cart, cancellationToken));
    }

    private async Task<ShoppingCart?> FindActiveCartAsync(
        Guid tenantId,
        string sessionId,
        DateTimeOffset now,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var currencyCode = await ResolveCurrencyAsync(tenantId, cancellationToken);
        var query = _dbContext.Set<ShoppingCart>().Where(x =>
            x.TenantId == tenantId &&
            x.AnonymousSessionId == sessionId &&
            x.CurrencyCode == currencyCode &&
            x.CartStatus == Active &&
            (!x.ExpiresAt.HasValue || x.ExpiresAt > now));
        if (!tracking) query = query.AsNoTracking();
        return await query.OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ShoppingCart> CreateCartAsync(
        Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var currency = await ResolveCurrencyAsync(tenantId, cancellationToken);
        var isTaxInclusive = await ResolveTaxInclusiveAsync(tenantId, currency, now, cancellationToken);
        var cart = ShoppingCart.Create(
            tenantId,
            PlatformSalesChannelSeedConstants.OnlineChannelId,
            null,
            sessionId,
            $"CART-{Guid.NewGuid():N}",
            currency,
            isTaxInclusive,
            now.AddDays(30),
            now);
        _dbContext.Set<ShoppingCart>().Add(cart);
        return cart;
    }

    private async Task RecalculateCartAsync(
        ShoppingCart cart, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Set<ShoppingCartItem>()
            .Where(x => x.TenantId == cart.TenantId && x.ShoppingCartId == cart.Id)
            .ToListAsync(cancellationToken);
        var addedItems = _dbContext.ChangeTracker.Entries<ShoppingCartItem>()
            .Where(x => x.State == EntityState.Added &&
                        x.Entity.TenantId == cart.TenantId &&
                        x.Entity.ShoppingCartId == cart.Id)
            .Select(x => x.Entity);
        items = items.Concat(addedItems).DistinctBy(x => x.Id).ToList();
        var activeItems = items.Where(x => x.LineStatus == Active).ToList();
        cart.UpdateTotals(
            activeItems.Sum(x => x.LineSubtotalAmount),
            activeItems.Sum(x => x.LineDiscountAmount),
            activeItems.Sum(x => x.LineTaxAmount),
            0m,
            now);
    }

    private async Task<CartSelection> ResolveSelectionAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        decimal quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Set<Product>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == productId &&
                                      x.Status == Active && x.IsSellable, cancellationToken);
        if (product is null)
            return CartSelection.Failure("storefront_cart.product_not_found");

        ProductVariant? variant = null;
        if (variantId.HasValue)
        {
            variant = await _dbContext.Set<ProductVariant>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId &&
                                          x.Id == variantId.Value && x.Status == Active && x.IsSellable,
                    cancellationToken);
            if (variant is null)
                return CartSelection.Failure("storefront_cart.variant_not_found");
        }
        else
        {
            var hasVariants = await _dbContext.Set<ProductVariant>().AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.ProductId == productId &&
                               x.Status == Active && x.IsSellable, cancellationToken);
            if (hasVariants)
                return CartSelection.Failure("storefront_cart.variant_required");
        }

        var unitPrice = await ResolvePriceAsync(tenantId, productId, variantId, quantity, now, cancellationToken);
        if (!unitPrice.HasValue)
            return CartSelection.Failure("storefront_cart.price_not_configured");

        var inventoryQuery = _dbContext.Set<InventoryBalance>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId);
        inventoryQuery = variantId.HasValue
            ? inventoryQuery.Where(x => x.ProductVariantId == variantId)
            : inventoryQuery.Where(x => !x.ProductVariantId.HasValue);
        var inventory = await inventoryQuery
            .GroupBy(_ => 1)
            .Select(x => new { Count = x.Count(), Available = x.Sum(y => y.AvailableQuantity) })
            .FirstOrDefaultAsync(cancellationToken);
        if (inventory is not null && inventory.Available < quantity)
            return CartSelection.Failure("storefront_cart.insufficient_stock");

        var taxPercent = await ResolveTaxPercentAsync(
            tenantId, productId, variantId, now, cancellationToken);
        return CartSelection.Success(
            product, variant, unitPrice.Value, taxPercent,
            inventory is not null, inventory?.Available ?? 0m);
    }

    private async Task<decimal?> ResolvePriceAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        decimal quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var currencyCode = await ResolveCurrencyAsync(tenantId, cancellationToken);
        var rows = await (from item in _dbContext.Set<PriceListItem>().AsNoTracking()
                join priceList in _dbContext.Set<PriceList>().AsNoTracking()
                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                where item.TenantId == tenantId && item.ProductId == productId &&
                      item.Status == Active && item.MinQuantity <= quantity &&
                      priceList.Status == Active &&
                      priceList.CurrencyCode == currencyCode &&
                      (!priceList.ValidFrom.HasValue || priceList.ValidFrom <= now) &&
                      (!priceList.ValidUntil.HasValue || priceList.ValidUntil >= now) &&
                      (!item.ValidFrom.HasValue || item.ValidFrom <= now) &&
                      (!item.ValidUntil.HasValue || item.ValidUntil >= now) &&
                      (!item.ProductVariantId.HasValue || item.ProductVariantId == variantId)
                orderby item.ProductVariantId.HasValue descending,
                        priceList.IsDefaultPriceList descending,
                        priceList.Priority descending,
                        item.ValidFrom ?? DateTimeOffset.MinValue descending,
                        item.MinQuantity descending
                select (decimal?)item.SellingPrice)
            .FirstOrDefaultAsync(cancellationToken);
        return rows;
    }

    private async Task<decimal> ResolveTaxPercentAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.Set<ProductTaxAssignment>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId &&
                        x.Status == Active &&
                        (!x.ProductVariantId.HasValue || x.ProductVariantId == variantId) &&
                        (!x.AppliesFrom.HasValue || x.AppliesFrom <= now) &&
                        (!x.AppliesUntil.HasValue || x.AppliesUntil >= now))
            .OrderByDescending(x => x.ProductVariantId.HasValue)
            .ThenByDescending(x => x.AppliesFrom)
            .Select(x => new { x.TaxClassId })
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null) return 0m;

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var rates = await (
            from classRate in _dbContext.Set<TaxClassRate>().AsNoTracking()
            join rate in _dbContext.Set<TaxRate>().AsNoTracking()
                on new { classRate.TenantId, Id = classRate.TaxRateId }
                equals new { rate.TenantId, rate.Id }
            where classRate.TenantId == tenantId &&
                  classRate.TaxClassId == assignment.TaxClassId &&
                  classRate.Status == Active && rate.Status == Active &&
                  (!rate.ValidFrom.HasValue || rate.ValidFrom <= today) &&
                  (!rate.ValidUntil.HasValue || rate.ValidUntil >= today)
            orderby classRate.SortOrder
            select new { rate.RatePercent, rate.IsCompound })
            .ToListAsync(cancellationToken);
        return rates.Aggregate(0m, (effective, rate) =>
            effective + (rate.IsCompound
                ? (100m + effective) * rate.RatePercent / 100m
                : rate.RatePercent));
    }

    private async Task<StorefrontCartReadModel> BuildReadModelAsync(
        ShoppingCart cart, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Set<ShoppingCartItem>().AsNoTracking()
            .Where(x => x.TenantId == cart.TenantId && x.ShoppingCartId == cart.Id && x.LineStatus == Active)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = items.Where(x => x.ProductVariantId.HasValue)
            .Select(x => x.ProductVariantId!.Value).Distinct().ToList();

        var products = await _dbContext.Set<Product>().AsNoTracking()
            .Where(x => x.TenantId == cart.TenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var variants = await _dbContext.Set<ProductVariant>().AsNoTracking()
            .Where(x => x.TenantId == cart.TenantId && variantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var images = await _dbContext.Set<ProductImage>().AsNoTracking()
            .Where(x => x.TenantId == cart.TenantId && productIds.Contains(x.ProductId) &&
                        x.Status == Active && x.IsPrimaryImage &&
                        (!x.ProductVariantId.HasValue || variantIds.Contains(x.ProductVariantId.Value)))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var balances = await _dbContext.Set<InventoryBalance>().AsNoTracking()
            .Where(x => x.TenantId == cart.TenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        var optionRows = await (
            from link in _dbContext.Set<ProductVariantOptionValue>().AsNoTracking()
            join option in _dbContext.Set<ProductOption>().AsNoTracking()
                on new { link.TenantId, Id = link.ProductOptionId }
                equals new { option.TenantId, option.Id }
            join value in _dbContext.Set<ProductOptionValue>().AsNoTracking()
                on new { link.TenantId, Id = link.ProductOptionValueId }
                equals new { value.TenantId, value.Id }
            where link.TenantId == cart.TenantId && variantIds.Contains(link.ProductVariantId)
            orderby option.SortOrder, value.SortOrder
            select new { link.ProductVariantId, option.OptionName, value.ValueName, value.DisplayName, value.ColorHex })
            .ToListAsync(cancellationToken);

        var readItems = items.Select(item =>
        {
            products.TryGetValue(item.ProductId, out var product);
            ProductVariant? variant = null;
            if (item.ProductVariantId.HasValue)
                variants.TryGetValue(item.ProductVariantId.Value, out variant);
            var inventoryRows = balances.Where(x => x.ProductId == item.ProductId &&
                (item.ProductVariantId.HasValue
                    ? x.ProductVariantId == item.ProductVariantId
                    : !x.ProductVariantId.HasValue)).ToList();
            var available = inventoryRows.Sum(x => x.AvailableQuantity);
            var image = images.FirstOrDefault(x => x.ProductId == item.ProductId &&
                                                   x.ProductVariantId == item.ProductVariantId)
                        ?? images.FirstOrDefault(x => x.ProductId == item.ProductId && !x.ProductVariantId.HasValue);
            var options = item.ProductVariantId.HasValue
                ? optionRows.Where(x => x.ProductVariantId == item.ProductVariantId.Value)
                    .Select(x => new StorefrontCartItemOptionReadModel
                    {
                        Name = x.OptionName,
                        Value = x.DisplayName ?? x.ValueName,
                        ColorHex = x.ColorHex
                    }).ToList()
                : [];
            return new StorefrontCartItemReadModel
            {
                Id = item.Id,
                LineNumber = item.LineNumber,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Slug = product?.ProductSlug,
                Name = item.ProductNameSnapshot,
                VariantName = variant?.VariantName,
                Sku = item.SkuSnapshot,
                ImageUrl = image?.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.LineSubtotalAmount,
                DiscountTotal = item.LineDiscountAmount,
                TaxTotal = item.LineTaxAmount,
                LineTotal = item.LineTotalAmount,
                IsInStock = inventoryRows.Count == 0 || available >= item.Quantity,
                Options = options
            };
        }).ToList();

        return new StorefrontCartReadModel
        {
            Id = cart.Id,
            CartNumber = cart.CartNumber,
            CurrencyCode = cart.CurrencyCode,
            Status = cart.CartStatus,
            Items = readItems,
            Subtotal = cart.SubtotalAmount,
            DiscountTotal = cart.DiscountAmount,
            TaxTotal = cart.TaxAmount,
            ChargeTotal = cart.ChargeAmount,
            GrandTotal = cart.TotalAmount,
            TotalQuantity = readItems.Sum(x => x.Quantity),
            IsTaxInclusive = cart.IsTaxInclusive,
            ExpiresAt = cart.ExpiresAt
        };
    }

    private async Task<StorefrontCartReadModel> CreateEmptyReadModelAsync(
        Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var currencyCode = await ResolveCurrencyAsync(tenantId, cancellationToken);
        return new StorefrontCartReadModel
        {
            CurrencyCode = currencyCode,
            Status = Active,
            Items = [],
            IsTaxInclusive = await ResolveTaxInclusiveAsync(tenantId, currencyCode, now, cancellationToken)
        };
    }

    private async Task<string> ResolveCurrencyAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.BaseCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken) ?? "LKR";

    private async Task<bool> ResolveTaxInclusiveAsync(
        Guid tenantId,
        string currencyCode,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<PriceList>().AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == Active &&
                        x.CurrencyCode == currencyCode &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .OrderByDescending(x => x.IsDefaultPriceList)
            .ThenByDescending(x => x.Priority)
            .Select(x => x.PriceIncludesTax)
            .FirstOrDefaultAsync(cancellationToken);

    private sealed record CartSelection(
        string? ErrorCode,
        Product? Product,
        ProductVariant? Variant,
        decimal UnitPrice,
        decimal TaxPercent,
        bool HasInventory,
        decimal AvailableQuantity)
    {
        public static CartSelection Failure(string code) => new(code, null, null, 0m, 0m, false, 0m);
        public static CartSelection Success(
            Product product, ProductVariant? variant, decimal price, decimal taxPercent,
            bool hasInventory, decimal available) =>
            new(null, product, variant, price, taxPercent, hasInventory, available);
    }
}
