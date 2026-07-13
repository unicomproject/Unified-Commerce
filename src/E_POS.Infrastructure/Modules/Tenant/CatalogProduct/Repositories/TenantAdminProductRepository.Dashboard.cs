using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed partial class TenantAdminProductRepository
{
    public async Task<TenantAdminProductDashboardRawData> GetDashboardAsync(
        Guid tenantId,
        TenantAdminProductDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var periodDays = Math.Max(1, query.DateTo.DayNumber - query.DateFrom.DayNumber + 1);
        var previousFrom = query.DateFrom.AddDays(-periodDays);
        var previousTo = query.DateFrom.AddDays(-1);

        var currentFrom = ToUtcStart(query.DateFrom);
        var currentTo = ToUtcEnd(query.DateTo);
        var previousFromUtc = ToUtcStart(previousFrom);
        var previousToUtc = ToUtcEnd(previousTo);

        var currencyCode = await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.BaseCurrencyCode)
            .FirstAsync(cancellationToken);

        var locationIds = await ResolveInventoryLocationIdsAsync(
            tenantId,
            query.OutletId,
            cancellationToken);

        var totalProducts = await CountActiveProductsAsync(tenantId, currentTo, cancellationToken);
        var previousTotalProducts = await CountActiveProductsAsync(tenantId, previousToUtc, cancellationToken);

        var lowStock = await CountLowStockAsync(tenantId, locationIds, cancellationToken);
        var outOfStock = await CountOutOfStockAsync(tenantId, locationIds, cancellationToken);
        var expiryAlerts = await CountExpiryAlertsAsync(tenantId, locationIds, cancellationToken);

        var stockAddedCurrent = await SumStockInQuantityAsync(
            tenantId,
            locationIds,
            currentFrom,
            currentTo,
            cancellationToken);
        var stockAddedPrevious = await SumStockInQuantityAsync(
            tenantId,
            locationIds,
            previousFromUtc,
            previousToUtc,
            cancellationToken);

        var fastMovingCurrent = await CountFastMovingProductsAsync(
            tenantId,
            locationIds,
            currentFrom,
            currentTo,
            cancellationToken);
        var fastMovingPrevious = await CountFastMovingProductsAsync(
            tenantId,
            locationIds,
            previousFromUtc,
            previousToUtc,
            cancellationToken);

        var currentStockValue = await SumStockValueAsync(tenantId, locationIds, currentTo, cancellationToken);
        var previousStockValue = await SumStockValueAsync(tenantId, locationIds, previousToUtc, cancellationToken);
        var stockValueTrend = await BuildStockValueTrendAsync(
            tenantId,
            locationIds,
            query.DateFrom,
            query.DateTo,
            cancellationToken);
        var stockMovements = await CountStockMovementsAsync(
            tenantId,
            locationIds,
            currentFrom,
            currentTo,
            cancellationToken);

        return new TenantAdminProductDashboardRawData(
            currencyCode,
            new TenantAdminProductDashboardRawMetric(totalProducts, previousTotalProducts),
            new TenantAdminProductDashboardRawMetric(lowStock, lowStock),
            new TenantAdminProductDashboardRawMetric(outOfStock, outOfStock),
            new TenantAdminProductDashboardRawMetric(expiryAlerts, expiryAlerts),
            new TenantAdminProductDashboardRawMetric(
                (int)Math.Round(stockAddedCurrent, MidpointRounding.AwayFromZero),
                (int)Math.Round(stockAddedPrevious, MidpointRounding.AwayFromZero)),
            new TenantAdminProductDashboardRawMetric(fastMovingCurrent, fastMovingPrevious),
            currentStockValue,
            previousStockValue,
            stockValueTrend,
            stockMovements);
    }

    private static DateTimeOffset ToUtcStart(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

    private static DateTimeOffset ToUtcEnd(DateOnly date) =>
        new(date.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc));

    private async Task<List<Guid>> ResolveInventoryLocationIdsAsync(
        Guid tenantId,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.InventoryLocations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE");

        if (outletId.HasValue)
        {
            query = query.Where(x => x.OutletId == outletId.Value);
        }

        return await query.Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private async Task<int> CountActiveProductsAsync(
        Guid tenantId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status != ProductConstants.DeletedStatus &&
                x.CreatedAt <= asOf)
            .CountAsync(cancellationToken);
    }

    private async Task<int> CountLowStockAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return 0;
        }

        var balances = await (
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join rule in _dbContext.InventoryReorderRules.AsNoTracking()
                on new { balance.TenantId, balance.InventoryLocationId, balance.ProductId, balance.ProductVariantId }
                equals new
                {
                    rule.TenantId,
                    rule.InventoryLocationId,
                    rule.ProductId,
                    rule.ProductVariantId,
                }
                into rules
            from rule in rules.DefaultIfEmpty()
            where balance.TenantId == tenantId &&
                  locationIds.Contains(balance.InventoryLocationId) &&
                  balance.AvailableQuantity > 0
            select new
            {
                balance.AvailableQuantity,
                Threshold = rule != null
                    ? (rule.MinStockQuantity ?? rule.ReorderPointQuantity)
                    : (decimal?)null,
            })
            .ToListAsync(cancellationToken);

        return balances.Count(x =>
            x.AvailableQuantity <= (x.Threshold ?? ProductDashboardConstants.DefaultLowStockThreshold));
    }

    private async Task<int> CountOutOfStockAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return 0;
        }

        return await _dbContext.InventoryBalances
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                locationIds.Contains(x.InventoryLocationId) &&
                x.AvailableQuantity <= 0)
            .Select(x => new { x.ProductId, x.ProductVariantId })
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<int> CountExpiryAlertsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return 0;
        }

        var alertDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(ProductDashboardConstants.ExpiryAlertDays));

        return await (
            from batch in _dbContext.ProductBatches.AsNoTracking()
            join balance in _dbContext.InventoryBalances.AsNoTracking()
                on new { batch.TenantId, batch.Id }
                equals new { balance.TenantId, Id = balance.ProductBatchId!.Value }
            where batch.TenantId == tenantId &&
                  locationIds.Contains(balance.InventoryLocationId) &&
                  balance.AvailableQuantity > 0 &&
                  batch.ExpiryDate != null &&
                  batch.ExpiryDate <= alertDate
            select batch.Id)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<decimal> SumStockInQuantityAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        DateTimeOffset periodFrom,
        DateTimeOffset periodTo,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return 0;
        }

        return await (
            from movement in _dbContext.StockMovements.AsNoTracking()
            join balance in _dbContext.InventoryBalances.AsNoTracking()
                on new { movement.TenantId, movement.InventoryBalanceId }
                equals new { balance.TenantId, InventoryBalanceId = balance.Id }
            where movement.TenantId == tenantId &&
                  locationIds.Contains(balance.InventoryLocationId) &&
                  movement.OccurredAt >= periodFrom &&
                  movement.OccurredAt <= periodTo &&
                  movement.QuantityChange > 0 &&
                  StockMovementConstants.StockInAliases.Contains(movement.MovementType)
            select movement.QuantityChange)
            .SumAsync(cancellationToken);
    }

    private async Task<int> CountFastMovingProductsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        DateTimeOffset periodFrom,
        DateTimeOffset periodTo,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return 0;
        }

        var outletIds = await _dbContext.InventoryLocations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && locationIds.Contains(x.Id))
            .Select(x => x.OutletId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (outletIds.Count == 0)
        {
            return 0;
        }

        var rows = await (
            from line in _dbContext.SalesOrderLines.AsNoTracking()
            join order in _dbContext.SalesOrders.AsNoTracking()
                on new { line.TenantId, OrderId = line.SalesOrderId!.Value }
                equals new { order.TenantId, OrderId = order.Id }
            where line.TenantId == tenantId &&
                  line.SalesOrderId != null &&
                  order.FulfillmentMethodOutletId != null &&
                  outletIds.Contains(order.FulfillmentMethodOutletId.Value) &&
                  order.PlacedAt >= periodFrom &&
                  order.PlacedAt <= periodTo
            group line by line.ProductId into grouped
            select new
            {
                Quantity = grouped.Sum(x => x.Quantity),
            })
            .ToListAsync(cancellationToken);

        return rows.Count(x => x.Quantity >= ProductDashboardConstants.FastMovingSalesThreshold);
    }

    private async Task<decimal> SumStockValueAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return 0;
        }

        return await (
            from layer in _dbContext.InventoryCostLayers.AsNoTracking()
            join balance in _dbContext.InventoryBalances.AsNoTracking()
                on new { layer.TenantId, layer.InventoryBalanceId }
                equals new { balance.TenantId, InventoryBalanceId = balance.Id }
            where layer.TenantId == tenantId &&
                  locationIds.Contains(balance.InventoryLocationId) &&
                  layer.ReceivedAt <= asOf &&
                  layer.RemainingQuantity > 0
            select layer.RemainingQuantity * layer.UnitCost)
            .SumAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<TenantAdminProductDashboardRawStockValuePoint>> BuildStockValueTrendAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        DateOnly rangeFrom,
        DateOnly rangeTo,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return [];
        }

        var points = new List<TenantAdminProductDashboardRawStockValuePoint>();
        for (var date = rangeFrom; date <= rangeTo; date = date.AddDays(1))
        {
            var value = await SumStockValueAsync(
                tenantId,
                locationIds,
                ToUtcEnd(date),
                cancellationToken);
            points.Add(new TenantAdminProductDashboardRawStockValuePoint(date, value));
        }

        return points;
    }

    private async Task<IReadOnlyList<TenantAdminProductDashboardRawMovement>> CountStockMovementsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        DateTimeOffset periodFrom,
        DateTimeOffset periodTo,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return
            [
                new("stock_in", 0),
                new("stock_out", 0),
                new("adjustment", 0),
                new("transfer", 0),
            ];
        }

        var movements = await (
            from movement in _dbContext.StockMovements.AsNoTracking()
            join balance in _dbContext.InventoryBalances.AsNoTracking()
                on new { movement.TenantId, movement.InventoryBalanceId }
                equals new { balance.TenantId, InventoryBalanceId = balance.Id }
            where movement.TenantId == tenantId &&
                  locationIds.Contains(balance.InventoryLocationId) &&
                  movement.OccurredAt >= periodFrom &&
                  movement.OccurredAt <= periodTo
            select movement.MovementType)
            .ToListAsync(cancellationToken);

        var stockIn = movements.Count(x => StockMovementConstants.StockInAliases.Contains(x));
        var stockOut = movements.Count(x => StockMovementConstants.StockOutAliases.Contains(x));
        var adjustment = movements.Count(x => StockMovementConstants.AdjustmentAliases.Contains(x));
        var transfer = movements.Count(x => StockMovementConstants.TransferAliases.Contains(x));

        return
        [
            new("stock_in", stockIn),
            new("stock_out", stockOut),
            new("adjustment", adjustment),
            new("transfer", transfer),
        ];
    }
}
