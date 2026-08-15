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

public abstract class StorefrontCheckoutRepositoryBase
{
    protected const string Active = "ACTIVE";
    protected static readonly string[] RequiredCheckoutFeatures =
    [
        PlatformTenantFeatureCodes.OnlineStore,
        PlatformTenantFeatureCodes.ClickCollect
    ];
    protected static readonly TimeSpan CheckoutLifetime = TimeSpan.FromMinutes(15);
    private readonly IMediaReadUrlResolver? _mediaReadUrlResolver;

    protected StorefrontCheckoutRepositoryBase(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
    {
        DbContext = dbContext;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    protected EPosDbContext DbContext { get; }

    protected async Task ExpireCheckoutAsync(
        CheckoutSession checkout,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (checkout.CheckoutStatus != "EXPIRED") checkout.Expire(now);

        if (checkout.InventoryReservationId.HasValue)
        {
            var reservation = await DbContext.InventoryReservations.FirstOrDefaultAsync(x =>
                x.TenantId == checkout.TenantId && x.Id == checkout.InventoryReservationId.Value,
                cancellationToken);
            if (reservation is not null &&
                reservation.ReservationStatus is not ("RELEASED" or "EXPIRED" or "CANCELLED"))
            {
                var lines = await DbContext.InventoryReservationLines
                    .Where(x => x.TenantId == checkout.TenantId &&
                                x.InventoryReservationId == reservation.Id)
                    .ToListAsync(cancellationToken);
                var lineIds = lines.Select(x => x.Id).ToList();
                var allocations = await DbContext.InventoryReservationAllocations
                    .Where(x => x.TenantId == checkout.TenantId &&
                                lineIds.Contains(x.InventoryReservationLineId))
                    .ToListAsync(cancellationToken);
                var balanceIds = allocations.Select(x => x.InventoryBalanceId).Distinct().ToList();
                var balances = await DbContext.InventoryBalances
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

        DbContext.CheckoutEvents.Add(CheckoutEvent.Record(
            Guid.NewGuid(), checkout.TenantId, checkout.Id,
            "CHECKOUT_EXPIRED", "SUCCEEDED", null, now));
    }

    protected async Task<CollectionValidationResult> ValidateCollectionAsync(
        Guid tenantId,
        Guid outletId,
        string timezoneId,
        DateTimeOffset requestedCollectionAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var configuration = await (
                from methodOutlet in DbContext.FulfillmentMethodOutlets.AsNoTracking()
                join method in DbContext.FulfillmentMethods.AsNoTracking()
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
        // Allow a 5-minute grace period for network latency and user think-time
        if (requestedUtc < now.ToUniversalTime().AddMinutes(configuration.PreparationLeadMinutes.Value - 5))
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
        var businessHour = await DbContext.OutletBusinessHours.AsNoTracking()
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

    protected async Task<string?> GetAccessErrorAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tenantAvailable = await DbContext.Tenants.AsNoTracking().AnyAsync(
            x => x.Id == tenantId && x.Status == TenantStatusConstants.Active,
            cancellationToken);
        if (!tenantAvailable)
            return "storefront_checkout.tenant_unavailable";

        var entitlements = await (
                from entitlement in DbContext.TenantFeatureEntitlements.AsNoTracking()
                join feature in DbContext.PlatformFeatures.AsNoTracking()
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

    protected async Task ReleaseInventoryReservationAsync(
        InventoryReservation reservation,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lines = await DbContext.InventoryReservationLines
            .Where(x => x.TenantId == reservation.TenantId &&
                        x.InventoryReservationId == reservation.Id)
            .ToListAsync(cancellationToken);
        var lineIds = lines.Select(x => x.Id).ToList();
        var allocations = await DbContext.InventoryReservationAllocations
            .Where(x => x.TenantId == reservation.TenantId &&
                        lineIds.Contains(x.InventoryReservationLineId))
            .ToListAsync(cancellationToken);
        var balanceIds = allocations.Select(x => x.InventoryBalanceId).Distinct().ToList();
        var balances = await DbContext.InventoryBalances
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

    protected async Task<CheckoutLineSelection> ResolveLineSelectionAsync(
        Guid tenantId,
        Guid outletId,
        ShoppingCartItem item,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = await DbContext.Products.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == item.ProductId &&
            x.Status == Active && x.IsSellable,
            cancellationToken);
        if (product is null)
            return CheckoutLineSelection.Failure(item, "storefront_checkout.product_unavailable");

        ProductVariant? variant = null;
        if (item.ProductVariantId.HasValue)
        {
            variant = await DbContext.ProductVariants.AsNoTracking().FirstOrDefaultAsync(x =>
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
                from balance in DbContext.InventoryBalances
                join location in DbContext.InventoryLocations
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

    protected async Task<decimal?> ResolvePriceAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        decimal quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var currencyCode = await ResolveCurrencyAsync(tenantId, cancellationToken);
        return await (from item in DbContext.PriceListItems.AsNoTracking()
                join priceList in DbContext.PriceLists.AsNoTracking()
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
    }

    protected async Task<decimal> ResolveTaxPercentAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignment = await DbContext.ProductTaxAssignments.AsNoTracking()
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
                from classRate in DbContext.TaxClassRates.AsNoTracking()
                join rate in DbContext.TaxRates.AsNoTracking()
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

    protected Task<Guid?> ResolveOnlineSalesChannelIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        DbContext.SalesChannels.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.PlatformSalesChannelId == PlatformSalesChannelSeedConstants.OnlineChannelId &&
                        x.Status == Active)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    protected async Task<string> ResolveCurrencyAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await DbContext.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.BaseCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken) ?? "LKR";

    protected async Task<string> GenerateOrderSequenceAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var sequence = await DbContext.DocumentNumberSequences
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DocumentType == "SALES_ORDER", cancellationToken);

        if (sequence == null)
        {
            return $"SO-WEB-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        sequence.Increment(DateTimeOffset.UtcNow);

        return $"{sequence.Prefix}{sequence.CurrentValue.ToString().PadLeft(sequence.PaddingLength, '0')}";
    }

    protected async Task<StorefrontCheckoutReadModel> BuildReadModelAsync(
        CheckoutSession checkout,
        CancellationToken cancellationToken)
    {
        var lines = await DbContext.CheckoutSessionLines.AsNoTracking()
            .Where(x => x.TenantId == checkout.TenantId && x.CheckoutSessionId == checkout.Id)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        var outletName = await DbContext.Outlets.AsNoTracking()
            .Where(x => x.TenantId == checkout.TenantId && x.Id == checkout.SelectedOutletId)
            .Select(x => x.OutletName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var productIds = lines.Select(x => x.ProductId).Distinct().ToList();

        var primaryImages = await (from image in DbContext.Set<ProductImage>().AsNoTracking()
                                   join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                       on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                       equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id }
                                   where image.TenantId == checkout.TenantId &&
                                         productIds.Contains(image.ProductId) &&
                                         image.Status == Active &&
                                         image.IsPrimaryImage &&
                                         mediaAsset.Status == Active
                                 select new
                                 {
                                     image.ProductId,
                                     MediaContainerName = mediaAsset.ContainerName,
                                     MediaStorageKey = mediaAsset.StorageKey,
                                     MediaPublicUrl = mediaAsset.PublicUrl,
                                     MediaStatus = mediaAsset.Status
                                 })
                                 .ToListAsync(cancellationToken);

        var primaryImageDict = primaryImages
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var row = g.First();
                    return ResolveActiveMediaReadUrl(
                        row.MediaStatus,
                        row.MediaContainerName,
                        row.MediaStorageKey,
                        row.MediaPublicUrl);
                });

        StorefrontCheckoutOrderReadModel? orderModel = null;
        if (checkout.ConvertedOrderId.HasValue)
        {
            orderModel = await DbContext.SalesOrders.AsNoTracking()
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
            IsTaxInclusive = checkout.IsTaxInclusive,
            ExpiresAt = checkout.ExpiredAt,
            Items = lines.Select(x => new StorefrontCheckoutLineReadModel
            {
                Id = x.Id,
                LineNumber = x.LineNumber,
                ProductId = x.ProductId,
                ProductVariantId = x.ProductVariantId,
                Sku = x.SkuSnapshot,
                ProductName = x.ProductNameSnapshot,
                ImageUrl = primaryImageDict.GetValueOrDefault(x.ProductId),
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

    protected async Task ExpireSessionInternalAsync(
        Guid tenantId,
        CheckoutSession session,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        session.Expire(now);

        if (session.InventoryReservationId.HasValue)
        {
            var reservation = await DbContext.InventoryReservations
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == session.InventoryReservationId.Value, cancellationToken);

            if (reservation is not null)
            {
                reservation.Release("STALE_CHECKOUT_REPLACED", now, null);
                reservation.UpdateStatus("CANCELLED", null, now);

                var reservationLines = await DbContext.InventoryReservationLines
                    .Where(x => x.TenantId == tenantId && x.InventoryReservationId == reservation.Id)
                    .ToListAsync(cancellationToken);

                var lineIds = reservationLines.Select(x => x.Id).ToList();

                var allocations = await DbContext.InventoryReservationAllocations
                    .Where(x => x.TenantId == tenantId && lineIds.Contains(x.InventoryReservationLineId))
                    .ToListAsync(cancellationToken);

                var balanceIds = allocations.Select(x => x.InventoryBalanceId).Distinct().ToList();
                var balances = await DbContext.InventoryBalances
                    .Where(x => x.TenantId == tenantId && balanceIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);

                foreach (var allocation in allocations)
                {
                    if (allocation.AllocatedQuantity > allocation.ReleasedQuantity)
                    {
                        var unreleased = allocation.AllocatedQuantity - allocation.ReleasedQuantity;
                        var balance = balances.FirstOrDefault(b => b.Id == allocation.InventoryBalanceId);
                        balance?.AdjustQuantities(0m, -unreleased, 0m, 0m, now);
                        allocation.UpdateQuantities(unreleased, 0m, now);
                    }
                    allocation.Release(now, now);
                    allocation.UpdateStatus("CANCELLED", now);
                }
            }
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    protected static string? FirstNonEmpty(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred)
            ? preferred.Trim()
            : string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();

    private string? ResolveActiveMediaReadUrl(
        string? mediaStatus,
        string? containerName,
        string? storageKey,
        string? mediaPublicUrl)
    {
        return mediaStatus == Active
            ? _mediaReadUrlResolver?.ResolveReadUrl(containerName, storageKey, mediaPublicUrl)
              ?? mediaPublicUrl?.Trim()
            : null;
    }

    protected static StorefrontCheckoutRepositoryResult Success(StorefrontCheckoutReadModel checkout) =>
        StorefrontCheckoutRepositoryResult.Success(checkout);

    protected static StorefrontCheckoutRepositoryResult Failure(string errorCode) =>
        StorefrontCheckoutRepositoryResult.Failure(errorCode);

    protected sealed record CheckoutLineSelection(
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

    protected sealed record CheckoutReservationSelection(
        CheckoutSessionLine Line,
        IReadOnlyList<InventoryBalance> Balances);

    protected sealed record CollectionValidationResult(
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
