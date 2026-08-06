using System.Data;
using System.Text.Json;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
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

public sealed class StorefrontCheckoutConfirmationRepository : StorefrontCheckoutRepositoryBase, IStorefrontCheckoutConfirmationRepository
{
    public StorefrontCheckoutConfirmationRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<StorefrontCheckoutRepositoryResult> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string idempotencyKey,
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
        DbContext.SalesOrders.Add(order);

        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            ProductVariant? variant = null;
            if (line.ProductVariantId.HasValue)
                variants.TryGetValue(line.ProductVariantId.Value, out variant);
            var uom = variant is null ? defaultUom! : uoms.First(x => x.Id == variant.SalesUomId);
            DbContext.SalesOrderLines.Add(SalesOrderLine.CreateForClickAndCollect(
                Guid.NewGuid(), tenantId, orderId, line.LineNumber, line.ProductId,
                line.ProductVariantId, uom.Id, line.SkuSnapshot, line.ProductNameSnapshot,
                variant?.VariantName, uom.UomCode, uom.UomName,
                product.ProductType, product.ProductStructure,
                line.Quantity, line.UnitPrice, line.LineSubtotalAmount,
                line.LineDiscountAmount, line.LineTaxAmount, checkout.IsTaxInclusive, now));
        }

        reservation.UpdateStatus("CONFIRMED", null, now);
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

}
