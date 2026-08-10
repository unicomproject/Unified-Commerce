using E_POS.Application.Modules.Tenant.Inventory.Contracts.Dashboard;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.Dashboard;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.Dashboard;

internal sealed class DashboardRepository : IDashboardRepository
{
    private readonly EPosDbContext _dbContext;

    public DashboardRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardMetricsResponse> GetDashboardMetricsAsync(
        Guid tenantId,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        var balancesQuery = _dbContext.Set<InventoryBalance>()
            .Where(b => b.TenantId == tenantId);

        if (outletId.HasValue)
        {
            balancesQuery = balancesQuery.Where(b => b.InventoryLocationId == outletId.Value);
        }

        var outOfStockCount = await balancesQuery
            .Where(b => b.AvailableQuantity <= 0)
            .CountAsync(cancellationToken);

        // Join with Reorder Rules to find items below their reorder point
        var lowStockCount = await (
            from b in balancesQuery
            join r in _dbContext.Set<InventoryReorderRule>().Where(r => r.TenantId == tenantId)
              on new { b.InventoryLocationId, b.ProductId, b.ProductVariantId } equals new { r.InventoryLocationId, r.ProductId, r.ProductVariantId } into rules
            from rule in rules.DefaultIfEmpty()
            where b.AvailableQuantity > 0 && b.AvailableQuantity <= (rule == null ? 0 : rule.ReorderPointQuantity)
            select b
        ).CountAsync(cancellationToken);

        var next30Days = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Join with ProductBatch to filter items expiring in next 30 days
        var nearExpiryCount = await (
            from b in balancesQuery
            join pb in _dbContext.Set<ProductBatch>().Where(pb => pb.TenantId == tenantId)
              on b.ProductBatchId equals pb.Id
            where pb.ExpiryDate.HasValue && pb.ExpiryDate.Value >= today && pb.ExpiryDate.Value <= next30Days
            select b
        ).CountAsync(cancellationToken);

        var activeStockCounts = 0; // Mocked for now until Stocktake is implemented

        return new DashboardMetricsResponse(
            lowStockCount,
            outOfStockCount,
            nearExpiryCount,
            activeStockCounts);
    }

    public async Task<DashboardAlertsResponse> GetDashboardAlertsAsync(
        Guid tenantId,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var next30Days = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = from b in _dbContext.Set<InventoryBalance>().Where(b => b.TenantId == tenantId)
                    join l in _dbContext.Set<InventoryLocation>().Where(l => l.TenantId == tenantId)
                      on b.InventoryLocationId equals l.Id
                    join r in _dbContext.Set<InventoryReorderRule>().Where(r => r.TenantId == tenantId)
                      on new { b.InventoryLocationId, b.ProductId, b.ProductVariantId } equals new { r.InventoryLocationId, r.ProductId, r.ProductVariantId } into rules
                    from rule in rules.DefaultIfEmpty()
                    join pb in _dbContext.Set<ProductBatch>().Where(pb => pb.TenantId == tenantId)
                      on b.ProductBatchId equals pb.Id into batches
                    from batch in batches.DefaultIfEmpty()
                    where 
                        (!outletId.HasValue || b.InventoryLocationId == outletId.Value) &&
                        (
                            b.AvailableQuantity <= 0 || 
                            (b.AvailableQuantity > 0 && b.AvailableQuantity <= (rule == null ? 0 : rule.ReorderPointQuantity)) ||
                            (batch != null && batch.ExpiryDate.HasValue && batch.ExpiryDate.Value >= today && batch.ExpiryDate.Value <= next30Days)
                        )
                    select new 
                    {
                        b.ProductId,
                        b.ProductVariantId,
                        OutletId = l.Id,
                        OutletName = l.LocationName,
                        b.AvailableQuantity,
                        ReorderPointQuantity = rule == null ? 0 : rule.ReorderPointQuantity,
                        batch.ExpiryDate,
                        DetectedOn = b.UpdatedAt
                    };

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedData = await query
            .OrderBy(x => x.AvailableQuantity)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pagedData.Select(x => 
        {
            string alertType = "LowStock";
            string severity = "Warning";

            if (x.AvailableQuantity <= 0)
            {
                alertType = "OutOfStock";
                severity = "Critical";
            }
            else if (x.ExpiryDate.HasValue && x.ExpiryDate.Value <= next30Days)
            {
                alertType = "NearExpiry";
                severity = "Warning";
            }

            return new DashboardAlertItemResponse(
                x.ProductId,
                x.ProductVariantId,
                "Product Name Unknown",
                null, 
                null, 
                x.OutletId,
                x.OutletName,
                alertType,
                severity,
                x.DetectedOn ?? DateTimeOffset.UtcNow);
        }).ToList();

        return new DashboardAlertsResponse(items, page, pageSize, totalCount);
    }

    public async Task<DashboardActivitiesResponse> GetDashboardActivitiesAsync(
        Guid tenantId,
        Guid? outletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var movementsQuery = _dbContext.Set<StockMovement>()
            .Where(m => m.TenantId == tenantId);

        if (outletId.HasValue)
        {
            var balances = _dbContext.Set<InventoryBalance>()
                .Where(b => b.TenantId == tenantId && b.InventoryLocationId == outletId.Value)
                .Select(b => b.Id);
                
            movementsQuery = movementsQuery.Where(m => balances.Contains(m.InventoryBalanceId));
        }

        var totalCount = await movementsQuery.CountAsync(cancellationToken);

        var query = from m in movementsQuery
                    join b in _dbContext.Set<InventoryBalance>().Where(b => b.TenantId == tenantId)
                        on m.InventoryBalanceId equals b.Id
                    join l in _dbContext.Set<InventoryLocation>().Where(l => l.TenantId == tenantId)
                        on b.InventoryLocationId equals l.Id
                    orderby m.OccurredAt descending
                    select new DashboardActivityItemResponse(
                        m.Id,
                        m.MovementType,
                        m.ReferenceNumberSnapshot,
                        l.Id,
                        l.LocationName,
                        m.OccurredAt,
                        m.QuantityChange);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new DashboardActivitiesResponse(items, page, pageSize, totalCount);
    }

    public async Task<bool> UserHasOutletAccessAsync(
        Guid tenantId,
        Guid userId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<InventoryLocation>()
            .AnyAsync(l => l.TenantId == tenantId && l.Id == outletId, cancellationToken);
    }
}
