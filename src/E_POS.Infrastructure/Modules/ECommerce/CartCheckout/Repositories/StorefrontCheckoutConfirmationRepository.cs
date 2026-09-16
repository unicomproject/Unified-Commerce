using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;

public sealed class StorefrontCheckoutConfirmationRepository : StorefrontCheckoutRepositoryBase, IStorefrontCheckoutConfirmationRepository
{
    public StorefrontCheckoutConfirmationRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<StorefrontCheckoutRepositoryResult> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string idempotencyKey,
        string paymentMethodCode,
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

        if (checkout.CheckoutStatus == "COMPLETED" && checkout.ConvertedOrderId.HasValue)
            return Success(await BuildReadModelAsync(checkout, cancellationToken));

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
            x.TenantId == tenantId && x.Id == checkout.SelectedOutletId && x.Status == Active,
            cancellationToken);
        if (outlet is null) return Failure("storefront_checkout.outlet_not_found");

        var customer = await DbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == customerId && x.Status == Active,
            cancellationToken);
        if (customer is null) return Failure("storefront_checkout.customer_not_found");

        var onlineSalesChannelId = await ResolveOnlineSalesChannelIdAsync(tenantId, cancellationToken);
        if (!onlineSalesChannelId.HasValue)
            return Failure("storefront_checkout.sales_channel_not_configured");

        var reservation = checkout.InventoryReservationId.HasValue
            ? await DbContext.InventoryReservations.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == checkout.InventoryReservationId.Value,
                cancellationToken)
            : null;
        if (reservation is null ||
            reservation.ReservationStatus is "RELEASED" or "EXPIRED" or "CANCELLED" ||
            (reservation.ExpiresAt.HasValue && reservation.ExpiresAt <= now))
        {
            await ExpireCheckoutAsync(checkout, now, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);
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

        var lines = await DbContext.CheckoutSessionLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.CheckoutSessionId == checkoutSessionId && x.LineStatus == Active)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0) return Failure("storefront_checkout.cart_empty");

        var productIds = lines.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = lines.Where(x => x.ProductVariantId.HasValue)
            .Select(x => x.ProductVariantId!.Value).Distinct().ToList();
        var products = await DbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var variants = await DbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && variantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var variantUomIds = variants.Values.Select(x => x.SalesUomId).Distinct().ToList();
        var uoms = await DbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => variantUomIds.Contains(x.Id) ||
                        ((x.TenantId == tenantId || !x.TenantId.HasValue) && x.UomCode == "PCS"))
            .ToListAsync(cancellationToken);
        var defaultUom = uoms.Where(x => x.UomCode == "PCS")
            .OrderByDescending(x => x.TenantId == tenantId)
            .FirstOrDefault();

        var primaryBarcodes = await DbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.IsPrimaryBarcode &&
                        x.Status == Active &&
                        ((x.ProductVariantId.HasValue &&
                          variantIds.Contains(x.ProductVariantId.Value)) ||
                         (!x.ProductVariantId.HasValue &&
                          productIds.Contains(x.ProductId))))
            .Select(x => new { x.ProductId, x.ProductVariantId, x.Barcode })
            .ToListAsync(cancellationToken);

        var barcodeByVariantId = primaryBarcodes
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Barcode).Distinct(StringComparer.Ordinal).ToList());
        var barcodeByProductId = primaryBarcodes
            .Where(x => !x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Barcode).Distinct(StringComparer.Ordinal).ToList());

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
            if (!TryResolvePrimaryBarcodeSnapshot(
                    line.ProductId,
                    line.ProductVariantId,
                    barcodeByVariantId,
                    barcodeByProductId,
                    out _))
                return Failure("storefront_checkout.barcode_unavailable");
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
        DbContext.SalesOrders.Add(order);

        if (string.Equals(paymentMethodCode, StorefrontPaymentMethodCodes.Stripe, StringComparison.OrdinalIgnoreCase))
        {
            // Order stays "UNPAID" (its PaymentStatus column only allows a fixed set of values) —
            // the pending SalesPayment row below is the actual signal that online payment is in flight.
            var paymentMethodId = await ResolveOrEnsureOnlinePaymentMethodAsync(
                tenantId, StorefrontPaymentMethodCodes.Stripe, "Pay online with card", now, cancellationToken);

            var paymentId = Guid.NewGuid();
            var paymentNumber = await GeneratePaymentNumberAsync(tenantId, cancellationToken);
            var payment = SalesPayment.CreatePendingOnlinePayment(
                paymentId,
                tenantId,
                orderId,
                paymentNumber,
                paymentMethodId,
                checkout.CurrencyCode,
                checkout.TotalAmount,
                idempotencyKey,
                now);
            DbContext.SalesPayments.Add(payment);

            DbContext.SalesPaymentTransactions.Add(SalesPaymentTransaction.CreatePendingProviderCharge(
                Guid.NewGuid(),
                tenantId,
                paymentId,
                checkout.TotalAmount,
                checkout.CurrencyCode,
                "STRIPE",
                null,
                idempotencyKey,
                now));
        }

        var orderLines = new List<SalesOrderLine>(lines.Count);
        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            ProductVariant? variant = null;
            if (line.ProductVariantId.HasValue)
                variants.TryGetValue(line.ProductVariantId.Value, out variant);
            var uom = variant is null ? defaultUom! : uoms.First(x => x.Id == variant.SalesUomId);
            if (!TryResolvePrimaryBarcodeSnapshot(
                    line.ProductId,
                    line.ProductVariantId,
                    barcodeByVariantId,
                    barcodeByProductId,
                    out var barcodeSnapshot))
                return Failure("storefront_checkout.barcode_unavailable");
<<<<<<< HEAD
            var orderLine = SalesOrderLine.CreateForClickAndCollect(
=======
            DbContext.SalesOrderLines.Add(SalesOrderLine.CreateForClickAndCollect(
>>>>>>> e48762da864721bec8833d67bc01c9213cb2d357
                Guid.NewGuid(), tenantId, orderId, line.LineNumber, line.ProductId,
                line.ProductVariantId, uom.Id, line.SkuSnapshot, barcodeSnapshot,
                line.ProductNameSnapshot, variant?.VariantName, uom.UomCode, uom.UomName,
                product.ProductType, product.ProductStructure,
                line.Quantity, line.UnitPrice, line.LineSubtotalAmount,
                line.LineDiscountAmount, line.LineTaxAmount, checkout.IsTaxInclusive, now);
            DbContext.SalesOrderLines.Add(orderLine);
            orderLines.Add(orderLine);
        }

        await CreateFulfillmentGraphAsync(
            tenantId,
            orderId,
            orderNumber,
            collection.FulfillmentMethodOutletId!.Value,
            reservation,
            orderLines,
            checkout.RequestedCollectionAt!.Value,
            checkout.RequestedCollectionEndAt!.Value,
            collectionTimezone,
            checkout.PickupContactName ?? customer.Name,
            checkout.PickupContactPhone ?? customer.Phone,
            checkout.PickupContactEmail ?? customer.Email,
            checkoutSessionId,
            now,
            cancellationToken);

        reservation.UpdateStatus("CONFIRMED", null, now);
        reservation.AttachOrder(orderId, orderNumber, now);
        checkout.Complete(orderId, now);
        var cart = await DbContext.ShoppingCarts.FirstAsync(x =>
            x.TenantId == tenantId && x.Id == checkout.CartId,
            cancellationToken);
        cart.MarkConverted(checkout.Id, orderId, customerId, now);

        DbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), tenantId, checkoutSessionId, "CHECKOUT_CONFIRMED", "SUCCEEDED",
            JsonSerializer.Serialize(new { orderId, idempotencyKey }), now));

        await DbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Success(await BuildReadModelAsync(checkout, cancellationToken));
    }

<<<<<<< HEAD
    public async Task<bool> CancelAwaitingOnlinePaymentAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var order = await DbContext.SalesOrders.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == salesOrderId, cancellationToken);
        var payment = await DbContext.SalesPayments.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == salesPaymentId, cancellationToken);
        if (order is null || payment is null) return false;
        if (!string.Equals(payment.PaymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase)) return false;

        order.CancelForFailedOnlinePayment(reason, now);
        payment.MarkFailedOrCancelled("FAILED", reason, now);
        await DbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Builds the full POS fulfilment graph (FulfillmentOrder -> FulfillmentOrderLines ->
    // PickupOrder -> PickupSlot/PickupSlotReservation) for a confirmed e-commerce order in
    // the same transaction as SalesOrder/SalesOrderLine creation, so an order can never be
    // committed without the operational graph the POS fulfilment workflow depends on.
    private async Task CreateFulfillmentGraphAsync(
        Guid tenantId,
        Guid orderId,
        string orderNumber,
        Guid fulfillmentMethodOutletId,
        InventoryReservation reservation,
        IReadOnlyList<SalesOrderLine> orderLines,
        DateTimeOffset requestedCollectionAt,
        DateTimeOffset requestedCollectionEndAt,
        string collectionTimezone,
        string pickupContactName,
        string? pickupContactPhone,
        string? pickupContactEmail,
        Guid checkoutSessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var reservationLineIds = await DbContext.InventoryReservationLines
            .Where(x => x.TenantId == tenantId && x.InventoryReservationId == reservation.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var allocatedLocationIds = await (
                from allocation in DbContext.InventoryReservationAllocations
                join balance in DbContext.InventoryBalances
                    on new { allocation.TenantId, allocation.InventoryBalanceId }
                    equals new { balance.TenantId, InventoryBalanceId = balance.Id }
                where allocation.TenantId == tenantId &&
                      reservationLineIds.Contains(allocation.InventoryReservationLineId)
                select balance.InventoryLocationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var sourceInventoryLocationId = allocatedLocationIds.Count == 1 ? allocatedLocationIds[0] : (Guid?)null;

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(collectionTimezone);
        var localStart = TimeZoneInfo.ConvertTime(requestedCollectionAt, timezone);
        var localEnd = TimeZoneInfo.ConvertTime(requestedCollectionEndAt, timezone);

        var fulfillmentOrderId = Guid.NewGuid();
        var fulfillmentOrder = FulfillmentOrder.Create(
            fulfillmentOrderId,
            tenantId,
            orderId,
            $"FUL-{orderNumber}",
            fulfillmentMethodOutletId,
            sourceInventoryLocationId,
            DateOnly.FromDateTime(localStart.DateTime),
            requestedCollectionAt,
            now);
        DbContext.FulfillmentOrders.Add(fulfillmentOrder);

        foreach (var orderLine in orderLines)
            DbContext.FulfillmentOrderLines.Add(FulfillmentOrderLine.Create(
                Guid.NewGuid(), tenantId, fulfillmentOrderId, orderLine.Id,
                orderLine.Quantity, orderLine.CancelledQuantity, now));

        var pickupSlotId = Guid.NewGuid();
        var pickupSlot = PickupSlot.CreateOpen(
            pickupSlotId,
            tenantId,
            fulfillmentMethodOutletId,
            $"ECOMM-{orderNumber}",
            DateOnly.FromDateTime(localStart.DateTime),
            TimeOnly.FromDateTime(localStart.DateTime),
            TimeOnly.FromDateTime(localEnd.DateTime),
            1,
            now);
        DbContext.PickupSlots.Add(pickupSlot);
        pickupSlot.Reserve(1, now);

        var pickupSlotReservationId = Guid.NewGuid();
        var pickupSlotReservation = PickupSlotReservation.CreatePending(
            pickupSlotReservationId, tenantId, pickupSlotId, checkoutSessionId, 1, now, now);
        pickupSlotReservation.Confirm(orderId, now);
        DbContext.PickupSlotReservations.Add(pickupSlotReservation);

        var hasEmail = !string.IsNullOrWhiteSpace(pickupContactEmail);
        var hasPhone = !string.IsNullOrWhiteSpace(pickupContactPhone);
        DbContext.PickupOrders.Add(PickupOrder.Create(
            Guid.NewGuid(),
            tenantId,
            fulfillmentOrderId,
            pickupSlotReservationId,
            $"PU-{orderNumber}",
            pickupContactName,
            pickupContactPhone,
            pickupContactEmail,
            hasEmail ? "EMAIL" : hasPhone ? "PHONE" : null,
            now));
    }

=======
>>>>>>> e48762da864721bec8833d67bc01c9213cb2d357
    private static bool TryResolvePrimaryBarcodeSnapshot(
        Guid productId,
        Guid? productVariantId,
        IReadOnlyDictionary<Guid, List<string>> barcodeByVariantId,
        IReadOnlyDictionary<Guid, List<string>> barcodeByProductId,
        out string? barcodeSnapshot)
    {
        barcodeSnapshot = null;
        List<string>? candidates;
        if (productVariantId.HasValue)
        {
            if (!barcodeByVariantId.TryGetValue(productVariantId.Value, out candidates))
                return false;
        }
        else if (!barcodeByProductId.TryGetValue(productId, out candidates))
        {
            return false;
        }

        if (candidates.Count != 1 || string.IsNullOrWhiteSpace(candidates[0]))
            return false;

        barcodeSnapshot = candidates[0].Trim();
        return true;
    }

}
