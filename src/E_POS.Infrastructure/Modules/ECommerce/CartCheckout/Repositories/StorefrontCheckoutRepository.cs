using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;

public sealed class StorefrontCheckoutRepository : IStorefrontCheckoutRepository
{
    private const string Active = "ACTIVE";
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

        PickupSlot? pickupSlot = null;
        if (request.SelectedPickupSlotId.HasValue)
        {
            pickupSlot = await (
                    from slot in _dbContext.PickupSlots
                    join methodOutlet in _dbContext.FulfillmentMethodOutlets
                        on new { slot.TenantId, Id = slot.FulfillmentMethodOutletId }
                        equals new { methodOutlet.TenantId, methodOutlet.Id }
                    where slot.TenantId == tenantId &&
                          slot.Id == request.SelectedPickupSlotId.Value &&
                          methodOutlet.OutletId == request.SelectedOutletId
                    select slot)
                .FirstOrDefaultAsync(cancellationToken);
            if (pickupSlot is null || pickupSlot.SlotStatus != "OPEN" || pickupSlot.ReservedCount >= pickupSlot.Capacity)
                return Failure("storefront_checkout.pickup_slot_unavailable");
        }

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
                selection.Item.Quantity, selection.UnitPrice, selection.TaxPercent, now);
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
            request.SelectedPickupSlotId,
            FirstNonEmpty(request.PickupContactName, customer.Name),
            FirstNonEmpty(request.PickupContactPhone, customer.Phone),
            FirstNonEmpty(request.PickupContactEmail, customer.Email),
            cart.CurrencyCode,
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
        if (pickupSlot is not null)
        {
            pickupSlot.Reserve(1, now);
            _dbContext.PickupSlotReservations.Add(
                PickupSlotReservation.CreatePending(
                    Guid.NewGuid(), tenantId, pickupSlot.Id, checkoutId, 1, expiresAt, now));
        }

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
        CancellationToken cancellationToken)
    {
        var checkout = await _dbContext.CheckoutSessions.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.CustomerId == customerId && x.Id == checkoutSessionId,
                cancellationToken);
        return checkout is null
            ? Failure("storefront_checkout.session_not_found")
            : Success(await BuildReadModelAsync(checkout, cancellationToken));
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

        var fulfillmentMethodOutletId = await (
                from methodOutlet in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
                join method in _dbContext.FulfillmentMethods.AsNoTracking()
                    on new { methodOutlet.TenantId, Id = methodOutlet.FulfillmentMethodId }
                    equals new { method.TenantId, method.Id }
                where methodOutlet.TenantId == tenantId &&
                      methodOutlet.OutletId == checkout.SelectedOutletId &&
                      methodOutlet.Status == Active && method.Status == Active &&
                      method.MethodType == "PICKUP"
                select (Guid?)methodOutlet.Id)
            .FirstOrDefaultAsync(cancellationToken);

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
        var order = SalesOrder.CreateClickAndCollect(
            orderId,
            tenantId,
            $"SO-WEB-{Guid.NewGuid():N}",
            $"CHECKOUT:{checkoutSessionId:N}:{idempotencyKey}",
            onlineSalesChannelId.Value,
            fulfillmentMethodOutletId,
            checkout.FulfillmentMethodCode ?? "CLICK_AND_COLLECT",
            outlet.Id,
            outlet.OutletCode,
            outlet.OutletName,
            customerId,
            customer.Name,
            checkout.PickupContactEmail ?? customer.Email,
            checkout.PickupContactPhone ?? customer.Phone,
            checkout.CurrencyCode,
            checkout.SubtotalAmount,
            checkout.DiscountAmount,
            checkout.TaxAmount,
            checkout.ChargeAmount,
            checkout.TotalAmount,
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
                line.LineDiscountAmount, line.LineTaxAmount, now));
        }

        reservation.UpdateStatus("CONFIRMED", null, now);
        checkout.Complete(orderId, now);
        var cart = await _dbContext.ShoppingCarts.FirstAsync(x =>
            x.TenantId == tenantId && x.Id == checkout.CartId,
            cancellationToken);
        cart.MarkConverted(checkout.Id, orderId, customerId, now);

        var slotReservation = await _dbContext.PickupSlotReservations.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.CheckoutSessionId == checkoutSessionId &&
            x.ReservationStatus == "PENDING",
            cancellationToken);
        slotReservation?.Confirm(orderId, now);

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

        var slotReservation = await _dbContext.PickupSlotReservations.FirstOrDefaultAsync(x =>
            x.TenantId == checkout.TenantId && x.CheckoutSessionId == checkout.Id &&
            x.ReservationStatus == "PENDING",
            cancellationToken);
        if (slotReservation is not null)
        {
            var slot = await _dbContext.PickupSlots.FirstAsync(x =>
                x.TenantId == checkout.TenantId && x.Id == slotReservation.PickupSlotId,
                cancellationToken);
            slot.Release(slotReservation.ReservedCapacity, now);
            slotReservation.Release("CHECKOUT_EXPIRED", now);
        }

        _dbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), checkout.TenantId, checkout.Id,
            "CHECKOUT_EXPIRED", "SUCCEEDED", null, now));
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

        StorefrontCheckoutPickupSlotReadModel? slotModel = null;
        if (checkout.SelectedPickupSlotId.HasValue)
        {
            slotModel = await _dbContext.PickupSlots.AsNoTracking()
                .Where(x => x.TenantId == checkout.TenantId &&
                            x.Id == checkout.SelectedPickupSlotId.Value)
                .Select(x => new StorefrontCheckoutPickupSlotReadModel
                {
                    SlotCode = x.SlotCode,
                    SlotDate = x.SlotDate,
                    WindowStart = x.WindowStart,
                    WindowEnd = x.WindowEnd
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

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
                    FulfillmentStatus = x.FulfillmentStatus
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
            SelectedPickupSlotId = checkout.SelectedPickupSlotId,
            PickupSlot = slotModel,
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
}
