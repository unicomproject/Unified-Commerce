using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.Inventory.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories;

public sealed class TenantAdminInventoryRepository : ITenantAdminInventoryRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly ICodeSequenceRepository _codeSequenceRepository;

    public TenantAdminInventoryRepository(
        EPosDbContext dbContext,
        ICodeSequenceRepository codeSequenceRepository)
    {
        _dbContext = dbContext;
        _codeSequenceRepository = codeSequenceRepository;
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleOutletIdsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var scopedOutletIds = await _dbContext.OutletUserRoles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == userId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (scopedOutletIds.Count > 0)
        {
            return await _dbContext.Outlets
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    scopedOutletIds.Contains(x.Id) &&
                    x.Status == OutletConstants.ActiveStatus)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        return await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == OutletConstants.ActiveStatus)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<TenantAdminCurrentStockListResponse> GetCurrentStockAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminCurrentStockQuery query,
        CancellationToken cancellationToken)
    {
        var accessibleOutletIds = await GetAccessibleOutletIdsAsync(tenantId, userId, cancellationToken);
        if (accessibleOutletIds.Count == 0)
        {
            return new TenantAdminCurrentStockListResponse([], query.Page, query.PageSize, 0);
        }

        if (query.OutletId.HasValue && !accessibleOutletIds.Contains(query.OutletId.Value))
        {
            return new TenantAdminCurrentStockListResponse([], query.Page, query.PageSize, 0);
        }

        var locationIds = await ResolveInventoryLocationIdsAsync(
            tenantId,
            query.OutletId,
            accessibleOutletIds,
            cancellationToken);

        if (locationIds.Count == 0)
        {
            return new TenantAdminCurrentStockListResponse([], query.Page, query.PageSize, 0);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var alertDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(ProductDashboardConstants.ExpiryAlertDays));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var balancesQuery = BuildCurrentStockQuery(tenantId, locationIds, query, today, alertDate);
        var totalCount = await balancesQuery.CountAsync(cancellationToken);

        balancesQuery = ApplySorting(balancesQuery, query.SortBy, query.SortDirection);
        var pageRows = await balancesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var balanceIds = pageRows.Select(x => x.BalanceId).ToList();
        var lastMovements = await LoadLastMovementsAsync(tenantId, balanceIds, cancellationToken);
        var variantOptions = await LoadVariantOptionsAsync(tenantId, pageRows, cancellationToken);
        var barcodes = await LoadPrimaryBarcodesAsync(
            tenantId,
            pageRows.Where(x => x.VariantId.HasValue).Select(x => x.VariantId!.Value).Distinct().ToList(),
            cancellationToken);

        var items = pageRows
            .Select(row =>
            {
                lastMovements.TryGetValue(row.BalanceId, out var lastMovementAt);
                IReadOnlyList<TenantAdminCurrentStockVariantOptionResponse> options =
                    row.VariantId.HasValue && variantOptions.TryGetValue(row.VariantId.Value, out var optionValues)
                        ? optionValues
                        : [];

                var barcode = row.VariantId.HasValue && barcodes.TryGetValue(row.VariantId.Value, out var code)
                    ? code
                    : null;

                return new TenantAdminCurrentStockListItemResponse(
                    row.BalanceId,
                    row.InventoryLocationId,
                    row.OutletId,
                    row.OutletName,
                    row.ProductId,
                    row.ProductName,
                    row.VariantId,
                    row.VariantName,
                    options,
                    row.Sku,
                    barcode,
                    row.BatchId,
                    row.BatchNumber,
                    row.ExpiryDate,
                    row.OnHandQuantity,
                    row.ReservedQuantity,
                    row.DamagedQuantity,
                    row.QuarantineQuantity,
                    row.AvailableQuantity,
                    row.StockStatus,
                    row.ExpiryStatus,
                    lastMovementAt,
                    row.RowVersion);
            })
            .ToList();

        return new TenantAdminCurrentStockListResponse(items, page, pageSize, totalCount);
    }

    public async Task<TenantAdminCurrentStockSummaryResponse> GetCurrentStockSummaryAsync(
        Guid tenantId,
        Guid userId,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        var accessibleOutletIds = await GetAccessibleOutletIdsAsync(tenantId, userId, cancellationToken);
        if (accessibleOutletIds.Count == 0)
        {
            return new TenantAdminCurrentStockSummaryResponse(0, 0, 0, 0, 0, 0);
        }

        if (outletId.HasValue && !accessibleOutletIds.Contains(outletId.Value))
        {
            return new TenantAdminCurrentStockSummaryResponse(0, 0, 0, 0, 0, 0);
        }

        var locationIds = await ResolveInventoryLocationIdsAsync(
            tenantId,
            outletId,
            accessibleOutletIds,
            cancellationToken);

        if (locationIds.Count == 0)
        {
            return new TenantAdminCurrentStockSummaryResponse(0, 0, 0, 0, 0, 0);
        }

        var alertDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(ProductDashboardConstants.ExpiryAlertDays));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new TenantAdminCurrentStockQuery(
            outletId,
            null,
            TenantAdminInventoryConstants.StockStatusFilterAll,
            null,
            null,
            TenantAdminInventoryConstants.ExpiryFilterAll,
            1,
            1,
            null,
            null);

        var rows = await BuildCurrentStockQuery(tenantId, locationIds, query, today, alertDate)
            .ToListAsync(cancellationToken);

        return new TenantAdminCurrentStockSummaryResponse(
            rows.Select(x => x.ProductId).Distinct().Count(),
            rows.Where(x => x.VariantId.HasValue).Select(x => x.VariantId!.Value).Distinct().Count(),
            rows.Sum(x => x.OnHandQuantity),
            rows.Count(x => x.StockStatus == TenantAdminInventoryConstants.StockStatusLowStock),
            rows.Count(x => x.StockStatus == TenantAdminInventoryConstants.StockStatusOutOfStock),
            rows.Count(x => x.ExpiryStatus == TenantAdminInventoryConstants.ExpiryStatusExpiringSoon));
    }

    public async Task<TenantAdminStockInResponse> StockInAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminStockInRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var location = await ResolveReceivingLocationAsync(tenantId, request.OutletId, cancellationToken);
        if (location is null)
        {
            throw new InventoryOperationException(new ApplicationError(
                "inventory.location_not_found",
                "Inventory location was not found for the selected outlet."));
        }

        var outletName = await GetOutletNameAsync(tenantId, request.OutletId, cancellationToken) ?? string.Empty;
        var receivedAt = request.ReceivedAt ?? now;
        var operationId = Guid.NewGuid();
        var movementNote = BuildMovementNote(request.ReferenceNumber, request.Notes);
        var lines = new List<TenantAdminStockInLineResponse>();

        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            for (var index = 0; index < request.Items.Count; index++)
            {
                var line = request.Items[index];
                var profile = await GetVariantTrackingProfileAsync(tenantId, line.ProductVariantId, cancellationToken)
                    ?? throw new InventoryOperationException(new ApplicationError(
                        "inventory.not_found",
                        "Inventory resource was not found."));

                var batchId = await ResolveOrCreateBatchAsync(
                    tenantId,
                    profile.ProductId,
                    profile.VariantId,
                    line.BatchNumber,
                    line.ManufacturedDate,
                    line.ExpiryDate,
                    userId,
                    receivedAt,
                    now,
                    cancellationToken);

                var balance = await ResolveOrCreateBalanceAsync(
                    tenantId,
                    location.Id,
                    profile.ProductId,
                    profile.VariantId,
                    batchId,
                    now,
                    cancellationToken);

                var quantityBefore = balance.OnHandQuantity;
                var movementNumber = await _codeSequenceRepository.GetNextCodeAsync(
                    tenantId,
                    TenantAdminInventoryConstants.StockMovementSequenceKey,
                    TenantAdminInventoryConstants.StockMovementPrefix,
                    TenantAdminInventoryConstants.StockMovementPadding,
                    now,
                    cancellationToken);

                var totalCost = line.UnitCost.HasValue ? line.UnitCost.Value * line.Quantity : (decimal?)null;
                var movementId = Guid.NewGuid();
                var idempotencyKey = index == 0 && !string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    ? request.IdempotencyKey.Trim()
                    : null;

                var movement = StockMovement.Create(
                    movementId,
                    tenantId,
                    movementNumber,
                    balance.Id,
                    StockMovementConstants.StockIn,
                    quantityBefore,
                    line.Quantity,
                    line.UnitCost,
                    totalCost,
                    idempotencyKey,
                    movementNote,
                    receivedAt,
                    userId,
                    now);

                balance.AdjustQuantities(line.Quantity, 0, 0, 0, now);

                await _dbContext.StockMovements.AddAsync(movement, cancellationToken);

                var reference = StockMovementReference.Create(
                    Guid.NewGuid(),
                    tenantId,
                    movementId,
                    TenantAdminInventoryConstants.StockReceiptReferenceType,
                    operationId,
                    movementId,
                    now);

                await _dbContext.StockMovementReferences.AddAsync(reference, cancellationToken);

                if (line.UnitCost.HasValue && line.UnitCost.Value > 0)
                {
                    var costLayer = InventoryCostLayer.Create(
                        Guid.NewGuid(),
                        tenantId,
                        balance.Id,
                        movementId,
                        line.Quantity,
                        line.UnitCost.Value,
                        receivedAt,
                        TenantAdminInventoryConstants.CostLayerActiveStatus,
                        now);

                    await _dbContext.InventoryCostLayers.AddAsync(costLayer, cancellationToken);
                }

                lines.Add(new TenantAdminStockInLineResponse(
                    line.ProductVariantId,
                    profile.VariantName,
                    batchId,
                    line.BatchNumber?.Trim(),
                    line.Quantity,
                    balance.OnHandQuantity,
                    balance.AvailableQuantity,
                    movementId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw new InventoryOperationException(new ApplicationError(
                "inventory.concurrency_conflict",
                "Inventory was updated by another process. Please retry."));
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return new TenantAdminStockInResponse(
            operationId,
            request.OutletId,
            outletName,
            request.ReferenceNumber?.Trim(),
            receivedAt,
            lines.Count,
            lines.Sum(x => x.QuantityReceived),
            TenantAdminInventoryConstants.StockInCompletedStatus,
            lines,
            now);
    }

    public async Task<TenantAdminInventoryVariantLookupResponse?> GetProductVariantsForStockInAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .OrderByDescending(x => x.IsDefaultVariant)
            .ThenBy(x => x.VariantName)
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            return null;
        }

        var barcodes = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductConstants.ActiveStatus)
            .ToListAsync(cancellationToken);

        var optionValues = await (
            from link in _dbContext.ProductVariantOptionValues.AsNoTracking()
            join option in _dbContext.ProductOptions.AsNoTracking()
                on link.ProductOptionId equals option.Id
            join value in _dbContext.ProductOptionValues.AsNoTracking()
                on link.ProductOptionValueId equals value.Id
            where link.TenantId == tenantId && link.ProductId == productId
            orderby option.SortOrder, value.SortOrder
            select new
            {
                link.ProductVariantId,
                option.OptionName,
                Value = value.DisplayName ?? value.ValueName,
            })
            .ToListAsync(cancellationToken);

        var items = new List<TenantAdminInventoryVariantLookupItemResponse>();
        var productBatchTracked = false;
        var productExpiryTracked = false;

        foreach (var variant in variants)
        {
            var profile = await ResolveTrackingProfileAsync(tenantId, variant, cancellationToken);
            productBatchTracked |= profile.RequiresBatchTracking;
            productExpiryTracked |= profile.RequiresExpiryTracking;

            var barcode = barcodes
                .Where(x => x.ProductVariantId == variant.Id || x.ProductVariantId == null)
                .OrderByDescending(x => x.ProductVariantId == variant.Id)
                .ThenByDescending(x => x.IsPrimaryBarcode)
                .Select(x => x.Barcode)
                .FirstOrDefault();

            var options = optionValues
                .Where(x => x.ProductVariantId == variant.Id)
                .Select(x => new TenantAdminInventoryVariantOptionValueResponse(x.OptionName, x.Value))
                .ToList();

            items.Add(new TenantAdminInventoryVariantLookupItemResponse(
                variant.Id,
                variant.VariantName,
                variant.Sku,
                barcode,
                variant.Status,
                profile.RequiresBatchTracking,
                profile.RequiresExpiryTracking,
                options));
        }

        return new TenantAdminInventoryVariantLookupResponse(
            product.Id,
            product.ProductName,
            productBatchTracked,
            productExpiryTracked,
            items);
    }

    public Task<bool> OutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
        _dbContext.Outlets.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.Id == outletId &&
                 x.Status == OutletConstants.ActiveStatus,
            cancellationToken);

    public async Task<bool> UserHasOutletAccessAsync(
        Guid tenantId,
        Guid userId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var accessibleOutletIds = await GetAccessibleOutletIdsAsync(tenantId, userId, cancellationToken);
        return accessibleOutletIds.Contains(outletId);
    }

    public Task<bool> StockInIdempotencyKeyExistsAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _dbContext.StockMovements.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public async Task<TenantAdminInventoryTrackingProfile?> GetVariantTrackingProfileAsync(
        Guid tenantId,
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var variant = await _dbContext.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == variantId &&
                     x.Status == ProductConstants.ActiveStatus,
                cancellationToken);

        if (variant is null)
        {
            return null;
        }

        var profile = await ResolveTrackingProfileAsync(tenantId, variant, cancellationToken);
        return new TenantAdminInventoryTrackingProfile(
            variant.ProductId,
            variant.Id,
            variant.VariantName,
            profile.IsStockTracked,
            profile.RequiresBatchTracking,
            profile.RequiresExpiryTracking);
    }

    public async Task<string?> GetOutletNameAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken) =>
        await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == outletId)
            .Select(x => x.OutletName)
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<CurrentStockProjection> BuildCurrentStockQuery(
        Guid tenantId,
        IReadOnlyCollection<Guid> locationIds,
        TenantAdminCurrentStockQuery query,
        DateOnly today,
        DateOnly alertDate)
    {
        var defaultThreshold = (decimal)ProductDashboardConstants.DefaultLowStockThreshold;

        var balancesQuery =
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking()
                on balance.InventoryLocationId equals location.Id
            join outlet in _dbContext.Outlets.AsNoTracking()
                on location.OutletId equals outlet.Id
            join product in _dbContext.Products.AsNoTracking()
                on balance.ProductId equals product.Id
            join variant in _dbContext.ProductVariants.AsNoTracking()
                on balance.ProductVariantId equals variant.Id into variants
            from variant in variants.DefaultIfEmpty()
            join batch in _dbContext.ProductBatches.AsNoTracking()
                on balance.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            join categoryLink in _dbContext.ProductCategories.AsNoTracking()
                on new { product.TenantId, ProductId = product.Id }
                equals new { categoryLink.TenantId, categoryLink.ProductId }
                into categoryLinks
            from categoryLink in categoryLinks
                .Where(x => x.IsPrimaryCategory)
                .DefaultIfEmpty()
            join rule in _dbContext.InventoryReorderRules.AsNoTracking()
                on new
                {
                    balance.TenantId,
                    balance.InventoryLocationId,
                    balance.ProductId,
                    balance.ProductVariantId,
                }
                equals new
                {
                    rule.TenantId,
                    rule.InventoryLocationId,
                    rule.ProductId,
                    rule.ProductVariantId,
                }
                into rules
            from rule in rules.Where(x => x.Status == TenantAdminInventoryConstants.ActiveStatus).DefaultIfEmpty()
            where balance.TenantId == tenantId &&
                  locationIds.Contains(balance.InventoryLocationId) &&
                  product.Status != ProductConstants.DeletedStatus
            let threshold = rule != null
                ? (rule.MinStockQuantity ?? rule.ReorderPointQuantity)
                : defaultThreshold
            let stockStatus =
                balance.AvailableQuantity <= 0
                    ? TenantAdminInventoryConstants.StockStatusOutOfStock
                    : balance.AvailableQuantity <= threshold
                        ? TenantAdminInventoryConstants.StockStatusLowStock
                        : TenantAdminInventoryConstants.StockStatusInStock
            let expiryStatus =
                batch == null || batch.ExpiryDate == null
                    ? TenantAdminInventoryConstants.ExpiryStatusNotApplicable
                    : batch.ExpiryDate < today
                        ? TenantAdminInventoryConstants.ExpiryStatusExpired
                        : batch.ExpiryDate <= alertDate
                            ? TenantAdminInventoryConstants.ExpiryStatusExpiringSoon
                            : TenantAdminInventoryConstants.ExpiryStatusValid
            select new CurrentStockProjection
            {
                BalanceId = balance.Id,
                InventoryLocationId = balance.InventoryLocationId,
                ProductId = product.Id,
                ProductName = product.ProductName,
                VariantId = variant != null ? variant.Id : (Guid?)null,
                VariantName = variant != null ? variant.VariantName : null,
                Sku = variant != null ? variant.Sku : null,
                OutletId = outlet.Id,
                OutletName = outlet.OutletName,
                BatchId = batch != null ? batch.Id : (Guid?)null,
                BatchNumber = batch != null ? batch.BatchNumber : null,
                ExpiryDate = batch != null ? batch.ExpiryDate : null,
                OnHandQuantity = balance.OnHandQuantity,
                ReservedQuantity = balance.ReservedQuantity,
                DamagedQuantity = balance.DamagedQuantity,
                QuarantineQuantity = balance.QuarantineQuantity,
                AvailableQuantity = balance.AvailableQuantity,
                StockStatus = stockStatus,
                ExpiryStatus = expiryStatus,
                RowVersion = balance.RowVersion,
            };

        if (query.CategoryId.HasValue)
        {
            balancesQuery = balancesQuery.Where(x =>
                _dbContext.ProductCategories.Any(pc =>
                    pc.TenantId == tenantId &&
                    pc.ProductId == x.ProductId &&
                    pc.CategoryId == query.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.BatchNumber))
        {
            var batchTerm = query.BatchNumber.Trim();
            balancesQuery = balancesQuery.Where(x =>
                x.BatchNumber != null && x.BatchNumber.Contains(batchTerm));
        }

        if (!string.IsNullOrWhiteSpace(query.ExpiryStatus) &&
            !string.Equals(query.ExpiryStatus, TenantAdminInventoryConstants.ExpiryFilterAll, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(query.ExpiryStatus, TenantAdminInventoryConstants.ExpiryFilterExpired, StringComparison.OrdinalIgnoreCase))
            {
                balancesQuery = balancesQuery.Where(x =>
                    x.ExpiryStatus == TenantAdminInventoryConstants.ExpiryStatusExpired);
            }
            else if (string.Equals(query.ExpiryStatus, TenantAdminInventoryConstants.ExpiryFilterExpiring, StringComparison.OrdinalIgnoreCase))
            {
                balancesQuery = balancesQuery.Where(x =>
                    x.ExpiryStatus == TenantAdminInventoryConstants.ExpiryStatusExpiringSoon);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.StockStatus) &&
            !string.Equals(query.StockStatus, TenantAdminInventoryConstants.StockStatusFilterAll, StringComparison.OrdinalIgnoreCase))
        {
            var statusFilter = query.StockStatus.Trim().ToUpperInvariant();
            balancesQuery = balancesQuery.Where(x => x.StockStatus == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim();
            balancesQuery = balancesQuery.Where(x =>
                x.ProductName.Contains(searchTerm) ||
                (x.Sku != null && x.Sku.Contains(searchTerm)) ||
                (x.BatchNumber != null && x.BatchNumber.Contains(searchTerm)) ||
                _dbContext.ProductBarcodes.Any(barcode =>
                    barcode.TenantId == tenantId &&
                    barcode.ProductId == x.ProductId &&
                    (barcode.ProductVariantId == null || barcode.ProductVariantId == x.VariantId) &&
                    barcode.Barcode.Contains(searchTerm)));
        }

        return balancesQuery;
    }

    private static IQueryable<CurrentStockProjection> ApplySorting(
        IQueryable<CurrentStockProjection> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = string.IsNullOrWhiteSpace(sortBy) ? "productName" : sortBy.Trim().ToLowerInvariant();

        return field switch
        {
            "onhandquantity" => descending
                ? query.OrderByDescending(x => x.OnHandQuantity)
                : query.OrderBy(x => x.OnHandQuantity),
            "availablequantity" => descending
                ? query.OrderByDescending(x => x.AvailableQuantity)
                : query.OrderBy(x => x.AvailableQuantity),
            "expirydate" => descending
                ? query.OrderByDescending(x => x.ExpiryDate)
                : query.OrderBy(x => x.ExpiryDate),
            _ => descending
                ? query.OrderByDescending(x => x.ProductName)
                : query.OrderBy(x => x.ProductName),
        };
    }

    private async Task<Dictionary<Guid, DateTimeOffset>> LoadLastMovementsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> balanceIds,
        CancellationToken cancellationToken)
    {
        if (balanceIds.Count == 0)
        {
            return new Dictionary<Guid, DateTimeOffset>();
        }

        return await (
            from movement in _dbContext.StockMovements.AsNoTracking()
            where movement.TenantId == tenantId && balanceIds.Contains(movement.InventoryBalanceId)
            group movement by movement.InventoryBalanceId
            into grouped
            select new
            {
                BalanceId = grouped.Key,
                LastMovementAt = grouped.Max(x => x.OccurredAt),
            })
            .ToDictionaryAsync(x => x.BalanceId, x => x.LastMovementAt, cancellationToken);
    }

    private async Task<Dictionary<Guid, List<TenantAdminCurrentStockVariantOptionResponse>>> LoadVariantOptionsAsync(
        Guid tenantId,
        IReadOnlyCollection<CurrentStockProjection> rows,
        CancellationToken cancellationToken)
    {
        var variantIds = rows
            .Where(x => x.VariantId.HasValue)
            .Select(x => x.VariantId!.Value)
            .Distinct()
            .ToList();

        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, List<TenantAdminCurrentStockVariantOptionResponse>>();
        }

        var optionRows = await (
            from link in _dbContext.ProductVariantOptionValues.AsNoTracking()
            join option in _dbContext.ProductOptions.AsNoTracking()
                on link.ProductOptionId equals option.Id
            join value in _dbContext.ProductOptionValues.AsNoTracking()
                on link.ProductOptionValueId equals value.Id
            where link.TenantId == tenantId && variantIds.Contains(link.ProductVariantId)
            orderby option.SortOrder, value.SortOrder
            select new
            {
                link.ProductVariantId,
                option.OptionName,
                Value = value.DisplayName ?? value.ValueName,
            })
            .ToListAsync(cancellationToken);

        return optionRows
            .GroupBy(x => x.ProductVariantId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => new TenantAdminCurrentStockVariantOptionResponse(x.OptionName, x.Value))
                    .ToList());
    }

    private async Task<Dictionary<Guid, string>> LoadPrimaryBarcodesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var barcodes = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductVariantId != null &&
                variantIds.Contains(x.ProductVariantId.Value) &&
                x.Status == ProductConstants.ActiveStatus)
            .OrderByDescending(x => x.IsPrimaryBarcode)
            .ToListAsync(cancellationToken);

        return barcodes
            .GroupBy(x => x.ProductVariantId!.Value)
            .ToDictionary(group => group.Key, group => group.First().Barcode);
    }

    private async Task<List<Guid>> ResolveInventoryLocationIdsAsync(
        Guid tenantId,
        Guid? outletId,
        IReadOnlyCollection<Guid> accessibleOutletIds,
        CancellationToken cancellationToken)
    {
        var outletFilter = outletId.HasValue
            ? accessibleOutletIds.Where(x => x == outletId.Value).ToList()
            : accessibleOutletIds.ToList();

        if (outletFilter.Count == 0)
        {
            return [];
        }

        return await _dbContext.InventoryLocations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                outletFilter.Contains(x.OutletId) &&
                x.Status == TenantAdminInventoryConstants.ActiveStatus)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<InventoryLocation?> ResolveReceivingLocationAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken) =>
        await _dbContext.InventoryLocations
            .Where(x =>
                x.TenantId == tenantId &&
                x.OutletId == outletId &&
                x.Status == TenantAdminInventoryConstants.ActiveStatus)
            .OrderByDescending(x => x.IsReceivingLocation)
            .ThenByDescending(x => x.IsSellableLocation)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<InventoryBalance> ResolveOrCreateBalanceAsync(
        Guid tenantId,
        Guid inventoryLocationId,
        Guid productId,
        Guid variantId,
        Guid? batchId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var balance = await _dbContext.InventoryBalances.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.InventoryLocationId == inventoryLocationId &&
                 x.ProductId == productId &&
                 x.ProductVariantId == variantId &&
                 x.ProductBatchId == batchId,
            cancellationToken);

        if (balance is not null)
        {
            return balance;
        }

        balance = InventoryBalance.Create(
            Guid.NewGuid(),
            tenantId,
            inventoryLocationId,
            productId,
            variantId,
            batchId,
            now);

        await _dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
        return balance;
    }

    private async Task<Guid?> ResolveOrCreateBatchAsync(
        Guid tenantId,
        Guid productId,
        Guid variantId,
        string? batchNumber,
        DateOnly? manufacturedDate,
        DateOnly? expiryDate,
        Guid userId,
        DateTimeOffset receivedAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            return null;
        }

        var normalizedBatchNumber = batchNumber.Trim();
        var existingBatch = await _dbContext.ProductBatches.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.ProductId == productId &&
                 x.ProductVariantId == variantId &&
                 x.BatchNumber == normalizedBatchNumber &&
                 x.Status == TenantAdminInventoryConstants.ActiveStatus,
            cancellationToken);

        if (existingBatch is not null)
        {
            existingBatch.UpdateProfile(null, manufacturedDate, expiryDate, userId, now);
            existingBatch.MarkAsReceived(receivedAt);
            return existingBatch.Id;
        }

        var batch = ProductBatch.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            variantId,
            normalizedBatchNumber,
            supplierBatchNumber: null,
            manufacturedDate,
            expiryDate,
            receivedAt,
            TenantAdminInventoryConstants.ActiveStatus,
            userId,
            now);

        await _dbContext.ProductBatches.AddAsync(batch, cancellationToken);
        return batch.Id;
    }

    private async Task<TrackingProfile> ResolveTrackingProfileAsync(
        Guid tenantId,
        ProductVariant variant,
        CancellationToken cancellationToken)
    {
        var variantSetting = await _dbContext.ProductInventorySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == variant.ProductId &&
                     x.ProductVariantId == variant.Id &&
                     x.Status == TenantAdminInventoryConstants.ActiveStatus,
                cancellationToken);

        if (variantSetting is not null)
        {
            return new TrackingProfile(
                variantSetting.IsStockTracked,
                variantSetting.RequiresBatchTracking,
                variantSetting.RequiresExpiryTracking);
        }

        var productSetting = await _dbContext.ProductInventorySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == variant.ProductId &&
                     x.ProductVariantId == null &&
                     x.Status == TenantAdminInventoryConstants.ActiveStatus,
                cancellationToken);

        if (productSetting is not null)
        {
            return new TrackingProfile(
                productSetting.IsStockTracked,
                productSetting.RequiresBatchTracking,
                productSetting.RequiresExpiryTracking);
        }

        var hasExpiryBatch = await _dbContext.ProductBatches.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.ProductId == variant.ProductId &&
                 x.Status == TenantAdminInventoryConstants.ActiveStatus &&
                 x.ExpiryDate != null,
            cancellationToken);

        var hasBatch = hasExpiryBatch || await _dbContext.ProductBatches.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.ProductId == variant.ProductId &&
                 x.Status == TenantAdminInventoryConstants.ActiveStatus,
            cancellationToken);

        return new TrackingProfile(
            IsStockTracked: true,
            RequiresBatchTracking: hasBatch,
            RequiresExpiryTracking: hasExpiryBatch);
    }

    private static string? BuildMovementNote(string? referenceNumber, string? notes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(referenceNumber))
        {
            parts.Add($"[REF:{referenceNumber.Trim()}]");
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            parts.Add(notes.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private sealed class CurrentStockProjection
    {
        public Guid BalanceId { get; init; }
        public Guid InventoryLocationId { get; init; }
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public Guid? VariantId { get; init; }
        public string? VariantName { get; init; }
        public string? Sku { get; init; }
        public Guid OutletId { get; init; }
        public string OutletName { get; init; } = string.Empty;
        public Guid? BatchId { get; init; }
        public string? BatchNumber { get; init; }
        public DateOnly? ExpiryDate { get; init; }
        public decimal OnHandQuantity { get; init; }
        public decimal ReservedQuantity { get; init; }
        public decimal DamagedQuantity { get; init; }
        public decimal QuarantineQuantity { get; init; }
        public decimal AvailableQuantity { get; init; }
        public string StockStatus { get; init; } = string.Empty;
        public string ExpiryStatus { get; init; } = string.Empty;
        public long RowVersion { get; init; }
    }

    private readonly record struct TrackingProfile(
        bool IsStockTracked,
        bool RequiresBatchTracking,
        bool RequiresExpiryTracking);
}
