using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Services;

/// <summary>
/// Canonical POS sale-line pricing used by checkout and exchange replacement.
/// Mirrors <c>PosCheckoutRepository</c> price-list, quantity-tier, inclusive/exclusive,
/// and compound-tax behavior so preview and completion stay consistent.
/// </summary>
public sealed class PosSaleLinePricingCalculator : IPosSaleLinePricingCalculator
{
    private const string ActiveStatus = "ACTIVE";
    private readonly EPosDbContext _dbContext;

    public PosSaleLinePricingCalculator(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosSaleLinePricingResult> CalculateAsync(
        Guid tenantId,
        Guid outletId,
        IReadOnlyList<PosSaleLinePricingRequest> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count == 0)
        {
            return Failure("price_not_found");
        }

        if (lines.Any(x =>
                x.LineKey == Guid.Empty ||
                x.ProductId == Guid.Empty ||
                x.VariantId == Guid.Empty ||
                x.Quantity <= 0))
        {
            return Failure("invalid_replacement");
        }

        var variantIds = lines.Select(x => x.VariantId).Distinct().ToArray();
        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == tenantId && variantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        if (variants.Count != variantIds.Length)
        {
            return Failure("variant_not_found");
        }

        var variantsById = variants.ToDictionary(x => x.Id);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != productIds.Length)
        {
            return Failure("product_not_found");
        }

        foreach (var line in lines)
        {
            var variant = variantsById[line.VariantId];
            if (!products.TryGetValue(variant.ProductId, out var product) ||
                product.Id != line.ProductId ||
                !string.Equals(product.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase) ||
                !product.IsSellable ||
                !variant.IsSellable ||
                string.Equals(variant.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
            {
                return Failure("product_not_sellable");
            }
        }

        var priceList = await ResolvePriceListAsync(tenantId, outletId, now, cancellationToken);
        if (priceList is null)
        {
            return Failure("price_not_found");
        }

        var pricesByVariant = await ResolvePricesAsync(
            tenantId,
            priceList.Id,
            lines.Select(line =>
            {
                var variant = variantsById[line.VariantId];
                return new PriceLookupInput(
                    line.VariantId,
                    variant.ProductId,
                    variant.SalesUomId,
                    line.Quantity);
            }).ToList(),
            now,
            cancellationToken);
        if (pricesByVariant.Count != variantIds.Length)
        {
            return Failure("price_not_found");
        }

        var availableByVariant = await ResolveAvailableStockAsync(
            tenantId, outletId, variantIds, cancellationToken);
        var taxPercentByVariant = await ResolveTaxPercentsAsync(
            tenantId,
            variants.Select(x => new TaxLookupInput(x.Id, x.ProductId)).ToList(),
            now,
            cancellationToken);

        var built = new List<PosSaleLinePricingLineResult>(lines.Count);
        decimal subtotal = 0m;
        decimal taxTotal = 0m;

        foreach (var line in lines)
        {
            var variant = variantsById[line.VariantId];
            var product = products[variant.ProductId];
            if (!pricesByVariant.TryGetValue(line.VariantId, out var priceRow))
            {
                return Failure("price_not_found");
            }

            availableByVariant.TryGetValue(line.VariantId, out var availableQuantity);
            if (availableQuantity < line.Quantity)
            {
                return Failure("insufficient_outlet_stock");
            }

            var listedUnitPrice = priceRow.SellingPrice;
            var taxPercent = product.IsTaxable &&
                             taxPercentByVariant.TryGetValue(line.VariantId, out var rate)
                ? rate
                : 0m;
            var unitPrice = priceList.PriceIncludesTax && taxPercent > 0m
                ? RoundMoney(listedUnitPrice * 100m / (100m + taxPercent))
                : listedUnitPrice;
            var lineSubtotal = RoundMoney(unitPrice * line.Quantity);
            var lineTax = RoundMoney(lineSubtotal * taxPercent / 100m);
            var lineTotal = RoundMoney(lineSubtotal + lineTax);

            subtotal += lineSubtotal;
            taxTotal += lineTax;
            built.Add(new PosSaleLinePricingLineResult(
                line.LineKey,
                product.Id,
                variant.Id,
                line.Quantity,
                priceRow.PriceListItemId,
                listedUnitPrice,
                unitPrice,
                lineSubtotal,
                0m,
                lineTax,
                lineTotal,
                taxPercent,
                availableQuantity));
        }

        subtotal = RoundMoney(subtotal);
        taxTotal = RoundMoney(taxTotal);
        var grandTotal = RoundMoney(subtotal + taxTotal);

        return new PosSaleLinePricingResult(
            null,
            priceList.Id,
            priceList.CurrencyCode,
            priceList.PriceIncludesTax,
            built,
            subtotal,
            0m,
            taxTotal,
            grandTotal);
    }

    private async Task<ResolvedPriceList?> ResolvePriceListAsync(
        Guid tenantId,
        Guid outletId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var posChannelId = await (
                from sc in _dbContext.SalesChannels.AsNoTracking()
                join psc in _dbContext.PlatformSalesChannels.AsNoTracking()
                    on sc.PlatformSalesChannelId equals psc.Id
                where sc.TenantId == tenantId &&
                      psc.ChannelType == "POS" &&
                      sc.Status == ActiveStatus
                orderby sc.SortOrder
                select (Guid?)sc.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var candidates = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == ActiveStatus &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .Select(x => new ResolvedPriceList(
                x.Id,
                x.CurrencyCode,
                x.PriceIncludesTax,
                x.IsDefaultPriceList,
                x.Priority,
                _dbContext.PriceListOutlets.Any(mapping =>
                    mapping.TenantId == tenantId &&
                    mapping.PriceListId == x.Id &&
                    mapping.OutletId == outletId &&
                    mapping.Status == ActiveStatus),
                posChannelId.HasValue && _dbContext.PriceListChannels.Any(mapping =>
                    mapping.TenantId == tenantId &&
                    mapping.PriceListId == x.Id &&
                    mapping.SalesChannelId == posChannelId.Value &&
                    mapping.Status == ActiveStatus)))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(x => x.OutletMatched || x.ChannelMatched || x.IsDefault)
            .OrderByDescending(x => x.OutletMatched && x.ChannelMatched)
            .ThenByDescending(x => x.OutletMatched)
            .ThenByDescending(x => x.ChannelMatched)
            .ThenByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
    }

    private async Task<Dictionary<Guid, ResolvedPrice>> ResolvePricesAsync(
        Guid tenantId,
        Guid priceListId,
        IReadOnlyList<PriceLookupInput> inputs,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var productIds = inputs.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = inputs.Select(x => x.VariantId).ToList();
        var rows = await _dbContext.PriceListItems.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.PriceListId == priceListId &&
                        x.Status == ActiveStatus &&
                        productIds.Contains(x.ProductId) &&
                        (!x.ProductVariantId.HasValue || variantIds.Contains(x.ProductVariantId.Value)) &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .Select(x => new PriceRow(
                x.Id,
                x.ProductId,
                x.ProductVariantId,
                x.UomId,
                x.SellingPrice,
                x.MinQuantity))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, ResolvedPrice>();
        foreach (var input in inputs)
        {
            var row = rows
                .Where(x => x.ProductId == input.ProductId &&
                            (!x.ProductVariantId.HasValue || x.ProductVariantId == input.VariantId) &&
                            (!x.UomId.HasValue || x.UomId == input.UomId) &&
                            x.MinQuantity <= input.Quantity)
                .OrderByDescending(x => x.ProductVariantId.HasValue)
                .ThenByDescending(x => x.UomId.HasValue)
                .ThenByDescending(x => x.MinQuantity)
                .ThenBy(x => x.Id)
                .FirstOrDefault();
            if (row is not null)
            {
                result[input.VariantId] = new ResolvedPrice(row.SellingPrice, row.Id);
            }
        }

        return result;
    }

    private async Task<Dictionary<Guid, decimal>> ResolveAvailableStockAsync(
        Guid tenantId,
        Guid outletId,
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken) =>
        await (
                from balance in _dbContext.InventoryBalances.AsNoTracking()
                join location in _dbContext.InventoryLocations.AsNoTracking()
                    on balance.InventoryLocationId equals location.Id
                where balance.TenantId == tenantId &&
                      location.TenantId == tenantId &&
                      location.OutletId == outletId &&
                      location.Status == ActiveStatus &&
                      location.IsSellableLocation &&
                      balance.ProductVariantId.HasValue &&
                      variantIds.Contains(balance.ProductVariantId.Value)
                group balance by balance.ProductVariantId!.Value
                into grouped
                select new { VariantId = grouped.Key, Available = grouped.Sum(x => x.AvailableQuantity) })
            .ToDictionaryAsync(x => x.VariantId, x => x.Available, cancellationToken);

    private async Task<Dictionary<Guid, decimal>> ResolveTaxPercentsAsync(
        Guid tenantId,
        IReadOnlyList<TaxLookupInput> inputs,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var productIds = inputs.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = inputs.Select(x => x.VariantId).ToList();
        var assignments = await _dbContext.ProductTaxAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status == ActiveStatus &&
                        productIds.Contains(x.ProductId) &&
                        (!x.ProductVariantId.HasValue || variantIds.Contains(x.ProductVariantId.Value)) &&
                        (!x.AppliesFrom.HasValue || x.AppliesFrom <= now) &&
                        (!x.AppliesUntil.HasValue || x.AppliesUntil >= now))
            .Select(x => new { x.ProductId, x.ProductVariantId, x.TaxClassId, x.AppliesFrom })
            .ToListAsync(cancellationToken);
        var classIds = assignments.Select(x => x.TaxClassId).Distinct().ToList();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var rates = await (
                from classRate in _dbContext.TaxClassRates.AsNoTracking()
                join taxRate in _dbContext.TaxRates.AsNoTracking()
                    on new { classRate.TenantId, Id = classRate.TaxRateId }
                    equals new { taxRate.TenantId, taxRate.Id }
                where classRate.TenantId == tenantId &&
                      classRate.Status == ActiveStatus &&
                      classIds.Contains(classRate.TaxClassId) &&
                      taxRate.Status == ActiveStatus &&
                      (!taxRate.ValidFrom.HasValue || taxRate.ValidFrom <= today) &&
                      (!taxRate.ValidUntil.HasValue || taxRate.ValidUntil >= today)
                select new TaxRateRow(
                    classRate.TaxClassId,
                    classRate.SortOrder,
                    taxRate.RatePercent,
                    taxRate.IsCompound))
            .ToListAsync(cancellationToken);

        var effectiveByClass = rates.GroupBy(x => x.TaxClassId).ToDictionary(
            group => group.Key,
            group => group.OrderBy(x => x.SortOrder).Aggregate(0m, (effective, rate) =>
                effective + (rate.IsCompound
                    ? (100m + effective) * rate.RatePercent / 100m
                    : rate.RatePercent)));

        var result = new Dictionary<Guid, decimal>();
        foreach (var input in inputs)
        {
            var assignment = assignments
                .Where(x => x.ProductId == input.ProductId &&
                            (!x.ProductVariantId.HasValue || x.ProductVariantId == input.VariantId))
                .OrderByDescending(x => x.ProductVariantId.HasValue)
                .ThenByDescending(x => x.AppliesFrom)
                .FirstOrDefault();
            if (assignment is not null &&
                effectiveByClass.TryGetValue(assignment.TaxClassId, out var rate))
            {
                result[input.VariantId] = rate;
            }
        }

        return result;
    }

    private static PosSaleLinePricingResult Failure(string errorCode) =>
        new(errorCode, null, null, false, [], 0m, 0m, 0m, 0m);

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private sealed record ResolvedPriceList(
        Guid Id,
        string CurrencyCode,
        bool PriceIncludesTax,
        bool IsDefault,
        int Priority,
        bool OutletMatched,
        bool ChannelMatched);

    private sealed record PriceLookupInput(
        Guid VariantId,
        Guid ProductId,
        Guid UomId,
        decimal Quantity);

    private sealed record PriceRow(
        Guid Id,
        Guid ProductId,
        Guid? ProductVariantId,
        Guid? UomId,
        decimal SellingPrice,
        decimal MinQuantity);

    private sealed record ResolvedPrice(decimal SellingPrice, Guid PriceListItemId);

    private sealed record TaxLookupInput(Guid VariantId, Guid ProductId);

    private sealed record TaxRateRow(
        Guid TaxClassId,
        int SortOrder,
        decimal RatePercent,
        bool IsCompound);
}
