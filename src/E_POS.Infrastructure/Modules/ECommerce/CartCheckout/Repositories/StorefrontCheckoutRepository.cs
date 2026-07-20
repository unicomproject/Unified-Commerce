using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;

public sealed class StorefrontCheckoutRepository : IStorefrontCheckoutRepository
{
    private const string Active = "ACTIVE";
    private static readonly string[] RequiredCheckoutFeatures =
    [
        PlatformTenantFeatureCodes.OnlineStore,
        PlatformTenantFeatureCodes.ClickCollect
    ];
    private static readonly TimeSpan CheckoutLifetime = TimeSpan.FromMinutes(15);
    private readonly EPosDbContext _dbContext;

    public StorefrontCheckoutRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StorefrontCheckoutRepositoryResult> CreateFromCartAsync(
        Guid tenantId,
        Guid customerId,
        string cartSessionId,
        CreateStorefrontCheckoutFromCartRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null) return Failure(accessError);

        var customer = await _dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == customerId && x.Status == Active,
                cancellationToken);
        if (customer is null) return Failure("storefront_checkout.customer_not_found");

        var outlet = await _dbContext.Outlets.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == request.SelectedOutletId && x.Status == Active,
                cancellationToken);
        if (outlet is null) return Failure("storefront_checkout.outlet_not_found");

        var cart = await _dbContext.ShoppingCarts.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.AnonymousSessionId == cartSessionId &&
            x.CartStatus == Active &&
            (!x.ExpiresAt.HasValue || x.ExpiresAt > now),
            cancellationToken);
        if (cart is null) return Failure("storefront_checkout.cart_not_found");

        var existingSession = await _dbContext.CheckoutSessions.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId && x.CustomerId == customerId && x.CartId == cart.Id &&
                (x.CheckoutStatus == "STARTED" || x.CheckoutStatus == "PENDING") &&
                (!x.ExpiredAt.HasValue || x.ExpiredAt > now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingSession is not null)
            return Success(await BuildReadModelAsync(existingSession, cancellationToken));

        var items = await _dbContext.ShoppingCartItems
            .Where(x => x.TenantId == tenantId && x.ShoppingCartId == cart.Id && x.LineStatus == Active)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        if (items.Count == 0) return Failure("storefront_checkout.cart_empty");

        var onlineSalesChannelId = await ResolveOnlineSalesChannelIdAsync(tenantId, cancellationToken);
        if (!onlineSalesChannelId.HasValue)
            return Failure("storefront_checkout.sales_channel_not_configured");

        var selections = new List<CheckoutLineSelection>(items.Count);
        foreach (var item in items)
        {
            var selection = await ResolveLineSelectionAsync(
                tenantId, request.SelectedOutletId, item, now, cancellationToken);
            if (selection.ErrorCode is not null) return Failure(selection.ErrorCode);
            selections.Add(selection);
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        foreach (var selection in selections)
            selection.Item.UpdateQuantityAndPrice(
                selection.Item.Quantity, selection.UnitPrice, selection.TaxPercent, cart.IsTaxInclusive, now);
        cart.UpdateTotals(
            items.Sum(x => x.LineSubtotalAmount),
            items.Sum(x => x.LineDiscountAmount),
            items.Sum(x => x.LineTaxAmount),
            0m,
            now);

        var checkoutId = Guid.NewGuid();
        var expiresAt = now.Add(CheckoutLifetime);
        var checkout = CheckoutSession.CreateFromCart(
            checkoutId,
            tenantId,
            cart.SalesChannelId,
            cart.Id,
            customerId,
            cartSessionId,
            $"CHK-{Guid.NewGuid():N}",
            request.SelectedOutletId,
            FirstNonEmpty(request.PickupContactName, customer.Name),
            FirstNonEmpty(request.PickupContactPhone, customer.Phone),
            FirstNonEmpty(request.PickupContactEmail, customer.Email),
            cart.CurrencyCode,
            cart.IsTaxInclusive,
            cart.SubtotalAmount,
            cart.DiscountAmount,
            cart.TaxAmount,
            cart.ChargeAmount,
            expiresAt,
            now);
        _dbContext.CheckoutSessions.Add(checkout);

        foreach (var item in items)
            _dbContext.CheckoutSessionLines.Add(
                CheckoutSessionLine.CreateFromCartItem(Guid.NewGuid(), tenantId, checkoutId, item, now));

        var reservationId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(
            reservationId,
            tenantId,
            $"RES-{Guid.NewGuid():N}",
            "CHECKOUT",
            checkoutId,
            checkout.CheckoutNumber,
            onlineSalesChannelId,
            request.SelectedOutletId,
            customerId,
            "PENDING",
            now,
            expiresAt,
            null,
            now);
        _dbContext.InventoryReservations.Add(reservation);

        foreach (var selection in selections)
        {
            var reservationLine = InventoryReservationLine.Create(
                Guid.NewGuid(), tenantId, reservationId, selection.Item.LineNumber,
                selection.Item.ProductId, selection.Item.ProductVariantId,
                selection.Item.Quantity, "RESERVED", now);
            reservationLine.UpdateQuantities(selection.Item.Quantity, 0m, 0m, now);
            _dbContext.InventoryReservationLines.Add(reservationLine);

            var remaining = selection.Item.Quantity;
            foreach (var balance in selection.Balances.Where(x => x.AvailableQuantity > 0m))
            {
                if (remaining <= 0m) break;
                var allocated = Math.Min(remaining, balance.AvailableQuantity);
                balance.AdjustQuantities(0m, allocated, 0m, 0m, now);
                _dbContext.InventoryReservationAllocations.Add(
                    InventoryReservationAllocation.Create(
                        Guid.NewGuid(), tenantId, reservationLine.Id, balance.Id, null,
                        allocated, "ALLOCATED", now, now));
                remaining -= allocated;
            }
        }

        checkout.AttachInventoryReservation(reservationId, now);
        _dbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), tenantId, checkoutId, "CHECKOUT_STARTED", "SUCCEEDED",
            JsonSerializer.Serialize(new { cartId = cart.Id, outletId = outlet.Id }), now));

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Success(await BuildReadModelAsync(checkout, cancellationToken));
    }

    public async Task<StorefrontCheckoutRepositoryResult> GetAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var checkout = await _dbContext.CheckoutSessions.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.CustomerId == customerId && x.Id == checkoutSessionId,
                cancellationToken);
        if (checkout is null)
            return Failure("storefront_checkout.session_not_found");

        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        return accessError is null
            ? Success(await BuildReadModelAsync(checkout, cancellationToken))
            : Failure(accessError);
    }

    public async Task<StorefrontCheckoutRepositoryResult> UpdateCollectionAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        UpdateStorefrontCheckoutCollectionRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var checkout = await _dbContext.CheckoutSessions.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.CustomerId == customerId && x.Id == checkoutSessionId,
            cancellationToken);
        if (checkout is null) return Failure("storefront_checkout.session_not_found");
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null) return Failure(accessError);
        if (checkout.ExpiredAt.HasValue && checkout.ExpiredAt <= now)
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Failure("storefront_checkout.session_expired");
        }
        if (checkout.CheckoutStatus is not ("STARTED" or "PENDING"))
            return Failure("storefront_checkout.invalid_state");

        var outlet = await _dbContext.Outlets.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == request.SelectedOutletId && x.Status == Active,
            cancellationToken);
        if (outlet is null) return Failure("storefront_checkout.outlet_not_found");

        var requestedAtUtc = request.RequestedCollectionAt.ToUniversalTime();
        var sameOutlet = checkout.SelectedOutletId == outlet.Id;
        var sameTime = checkout.RequestedCollectionAt?.ToUniversalTime() == requestedAtUtc;
        if (sameOutlet && sameTime &&
            checkout.RequestedCollectionEndAt.HasValue &&
            !string.IsNullOrWhiteSpace(checkout.CollectionTimezoneSnapshot))
            return Success(await BuildReadModelAsync(checkout, cancellationToken));

        var collection = await ValidateCollectionAsync(
            tenantId, outlet.Id, outlet.Timezone, request.RequestedCollectionAt, now, cancellationToken);
        if (collection.ErrorCode is not null) return Failure(collection.ErrorCode);

        var currentReservation = checkout.InventoryReservationId.HasValue
            ? await _dbContext.InventoryReservations.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == checkout.InventoryReservationId.Value,
                cancellationToken)
            : null;
        if (currentReservation is null ||
            currentReservation.ReservationStatus is "RELEASED" or "EXPIRED" or "CANCELLED" ||
            (currentReservation.ExpiresAt.HasValue && currentReservation.ExpiresAt <= now))
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Failure("storefront_checkout.session_expired");
        }

        if (!sameOutlet)
        {
            var checkoutLines = await _dbContext.CheckoutSessionLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.CheckoutSessionId == checkoutSessionId && x.LineStatus == Active)
                .OrderBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);
            if (checkoutLines.Count == 0) return Failure("storefront_checkout.cart_empty");

            var selections = new List<CheckoutReservationSelection>(checkoutLines.Count);
            foreach (var line in checkoutLines)
            {
                var balances = await (
                        from balance in _dbContext.InventoryBalances
                        join location in _dbContext.InventoryLocations
                            on new { balance.TenantId, Id = balance.InventoryLocationId }
                            equals new { location.TenantId, location.Id }
                        where balance.TenantId == tenantId &&
                              location.OutletId == outlet.Id && location.Status == Active &&
                              location.IsSellableLocation && balance.ProductId == line.ProductId &&
                              (line.ProductVariantId.HasValue
                                  ? balance.ProductVariantId == line.ProductVariantId
                                  : !balance.ProductVariantId.HasValue)
                        orderby balance.ProductBatchId, balance.Id
                        select balance)
                    .ToListAsync(cancellationToken);
                if (balances.Sum(x => x.AvailableQuantity) < line.Quantity)
                    return Failure("storefront_checkout.insufficient_stock");
                selections.Add(new CheckoutReservationSelection(line, balances));
            }

            var onlineSalesChannelId = await ResolveOnlineSalesChannelIdAsync(tenantId, cancellationToken);
            if (!onlineSalesChannelId.HasValue)
                return Failure("storefront_checkout.sales_channel_not_configured");

            await ReleaseInventoryReservationAsync(
                currentReservation, "COLLECTION_OUTLET_CHANGED", now, cancellationToken);

            var reservationId = Guid.NewGuid();
            var reservation = InventoryReservation.Create(
                reservationId,
                tenantId,
                $"RES-{Guid.NewGuid():N}",
                "CHECKOUT",
                checkout.Id,
                checkout.CheckoutNumber,
                onlineSalesChannelId,
                outlet.Id,
                customerId,
                "PENDING",
                now,
                checkout.ExpiredAt,
                null,
                now);
            _dbContext.InventoryReservations.Add(reservation);

            foreach (var selection in selections)
            {
                var reservationLine = InventoryReservationLine.Create(
                    Guid.NewGuid(), tenantId, reservationId, selection.Line.LineNumber,
                    selection.Line.ProductId, selection.Line.ProductVariantId,
                    selection.Line.Quantity, "RESERVED", now);
                reservationLine.UpdateQuantities(selection.Line.Quantity, 0m, 0m, now);
                _dbContext.InventoryReservationLines.Add(reservationLine);

                var remaining = selection.Line.Quantity;
                foreach (var balance in selection.Balances.Where(x => x.AvailableQuantity > 0m))
                {
                    if (remaining <= 0m) break;
                    var allocated = Math.Min(remaining, balance.AvailableQuantity);
                    balance.AdjustQuantities(0m, allocated, 0m, 0m, now);
                    _dbContext.InventoryReservationAllocations.Add(
                        InventoryReservationAllocation.Create(
                            Guid.NewGuid(), tenantId, reservationLine.Id, balance.Id, null,
                            allocated, "ALLOCATED", now, now));
                    remaining -= allocated;
                }
            }

            checkout.AttachInventoryReservation(reservationId, now);
        }

        checkout.SelectCollection(
            outlet.Id,
            requestedAtUtc,
            collection.RequestedCollectionEndAt!.Value,
            outlet.Timezone,
            now);
        _dbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), tenantId, checkout.Id, "COLLECTION_SELECTION_UPDATED", "SUCCEEDED",
            JsonSerializer.Serialize(new
            {
                outletId = outlet.Id,
                requestedCollectionAt = requestedAtUtc,
                requestedCollectionEndAt = collection.RequestedCollectionEndAt
            }),
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Success(await BuildReadModelAsync(checkout, cancellationToken));
    }

    public async Task<StorefrontCheckoutRepositoryResult> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string idempotencyKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var checkout = await _dbContext.CheckoutSessions.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.CustomerId == customerId && x.Id == checkoutSessionId,
            cancellationToken);
        if (checkout is null) return Failure("storefront_checkout.session_not_found");
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null) return Failure(accessError);

        if (checkout.CheckoutStatus == "COMPLETED" && checkout.ConvertedOrderId.HasValue)
            return Success(await BuildReadModelAsync(checkout, cancellationToken));

        if (checkout.ExpiredAt.HasValue && checkout.ExpiredAt <= now)
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Failure("storefront_checkout.session_expired");
        }
        if (checkout.CheckoutStatus is not ("STARTED" or "PENDING"))
            return Failure("storefront_checkout.invalid_state");

        var outlet = await _dbContext.Outlets.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == checkout.SelectedOutletId && x.Status == Active,
            cancellationToken);
        if (outlet is null) return Failure("storefront_checkout.outlet_not_found");

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == customerId && x.Status == Active,
            cancellationToken);
        if (customer is null) return Failure("storefront_checkout.customer_not_found");

        var onlineSalesChannelId = await ResolveOnlineSalesChannelIdAsync(tenantId, cancellationToken);
        if (!onlineSalesChannelId.HasValue)
            return Failure("storefront_checkout.sales_channel_not_configured");

        var reservation = checkout.InventoryReservationId.HasValue
            ? await _dbContext.InventoryReservations.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == checkout.InventoryReservationId.Value,
                cancellationToken)
            : null;
        if (reservation is null ||
            reservation.ReservationStatus is "RELEASED" or "EXPIRED" or "CANCELLED" ||
            (reservation.ExpiresAt.HasValue && reservation.ExpiresAt <= now))
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Failure("storefront_checkout.session_expired");
        }

        if (!checkout.RequestedCollectionAt.HasValue ||
            !checkout.RequestedCollectionEndAt.HasValue ||
            string.IsNullOrWhiteSpace(checkout.CollectionTimezoneSnapshot))
            return Failure("storefront_checkout.collection_required");

        var collectionTimezone = checkout.CollectionTimezoneSnapshot.Trim();
        if (!string.Equals(outlet.Timezone.Trim(), collectionTimezone, StringComparison.OrdinalIgnoreCase))
            return Failure("storefront_checkout.collection_time_unavailable");

        var collection = await ValidateCollectionAsync(
            tenantId,
            outlet.Id,
            collectionTimezone,
            checkout.RequestedCollectionAt.Value,
            now,
            cancellationToken);
        if (collection.ErrorCode is not null) return Failure(collection.ErrorCode);
        checkout.SelectCollection(
            outlet.Id,
            checkout.RequestedCollectionAt.Value,
            collection.RequestedCollectionEndAt!.Value,
            collectionTimezone,
            now);

        var lines = await _dbContext.CheckoutSessionLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.CheckoutSessionId == checkoutSessionId && x.LineStatus == Active)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0) return Failure("storefront_checkout.cart_empty");

        var productIds = lines.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = lines.Where(x => x.ProductVariantId.HasValue)
            .Select(x => x.ProductVariantId!.Value).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && variantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var variantUomIds = variants.Values.Select(x => x.SalesUomId).Distinct().ToList();
        var uoms = await _dbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => variantUomIds.Contains(x.Id) ||
                        ((x.TenantId == tenantId || !x.TenantId.HasValue) && x.UomCode == "PCS"))
            .ToListAsync(cancellationToken);
        var defaultUom = uoms.Where(x => x.UomCode == "PCS")
            .OrderByDescending(x => x.TenantId == tenantId)
            .FirstOrDefault();

        foreach (var line in lines)
        {
            if (!products.ContainsKey(line.ProductId))
                return Failure("storefront_checkout.product_unavailable");
            if (line.ProductVariantId.HasValue && !variants.ContainsKey(line.ProductVariantId.Value))
                return Failure("storefront_checkout.variant_unavailable");
            var uomId = line.ProductVariantId.HasValue
                ? variants[line.ProductVariantId.Value].SalesUomId
                : defaultUom?.Id;
            if (!uomId.HasValue || uoms.All(x => x.Id != uomId.Value))
                return Failure("storefront_checkout.uom_not_configured");
        }

        var orderId = Guid.NewGuid();
        var orderNumber = await GenerateOrderSequenceAsync(tenantId, cancellationToken);
        var order = SalesOrder.CreateClickAndCollect(
            orderId,
            tenantId,
            orderNumber,
            $"CHECKOUT:{checkoutSessionId:N}:{idempotencyKey}",
            onlineSalesChannelId.Value,
            collection.FulfillmentMethodOutletId,
            checkout.FulfillmentMethodCode ?? "CLICK_AND_COLLECT",
            outlet.Id,
            outlet.OutletCode,
            outlet.OutletName,
            customerId,
            customer.Name,
            checkout.PickupContactEmail ?? customer.Email,
            checkout.PickupContactPhone ?? customer.Phone,
            checkout.CurrencyCode,
            checkout.IsTaxInclusive,
            checkout.SubtotalAmount,
            checkout.DiscountAmount,
            checkout.TaxAmount,
            checkout.ChargeAmount,
            checkout.TotalAmount,
            checkout.RequestedCollectionAt.Value,
            checkout.RequestedCollectionEndAt.Value,
            checkout.CollectionTimezoneSnapshot!,
            now);
        _dbContext.SalesOrders.Add(order);

        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            ProductVariant? variant = null;
            if (line.ProductVariantId.HasValue)
                variants.TryGetValue(line.ProductVariantId.Value, out variant);
            var uom = variant is null ? defaultUom! : uoms.First(x => x.Id == variant.SalesUomId);
            _dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForClickAndCollect(
                Guid.NewGuid(), tenantId, orderId, line.LineNumber, line.ProductId,
                line.ProductVariantId, uom.Id, line.SkuSnapshot, line.ProductNameSnapshot,
                variant?.VariantName, uom.UomCode, uom.UomName,
                product.ProductType, product.ProductStructure,
                line.Quantity, line.UnitPrice, line.LineSubtotalAmount,
                line.LineDiscountAmount, line.LineTaxAmount, checkout.IsTaxInclusive, now));
        }

        reservation.UpdateStatus("CONFIRMED", null, now);
        checkout.Complete(orderId, now);
        var cart = await _dbContext.ShoppingCarts.FirstAsync(x =>
            x.TenantId == tenantId && x.Id == checkout.CartId,
            cancellationToken);
        cart.MarkConverted(checkout.Id, orderId, customerId, now);

        _dbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), tenantId, checkoutSessionId, "CHECKOUT_CONFIRMED", "SUCCEEDED",
            JsonSerializer.Serialize(new { orderId, idempotencyKey }), now));

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Success(await BuildReadModelAsync(checkout, cancellationToken));
    }

    private async Task ExpireCheckoutAsync(
        CheckoutSession checkout,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (checkout.CheckoutStatus != "EXPIRED") checkout.Expire(now);

        if (checkout.InventoryReservationId.HasValue)
        {
            var reservation = await _dbContext.InventoryReservations.FirstOrDefaultAsync(x =>
                x.TenantId == checkout.TenantId && x.Id == checkout.InventoryReservationId.Value,
                cancellationToken);
            if (reservation is not null &&
                reservation.ReservationStatus is not ("RELEASED" or "EXPIRED" or "CANCELLED"))
            {
                var lines = await _dbContext.InventoryReservationLines
                    .Where(x => x.TenantId == checkout.TenantId &&
                                x.InventoryReservationId == reservation.Id)
                    .ToListAsync(cancellationToken);
                var lineIds = lines.Select(x => x.Id).ToList();
                var allocations = await _dbContext.InventoryReservationAllocations
                    .Where(x => x.TenantId == checkout.TenantId &&
                                lineIds.Contains(x.InventoryReservationLineId))
                    .ToListAsync(cancellationToken);
                var balanceIds = allocations.Select(x => x.InventoryBalanceId).Distinct().ToList();
                var balances = await _dbContext.InventoryBalances
                    .Where(x => x.TenantId == checkout.TenantId && balanceIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

                foreach (var allocation in allocations)
                {
                    var releasable = allocation.AllocatedQuantity -
                                     allocation.ReleasedQuantity - allocation.FulfilledQuantity;
                    if (releasable <= 0m) continue;
                    if (balances.TryGetValue(allocation.InventoryBalanceId, out var balance))
                        balance.AdjustQuantities(0m, -releasable, 0m, 0m, now);
                    allocation.UpdateQuantities(releasable, 0m, now);
                    allocation.Release(now, now);
                    allocation.UpdateStatus("RELEASED", now);
                }

                foreach (var line in lines)
                {
                    var releasable = line.ReservedQuantity - line.ReleasedQuantity - line.FulfilledQuantity;
                    if (releasable > 0m) line.UpdateQuantities(0m, releasable, 0m, now);
                    line.UpdateStatus("RELEASED", now);
                }

                reservation.Release("CHECKOUT_EXPIRED", now, null);
                reservation.UpdateStatus("EXPIRED", null, now);
            }
        }

        _dbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), checkout.TenantId, checkout.Id,
            "CHECKOUT_EXPIRED", "SUCCEEDED", null, now));
    }

    private async Task<CollectionValidationResult> ValidateCollectionAsync(
        Guid tenantId,
        Guid outletId,
        string timezoneId,
        DateTimeOffset requestedCollectionAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var configuration = await (
                from methodOutlet in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
                join method in _dbContext.FulfillmentMethods.AsNoTracking()
                    on new { methodOutlet.TenantId, Id = methodOutlet.FulfillmentMethodId }
                    equals new { method.TenantId, method.Id }
                where methodOutlet.TenantId == tenantId &&
                      methodOutlet.OutletId == outletId &&
                      methodOutlet.Status == Active && method.Status == Active &&
                      method.MethodType == "PICKUP"
                orderby method.IsDefault descending,
                    method.MethodCode,
                    method.Id,
                    methodOutlet.Id
                select new
                {
                    methodOutlet.Id,
                    methodOutlet.PreparationLeadMinutes,
                    methodOutlet.PickupWindowMinutes,
                    methodOutlet.CutoffTime
                })
            .FirstOrDefaultAsync(cancellationToken);
        if (configuration is null ||
            !configuration.PreparationLeadMinutes.HasValue ||
            !configuration.PickupWindowMinutes.HasValue ||
            configuration.PreparationLeadMinutes.Value < 0 ||
            configuration.PreparationLeadMinutes.Value > 10080 ||
            configuration.PickupWindowMinutes.Value <= 0 ||
            configuration.PickupWindowMinutes.Value > 1440)
            return CollectionValidationResult.Failure(
                "storefront_checkout.collection_configuration_missing");

        TimeZoneInfo timezone;
        try
        {
            timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return CollectionValidationResult.Failure("storefront_checkout.invalid_outlet_timezone");
        }
        catch (InvalidTimeZoneException)
        {
            return CollectionValidationResult.Failure("storefront_checkout.invalid_outlet_timezone");
        }

        var requestedUtc = requestedCollectionAt.ToUniversalTime();
        if (requestedUtc < now.ToUniversalTime().AddMinutes(configuration.PreparationLeadMinutes.Value))
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");

        var localNow = TimeZoneInfo.ConvertTime(now, timezone);
        var localRequested = TimeZoneInfo.ConvertTime(requestedUtc, timezone);
        var localDate = DateOnly.FromDateTime(localRequested.DateTime);
        var localTime = TimeOnly.FromDateTime(localRequested.DateTime);
        var localToday = DateOnly.FromDateTime(localNow.DateTime);
        if (localDate < localToday || localDate >= localToday.AddDays(14) ||
            timezone.IsInvalidTime(localRequested.DateTime) ||
            timezone.IsAmbiguousTime(localRequested.DateTime))
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");
        if (localDate == DateOnly.FromDateTime(localNow.DateTime) &&
            configuration.CutoffTime.HasValue &&
            TimeOnly.FromDateTime(localNow.DateTime) >= configuration.CutoffTime.Value)
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");

        var dayOfWeek = (short)localRequested.DayOfWeek;
        var businessHour = await _dbContext.OutletBusinessHours.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId && x.OutletId == outletId &&
                x.DayOfWeek == dayOfWeek &&
                (!x.ValidFrom.HasValue || x.ValidFrom <= localDate) &&
                (!x.ValidUntil.HasValue || x.ValidUntil >= localDate))
            .OrderByDescending(x => x.ValidFrom.HasValue || x.ValidUntil.HasValue)
            .ThenByDescending(x => x.IsClosed)
            .ThenByDescending(x => x.ValidFrom)
            .ThenBy(x => x.ValidUntil)
            .FirstOrDefaultAsync(cancellationToken);
        if (businessHour is null || businessHour.IsClosed ||
            !businessHour.OpeningTime.HasValue || !businessHour.ClosingTime.HasValue)
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");

        var windowMinutes = configuration.PickupWindowMinutes.Value;
        var localEndDateTime = DateTime.SpecifyKind(
            localRequested.DateTime.AddMinutes(windowMinutes), DateTimeKind.Unspecified);
        if (timezone.IsInvalidTime(localEndDateTime) || timezone.IsAmbiguousTime(localEndDateTime))
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");

        var localEndTime = TimeOnly.FromDateTime(localEndDateTime);
        var openingTime = businessHour.OpeningTime.Value;
        if (localTime < openingTime || localEndTime > businessHour.ClosingTime.Value ||
            localEndDateTime.Date != localRequested.Date)
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");

        var offsetFromOpening = localTime.ToTimeSpan() - openingTime.ToTimeSpan();
        if (offsetFromOpening.Ticks < 0 ||
            offsetFromOpening.Ticks % TimeSpan.FromMinutes(windowMinutes).Ticks != 0)
            return CollectionValidationResult.Failure("storefront_checkout.collection_time_unavailable");

        var endOffset = timezone.GetUtcOffset(localEndDateTime);
        var requestedEndUtc = new DateTimeOffset(localEndDateTime, endOffset).ToUniversalTime();
        return CollectionValidationResult.Success(configuration.Id, requestedEndUtc);
    }

    private async Task<string?> GetAccessErrorAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tenantAvailable = await _dbContext.Tenants.AsNoTracking().AnyAsync(
            x => x.Id == tenantId && x.Status == TenantStatusConstants.Active,
            cancellationToken);
        if (!tenantAvailable)
            return "storefront_checkout.tenant_unavailable";

        var entitlements = await (
                from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
                join feature in _dbContext.PlatformFeatures.AsNoTracking()
                    on entitlement.PlatformFeatureId equals feature.Id
                where entitlement.TenantId == tenantId &&
                      RequiredCheckoutFeatures.Contains(feature.FeatureCode) &&
                      feature.Status == SubscriptionCatalogConstants.RecordStatus.Active
                select new
                {
                    feature.FeatureCode,
                    entitlement.EntitlementStatus,
                    entitlement.IsEnabled,
                    entitlement.RevokedAt,
                    entitlement.EffectiveFrom,
                    entitlement.EffectiveUntil
                })
            .ToListAsync(cancellationToken);

        return RequiredCheckoutFeatures.All(requiredFeature =>
            entitlements.Any(x =>
                string.Equals(x.FeatureCode, requiredFeature, StringComparison.OrdinalIgnoreCase) &&
                TenantEntitlementEffectivePredicate.IsEnabled(
                    x.EntitlementStatus,
                    x.IsEnabled,
                    x.RevokedAt,
                    x.EffectiveFrom,
                    x.EffectiveUntil,
                    now)))
            ? null
            : "storefront_checkout.feature_disabled";
    }

    private async Task ReleaseInventoryReservationAsync(
        InventoryReservation reservation,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lines = await _dbContext.InventoryReservationLines
            .Where(x => x.TenantId == reservation.TenantId &&
                        x.InventoryReservationId == reservation.Id)
            .ToListAsync(cancellationToken);
        var lineIds = lines.Select(x => x.Id).ToList();
        var allocations = await _dbContext.InventoryReservationAllocations
            .Where(x => x.TenantId == reservation.TenantId &&
                        lineIds.Contains(x.InventoryReservationLineId))
            .ToListAsync(cancellationToken);
        var balanceIds = allocations.Select(x => x.InventoryBalanceId).Distinct().ToList();
        var balances = await _dbContext.InventoryBalances
            .Where(x => x.TenantId == reservation.TenantId && balanceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var allocation in allocations)
        {
            var releasable = allocation.AllocatedQuantity -
                             allocation.ReleasedQuantity - allocation.FulfilledQuantity;
            if (releasable <= 0m) continue;
            if (balances.TryGetValue(allocation.InventoryBalanceId, out var balance))
                balance.AdjustQuantities(0m, -releasable, 0m, 0m, now);
            allocation.UpdateQuantities(releasable, 0m, now);
            allocation.Release(now, now);
            allocation.UpdateStatus("RELEASED", now);
        }

        foreach (var line in lines)
        {
            var releasable = line.ReservedQuantity - line.ReleasedQuantity - line.FulfilledQuantity;
            if (releasable > 0m) line.UpdateQuantities(0m, releasable, 0m, now);
            line.UpdateStatus("RELEASED", now);
        }

        reservation.Release(reason, now, null);
        reservation.UpdateStatus("RELEASED", null, now);
    }

    private async Task<CheckoutLineSelection> ResolveLineSelectionAsync(
        Guid tenantId,
        Guid outletId,
        ShoppingCartItem item,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == item.ProductId &&
            x.Status == Active && x.IsSellable,
            cancellationToken);
        if (product is null)
            return CheckoutLineSelection.Failure(item, "storefront_checkout.product_unavailable");

        ProductVariant? variant = null;
        if (item.ProductVariantId.HasValue)
        {
            variant = await _dbContext.ProductVariants.AsNoTracking().FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ProductId == item.ProductId &&
                x.Id == item.ProductVariantId.Value && x.Status == Active && x.IsSellable,
                cancellationToken);
            if (variant is null)
                return CheckoutLineSelection.Failure(item, "storefront_checkout.variant_unavailable");
        }

        var price = await ResolvePriceAsync(
            tenantId, item.ProductId, item.ProductVariantId, item.Quantity, now, cancellationToken);
        if (!price.HasValue)
            return CheckoutLineSelection.Failure(item, "storefront_checkout.price_not_configured");

        var balances = await (
                from balance in _dbContext.InventoryBalances
                join location in _dbContext.InventoryLocations
                    on new { balance.TenantId, Id = balance.InventoryLocationId }
                    equals new { location.TenantId, location.Id }
                where balance.TenantId == tenantId &&
                      location.OutletId == outletId && location.Status == Active &&
                      location.IsSellableLocation && balance.ProductId == item.ProductId &&
                      (item.ProductVariantId.HasValue
                          ? balance.ProductVariantId == item.ProductVariantId
                          : !balance.ProductVariantId.HasValue)
                orderby balance.ProductBatchId, balance.Id
                select balance)
            .ToListAsync(cancellationToken);
        if (balances.Sum(x => x.AvailableQuantity) < item.Quantity)
            return CheckoutLineSelection.Failure(item, "storefront_checkout.insufficient_stock");

        var taxPercent = product.IsTaxable
            ? await ResolveTaxPercentAsync(
                tenantId, item.ProductId, item.ProductVariantId, now, cancellationToken)
            : 0m;
        return CheckoutLineSelection.Success(item, product, variant, price.Value, taxPercent, balances);
    }

    private async Task<decimal?> ResolvePriceAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        decimal quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await _dbContext.PriceListItems.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId && x.ProductId == productId &&
                x.Status == Active && x.MinQuantity <= quantity &&
                (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                (!x.ValidUntil.HasValue || x.ValidUntil >= now) &&
                (!x.ProductVariantId.HasValue || x.ProductVariantId == variantId))
            .OrderByDescending(x => x.ProductVariantId.HasValue)
            .ThenByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.MinQuantity)
            .Select(x => (decimal?)x.SellingPrice)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<decimal> ResolveTaxPercentAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.ProductTaxAssignments.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId && x.ProductId == productId && x.Status == Active &&
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
                from classRate in _dbContext.TaxClassRates.AsNoTracking()
                join rate in _dbContext.TaxRates.AsNoTracking()
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

    private Task<Guid?> ResolveOnlineSalesChannelIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        _dbContext.SalesChannels.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.PlatformSalesChannelId == PlatformSalesChannelSeedConstants.OnlineChannelId &&
                        x.Status == Active)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string> GenerateOrderSequenceAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var sequence = await _dbContext.DocumentNumberSequences
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DocumentType == "SALES_ORDER", cancellationToken);

        if (sequence == null)
        {
            return $"SO-WEB-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        sequence.Increment(DateTimeOffset.UtcNow);

        return $"{sequence.Prefix}{sequence.CurrentValue.ToString().PadLeft(sequence.PaddingLength, '0')}";
    }

    private async Task<StorefrontCheckoutReadModel> BuildReadModelAsync(
        CheckoutSession checkout,
        CancellationToken cancellationToken)
    {
        var lines = await _dbContext.CheckoutSessionLines.AsNoTracking()
            .Where(x => x.TenantId == checkout.TenantId && x.CheckoutSessionId == checkout.Id)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        var outletName = await _dbContext.Outlets.AsNoTracking()
            .Where(x => x.TenantId == checkout.TenantId && x.Id == checkout.SelectedOutletId)
            .Select(x => x.OutletName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        StorefrontCheckoutOrderReadModel? orderModel = null;
        if (checkout.ConvertedOrderId.HasValue)
        {
            orderModel = await _dbContext.SalesOrders.AsNoTracking()
                .Where(x => x.TenantId == checkout.TenantId &&
                            x.Id == checkout.ConvertedOrderId.Value)
                .Select(x => new StorefrontCheckoutOrderReadModel
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    Status = x.Status,
                    PaymentStatus = x.PaymentStatus,
                    FulfillmentStatus = x.FulfillmentStatus,
                    RequestedCollectionAt = x.RequestedCollectionAt,
                    RequestedCollectionEndAt = x.RequestedCollectionEndAt,
                    CollectionTimezone = x.CollectionTimezoneSnapshot
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new StorefrontCheckoutReadModel
        {
            Id = checkout.Id,
            CartId = checkout.CartId,
            CheckoutNumber = checkout.CheckoutNumber,
            Status = checkout.CheckoutStatus,
            FulfillmentMethodCode = checkout.FulfillmentMethodCode ?? "CLICK_AND_COLLECT",
            SelectedOutletId = checkout.SelectedOutletId ?? Guid.Empty,
            SelectedOutletName = outletName,
            RequestedCollectionAt = checkout.RequestedCollectionAt,
            RequestedCollectionEndAt = checkout.RequestedCollectionEndAt,
            CollectionTimezone = checkout.CollectionTimezoneSnapshot,
            PickupContactName = checkout.PickupContactName,
            PickupContactPhone = checkout.PickupContactPhone,
            PickupContactEmail = checkout.PickupContactEmail,
            CurrencyCode = checkout.CurrencyCode,
            Subtotal = checkout.SubtotalAmount,
            DiscountTotal = checkout.DiscountAmount,
            TaxTotal = checkout.TaxAmount,
            ChargeTotal = checkout.ChargeAmount,
            GrandTotal = checkout.TotalAmount,
            TotalQuantity = lines.Sum(x => x.Quantity),
            ExpiresAt = checkout.ExpiredAt,
            Items = lines.Select(x => new StorefrontCheckoutLineReadModel
            {
                Id = x.Id,
                LineNumber = x.LineNumber,
                ProductId = x.ProductId,
                ProductVariantId = x.ProductVariantId,
                Sku = x.SkuSnapshot,
                ProductName = x.ProductNameSnapshot,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Subtotal = x.LineSubtotalAmount,
                DiscountTotal = x.LineDiscountAmount,
                TaxTotal = x.LineTaxAmount,
                LineTotal = x.LineTotalAmount
            }).ToList(),
            Order = orderModel
        };
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred)
            ? preferred.Trim()
            : string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();

    private static StorefrontCheckoutRepositoryResult Success(StorefrontCheckoutReadModel checkout) =>
        StorefrontCheckoutRepositoryResult.Success(checkout);

    private static StorefrontCheckoutRepositoryResult Failure(string errorCode) =>
        StorefrontCheckoutRepositoryResult.Failure(errorCode);

    private sealed record CheckoutLineSelection(
        ShoppingCartItem Item,
        string? ErrorCode,
        Product? Product,
        ProductVariant? Variant,
        decimal UnitPrice,
        decimal TaxPercent,
        IReadOnlyList<InventoryBalance> Balances)
    {
        public static CheckoutLineSelection Failure(ShoppingCartItem item, string errorCode) =>
            new(item, errorCode, null, null, 0m, 0m, []);

        public static CheckoutLineSelection Success(
            ShoppingCartItem item,
            Product product,
            ProductVariant? variant,
            decimal unitPrice,
            decimal taxPercent,
            IReadOnlyList<InventoryBalance> balances) =>
            new(item, null, product, variant, unitPrice, taxPercent, balances);
    }

    private sealed record CheckoutReservationSelection(
        CheckoutSessionLine Line,
        IReadOnlyList<InventoryBalance> Balances);

    private sealed record CollectionValidationResult(
        string? ErrorCode,
        Guid? FulfillmentMethodOutletId,
        DateTimeOffset? RequestedCollectionEndAt)
    {
        public static CollectionValidationResult Failure(string errorCode) =>
            new(errorCode, null, null);

        public static CollectionValidationResult Success(
            Guid fulfillmentMethodOutletId,
            DateTimeOffset requestedCollectionEndAt) =>
            new(null, fulfillmentMethodOutletId, requestedCollectionEndAt);
    }
}
