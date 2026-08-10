using E_POS.Application.Modules.Tenant.Inventory.Contracts.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.StockIn;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using E_POS.Application.Modules.Shared.Media.Contracts;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.CurrentStock;

internal sealed class CurrentStockRepository : ICurrentStockRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly IMediaReadUrlResolver? _mediaReadUrlResolver;

    public CurrentStockRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
    {
        _dbContext = dbContext;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    public async Task<CurrentStockSummaryResponse> GetCurrentStockSummaryAsync(
        Guid tenantId,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        var balancesQuery = _dbContext.Set<InventoryBalance>()
            .Where(b => b.TenantId == tenantId);

        if (outletId.HasValue)
        {
            var locationIds = _dbContext.Set<InventoryLocation>()
                .Where(l => l.TenantId == tenantId && l.OutletId == outletId.Value)
                .Select(l => l.Id);
            balancesQuery = balancesQuery.Where(b => locationIds.Contains(b.InventoryLocationId));
        }

        var stats = await (
            from b in balancesQuery
            join r in _dbContext.Set<InventoryReorderRule>().Where(r => r.TenantId == tenantId)
              on new { b.InventoryLocationId, b.ProductId, b.ProductVariantId } equals new { r.InventoryLocationId, r.ProductId, r.ProductVariantId } into rules
            from rule in rules.DefaultIfEmpty()
            group new { b, rule } by 1 into g
            select new 
            {
                TotalItems = g.Count(),
                LowStock = g.Count(x => x.b.AvailableQuantity > 0 && x.b.AvailableQuantity <= (x.rule == null ? 0 : x.rule.ReorderPointQuantity)),
                OutOfStock = g.Count(x => x.b.AvailableQuantity <= 0)
            }
        ).FirstOrDefaultAsync(cancellationToken);

        return new CurrentStockSummaryResponse(
            stats?.TotalItems ?? 0,
            stats?.LowStock ?? 0,
            stats?.OutOfStock ?? 0,
            0m // TotalInventoryValue requires joining with Product cost
        );
    }

    public async Task<CurrentStockListResponse> GetCurrentStockAsync(
        Guid tenantId,
        CurrentStockQuery query,
        CancellationToken cancellationToken)
    {
        var balancesQuery = _dbContext.Set<InventoryBalance>()
            .Where(b => b.TenantId == tenantId);

        if (query.OutletId.HasValue)
        {
            var locationIds = _dbContext.Set<InventoryLocation>()
                .Where(l => l.TenantId == tenantId && l.OutletId == query.OutletId.Value)
                .Select(l => l.Id);
            balancesQuery = balancesQuery.Where(b => locationIds.Contains(b.InventoryLocationId));
        }

        var dataQuery = from b in balancesQuery
                        join p in _dbContext.Set<Product>().Where(p => p.TenantId == tenantId)
                          on b.ProductId equals p.Id
                        join pv in _dbContext.Set<ProductVariant>().Where(pv => pv.TenantId == tenantId)
                          on b.ProductVariantId equals pv.Id into variants
                        from variant in variants.DefaultIfEmpty()
                        join r in _dbContext.Set<InventoryReorderRule>().Where(r => r.TenantId == tenantId)
                          on new { b.InventoryLocationId, b.ProductId, b.ProductVariantId } equals new { r.InventoryLocationId, r.ProductId, r.ProductVariantId } into rules
                        from rule in rules.DefaultIfEmpty()
                        select new { b, rule, p, variant };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.ToLower();
            dataQuery = dataQuery.Where(x => 
                x.p.ProductName.ToLower().Contains(searchTerm) || 
                (x.variant != null && x.variant.Sku != null && x.variant.Sku.ToLower().Contains(searchTerm)) ||
                _dbContext.Set<ProductBarcode>().Any(pb => pb.TenantId == tenantId && pb.ProductId == x.p.Id && (pb.ProductVariantId == null || x.variant == null || pb.ProductVariantId == x.variant.Id) && pb.Barcode.ToLower().Contains(searchTerm)));
        }

        var totalCount = await dataQuery.CountAsync(cancellationToken);

        var pagedData = await dataQuery
            .OrderByDescending(x => x.b.AvailableQuantity)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var productIds = pagedData.Select(x => x.p.Id).Distinct().ToList();
        var imageRows = await (from image in _dbContext.Set<ProductImage>().AsNoTracking()
                               join media in _dbContext.Set<MediaAsset>().AsNoTracking()
                                 on new { image.TenantId, MediaAssetId = image.MediaAssetId } equals new { media.TenantId, MediaAssetId = (Guid?)media.Id }
                               where image.TenantId == tenantId && productIds.Contains(image.ProductId) && image.IsPrimaryImage && image.Status == "ACTIVE"
                               select new { image.ProductId, media.PublicUrl })
                               .ToListAsync(cancellationToken);
                               
        var imageDict = imageRows.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.First().PublicUrl);

        var items = pagedData.Select(x => new CurrentStockListItemResponse(
            x.b.Id,
            x.b.InventoryLocationId,
            x.b.ProductId,
            x.p.ProductName,
            x.b.ProductVariantId,
            x.variant?.VariantName,
            Array.Empty<CurrentStockVariantOptionResponse>(),
            x.variant?.Sku,
            null, // Barcode
            x.b.ProductBatchId,
            null, // BatchNumber
            null, // ExpiryDate
            x.b.OnHandQuantity,
            x.b.ReservedQuantity,
            x.b.DamagedQuantity,
            x.b.QuarantineQuantity,
            x.b.AvailableQuantity,
            x.b.AvailableQuantity <= 0 ? "OutOfStock" : 
            (x.rule != null && x.b.AvailableQuantity <= x.rule.ReorderPointQuantity ? "LowStock" : "InStock"),
            "Normal", // ExpiryStatus
            x.rule?.ReorderPointQuantity,
            imageDict.GetValueOrDefault(x.p.Id), // ImageUrl
            x.b.UpdatedAt,
            x.b.RowVersion
        )).ToList();

        return new CurrentStockListResponse(items, query.Page, query.PageSize, totalCount);
    }

    public Task<StockInResponse> StockInAsync(
        Guid tenantId,
        Guid userId,
        StockInRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new StockInResponse(Guid.NewGuid(), request.OutletId, "StockIn", request.ReferenceNumber, [], now));
    }

    public async Task<bool> OutletExistsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<InventoryLocation>()
            .AnyAsync(l => l.TenantId == tenantId && l.Id == outletId, cancellationToken);
    }

    public async Task<bool> IdempotencyKeyExistsAsync(
        Guid tenantId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(idempotencyKey)) return false;
        
        return await _dbContext.Set<StockMovement>()
            .AnyAsync(sm => sm.TenantId == tenantId && sm.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<ProductStockDetailResponse?> GetProductStockDetailAsync(
        Guid tenantId,
        Guid productVariantId,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        var variant = await _dbContext.Set<ProductVariant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == productVariantId && v.TenantId == tenantId, cancellationToken);
            
        if (variant == null) return null;
        
        var product = await _dbContext.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == variant.ProductId, cancellationToken);
            
        if (product == null) return null;

        var balancesQuery = _dbContext.Set<InventoryBalance>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.ProductVariantId == productVariantId);

        if (outletId.HasValue)
        {
            var locationIds = _dbContext.Set<InventoryLocation>()
                .Where(l => l.TenantId == tenantId && l.OutletId == outletId.Value)
                .Select(l => l.Id);
            balancesQuery = balancesQuery.Where(b => locationIds.Contains(b.InventoryLocationId));
        }

        var balances = await (from b in balancesQuery
                              join l in _dbContext.Set<InventoryLocation>() on b.InventoryLocationId equals l.Id
                              join r in _dbContext.Set<InventoryReorderRule>().Where(x => x.TenantId == tenantId && x.ProductVariantId == productVariantId) 
                                on b.InventoryLocationId equals r.InventoryLocationId into rules
                              from rule in rules.DefaultIfEmpty()
                              select new { b, l, rule }).ToListAsync(cancellationToken);

        var locationBalances = balances.Select(x => new LocationBalanceDto(
            x.l.Id,
            x.l.LocationName,
            x.b.OnHandQuantity,
            x.b.ReservedQuantity,
            x.b.AvailableQuantity,
            x.rule != null ? x.rule.ReorderPointQuantity : 0
        )).ToList();

        var totalOnHand = locationBalances.Sum(x => x.OnHand);
        var totalReserved = locationBalances.Sum(x => x.Reserved);
        var totalAvailable = locationBalances.Sum(x => x.Available);
        var totalReorderLevel = locationBalances.Sum(x => x.ReorderLevel);

        var imageRow = await (from image in _dbContext.Set<ProductImage>().AsNoTracking()
                              join media in _dbContext.Set<MediaAsset>().AsNoTracking()
                                on new { image.TenantId, MediaAssetId = image.MediaAssetId } equals new { media.TenantId, MediaAssetId = (Guid?)media.Id }
                              where image.TenantId == tenantId && image.ProductId == product.Id && image.IsPrimaryImage && image.Status == "ACTIVE"
                              select media.PublicUrl).FirstOrDefaultAsync(cancellationToken);

        string? imageUrl = imageRow;
        if (!string.IsNullOrEmpty(imageUrl) && _mediaReadUrlResolver != null)
        {
             imageUrl = _mediaReadUrlResolver.ResolveReadUrl(imageUrl);
        }

        var categoryName = await (from pc in _dbContext.Set<ProductCategory>().AsNoTracking()
                                  join c in _dbContext.Set<Category>().AsNoTracking() on pc.CategoryId equals c.Id
                                  where pc.TenantId == tenantId && pc.ProductId == product.Id
                                  orderby pc.IsPrimaryCategory descending, pc.SortOrder ascending
                                  select c.CategoryName).FirstOrDefaultAsync(cancellationToken);

        return new ProductStockDetailResponse(
            product.Id,
            product.ProductName,
            variant.Id,
            variant.VariantName,
            variant.Sku,
            categoryName,
            product.Status,
            totalAvailable > 0 ? "In Stock" : "Out of Stock",
            false, // Batch tracking not enabled yet
            imageUrl,
            totalOnHand,
            totalReserved,
            totalAvailable,
            totalReorderLevel,
            locationBalances
        );
    }

    public async Task<StockMovementHistoryListResponse> GetStockMovementHistoryAsync(
        Guid tenantId,
        StockMovementHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var movementsQuery = from sm in _dbContext.Set<StockMovement>().Where(x => x.TenantId == tenantId)
                             join ib in _dbContext.Set<InventoryBalance>().Where(x => x.ProductVariantId == query.ProductVariantId)
                               on sm.InventoryBalanceId equals ib.Id
                             join il in _dbContext.Set<InventoryLocation>()
                               on ib.InventoryLocationId equals il.Id
                             select new { sm, il };

        if (query.OutletId.HasValue)
        {
            movementsQuery = movementsQuery.Where(x => x.il.OutletId == query.OutletId.Value);
        }

        var totalCount = await movementsQuery.CountAsync(cancellationToken);
        
        var data = await movementsQuery
            .OrderByDescending(x => x.sm.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new StockMovementHistoryDto(
                x.sm.Id,
                x.sm.MovementType,
                x.sm.ReferenceNumberSnapshot,
                x.il.LocationName,
                x.sm.OccurredAt,
                x.sm.QuantityChange
            ))
            .ToListAsync(cancellationToken);

        return new StockMovementHistoryListResponse(data, query.Page, query.PageSize, totalCount);
    }
}
