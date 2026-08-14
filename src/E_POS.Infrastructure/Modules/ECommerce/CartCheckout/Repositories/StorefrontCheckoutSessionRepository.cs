using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
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

public sealed class StorefrontCheckoutSessionRepository : StorefrontCheckoutRepositoryBase, IStorefrontCheckoutSessionRepository
{
    public StorefrontCheckoutSessionRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
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

        var customer = await DbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == customerId && x.Status == Active,
                cancellationToken);
        if (customer is null) return Failure("storefront_checkout.customer_not_found");

        var outlet = await DbContext.Outlets.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == request.SelectedOutletId && x.Status == Active,
                cancellationToken);
        if (outlet is null) return Failure("storefront_checkout.outlet_not_found");

        var currencyCode = await ResolveCurrencyAsync(tenantId, cancellationToken);
        var cart = await DbContext.ShoppingCarts.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.AnonymousSessionId == cartSessionId &&
            x.CurrencyCode == currencyCode &&
            x.CartStatus == Active &&
            (!x.ExpiresAt.HasValue || x.ExpiresAt > now),
            cancellationToken);
        if (cart is null) return Failure("storefront_checkout.cart_not_found");

        var items = await DbContext.ShoppingCartItems
            .Where(x => x.TenantId == tenantId && x.ShoppingCartId == cart.Id && x.LineStatus == Active)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        if (items.Count == 0) return Failure("storefront_checkout.cart_empty");

        var existingSession = await DbContext.CheckoutSessions
            .Where(x =>
                x.TenantId == tenantId && x.CustomerId == customerId && x.CartId == cart.Id &&
                (x.CheckoutStatus == "STARTED" || x.CheckoutStatus == "PENDING") &&
                (!x.ExpiredAt.HasValue || x.ExpiredAt > now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSession is not null)
        {
            var existingLines = await DbContext.CheckoutSessionLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CheckoutSessionId == existingSession.Id)
                .ToListAsync(cancellationToken);

            var isStale = existingSession.SelectedOutletId != request.SelectedOutletId ||
                          existingSession.RequestedCollectionAt != request.RequestedCollectionAt ||
                          existingLines.Count != items.Count ||
                          existingLines.Any(el =>
                          {
                              var match = items.FirstOrDefault(i => i.ProductId == el.ProductId && i.ProductVariantId == el.ProductVariantId);
                              return match is null || match.Quantity != el.Quantity;
                          });

            if (!isStale)
            {
                return Success(await BuildReadModelAsync(existingSession, cancellationToken));
            }

            await ExpireSessionInternalAsync(tenantId, existingSession, now, cancellationToken);
        }

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

        await using var transaction = DbContext.Database.IsRelational()
            ? await DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
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

        if (request.RequestedCollectionAt.HasValue)
        {
            var requestedAtUtc = request.RequestedCollectionAt.Value.ToUniversalTime();
            var collection = await ValidateCollectionAsync(
                tenantId, outlet.Id, outlet.Timezone, requestedAtUtc, now, cancellationToken);
            if (collection.ErrorCode is not null) return Failure(collection.ErrorCode);
            
            checkout.SelectCollection(
                outlet.Id,
                requestedAtUtc,
                collection.RequestedCollectionEndAt!.Value,
                outlet.Timezone,
                now);
            
            DbContext.CheckoutEvents.Add(CheckoutEvent.Record(
                Guid.NewGuid(), tenantId, checkoutId, "COLLECTION_SELECTION_UPDATED", "SUCCEEDED",
                JsonSerializer.Serialize(new
                {
                    outletId = outlet.Id,
                    requestedCollectionAt = requestedAtUtc,
                    requestedCollectionEndAt = collection.RequestedCollectionEndAt
                }),
                now));
        }

        DbContext.CheckoutSessions.Add(checkout);

        foreach (var item in items)
            DbContext.CheckoutSessionLines.Add(
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
        DbContext.InventoryReservations.Add(reservation);

        foreach (var selection in selections)
        {
            var reservationLine = InventoryReservationLine.Create(
                Guid.NewGuid(), tenantId, reservationId, selection.Item.LineNumber,
                selection.Item.ProductId, selection.Item.ProductVariantId,
                selection.Item.Quantity, "RESERVED", now);
            reservationLine.UpdateQuantities(selection.Item.Quantity, 0m, 0m, now);
            DbContext.InventoryReservationLines.Add(reservationLine);

            var remaining = selection.Item.Quantity;
            foreach (var balance in selection.Balances.Where(x => x.AvailableQuantity > 0m))
            {
                if (remaining <= 0m) break;
                var allocated = Math.Min(remaining, balance.AvailableQuantity);
                balance.AdjustQuantities(0m, allocated, 0m, 0m, now);
                DbContext.InventoryReservationAllocations.Add(
                    InventoryReservationAllocation.Create(
                        Guid.NewGuid(), tenantId, reservationLine.Id, balance.Id, null,
                        allocated, "ALLOCATED", now, now));
                remaining -= allocated;
            }
        }

        checkout.AttachInventoryReservation(reservationId, now);
        DbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), tenantId, checkoutId, "CHECKOUT_STARTED", "SUCCEEDED",
            JsonSerializer.Serialize(new { cartId = cart.Id, outletId = outlet.Id }), now));

        await DbContext.SaveChangesAsync(cancellationToken);
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
        var checkout = await DbContext.CheckoutSessions.AsNoTracking()
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
        await using var transaction = DbContext.Database.IsRelational()
            ? await DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var checkout = await DbContext.CheckoutSessions.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.CustomerId == customerId && x.Id == checkoutSessionId,
            cancellationToken);
        if (checkout is null) return Failure("storefront_checkout.session_not_found");
        var accessError = await GetAccessErrorAsync(tenantId, now, cancellationToken);
        if (accessError is not null) return Failure(accessError);
        if (checkout.ExpiredAt.HasValue && checkout.ExpiredAt <= now)
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Failure("storefront_checkout.session_expired");
        }
        if (checkout.CheckoutStatus is not ("STARTED" or "PENDING"))
            return Failure("storefront_checkout.invalid_state");

        var outlet = await DbContext.Outlets.AsNoTracking().FirstOrDefaultAsync(x =>
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
            ? await DbContext.InventoryReservations.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == checkout.InventoryReservationId.Value,
                cancellationToken)
            : null;
        if (currentReservation is null ||
            currentReservation.ReservationStatus is "RELEASED" or "EXPIRED" or "CANCELLED" ||
            (currentReservation.ExpiresAt.HasValue && currentReservation.ExpiresAt <= now))
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Failure("storefront_checkout.session_expired");
        }

        if (!sameOutlet)
        {
            var checkoutLines = await DbContext.CheckoutSessionLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.CheckoutSessionId == checkoutSessionId && x.LineStatus == Active)
                .OrderBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);
            if (checkoutLines.Count == 0) return Failure("storefront_checkout.cart_empty");

            var selections = new List<CheckoutReservationSelection>(checkoutLines.Count);
            foreach (var line in checkoutLines)
            {
                var balances = await (
                        from balance in DbContext.InventoryBalances
                        join location in DbContext.InventoryLocations
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
            DbContext.InventoryReservations.Add(reservation);

            foreach (var selection in selections)
            {
                var reservationLine = InventoryReservationLine.Create(
                    Guid.NewGuid(), tenantId, reservationId, selection.Line.LineNumber,
                    selection.Line.ProductId, selection.Line.ProductVariantId,
                    selection.Line.Quantity, "RESERVED", now);
                reservationLine.UpdateQuantities(selection.Line.Quantity, 0m, 0m, now);
                DbContext.InventoryReservationLines.Add(reservationLine);

                var remaining = selection.Line.Quantity;
                foreach (var balance in selection.Balances.Where(x => x.AvailableQuantity > 0m))
                {
                    if (remaining <= 0m) break;
                    var allocated = Math.Min(remaining, balance.AvailableQuantity);
                    balance.AdjustQuantities(0m, allocated, 0m, 0m, now);
                    DbContext.InventoryReservationAllocations.Add(
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
        DbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), tenantId, checkout.Id, "COLLECTION_SELECTION_UPDATED", "SUCCEEDED",
            JsonSerializer.Serialize(new
            {
                outletId = outlet.Id,
                requestedCollectionAt = requestedAtUtc,
                requestedCollectionEndAt = collection.RequestedCollectionEndAt
            }),
            now));

        await DbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Success(await BuildReadModelAsync(checkout, cancellationToken));
    }

}
