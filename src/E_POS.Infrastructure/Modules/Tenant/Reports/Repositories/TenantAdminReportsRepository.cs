using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed partial class TenantAdminReportsRepository : ITenantAdminReportsRepository
{
    private const string ActiveStatus = "ACTIVE";
    private const string CompletedStatus = "COMPLETED";
    private readonly EPosDbContext _dbContext;

    public TenantAdminReportsRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetScopeStampAsync(TenantRequestContext context, CancellationToken ct)
    {
        var outlets = await GetAccessibleOutletIdsAsync(context, ct);
        var (tills, scope) = await GetAccessibleTillIdsAsync(context, outlets, ct);
        return string.Join(",", outlets.Order()) + ":" + scope + ":" + string.Join(",", tills.Order()) + ":" + string.Join(",", context.Permissions.Order());
    }

    public async Task<bool> CanAccessAsync(TenantRequestContext context, Guid? outletId, Guid? tillId, CancellationToken ct)
    {
        var outlets = await GetAccessibleOutletIdsAsync(context, ct);
        if (outlets.Count == 0 || (outletId.HasValue && !outlets.Contains(outletId.Value))) return false;
        if (!tillId.HasValue) return true;
        var (tills, _) = await GetAccessibleTillIdsAsync(context, outlets, ct);
        return tills.Contains(tillId.Value) && await _dbContext.Tills.AnyAsync(t => t.Id == tillId.Value
            && t.TenantId == context.TenantId && (!outletId.HasValue || t.OutletId == outletId.Value), ct);
    }

    public async Task<ReportFilterOptionsResponse> GetFilterOptionsAsync(
        TenantRequestContext context,
        ReportFilterOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var includeInactive = request.IncludeInactive;
        var productTake = request.PageSize;
        var productSkip = (request.Page - 1) * request.PageSize;
        var search = request.Search?.Trim();

        var outlets = await _dbContext.Outlets.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && outletIds.Contains(x.Id) &&
                        (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.OutletName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.OutletCode, x.OutletName, x.Status, null, x.OutletType, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var (tillsIds, _) = await GetAccessibleTillIdsAsync(context, outletIds, cancellationToken);

        var tills = await _dbContext.Tills.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId &&
                        tillsIds.Contains(x.Id) &&
                        (!request.OutletId.HasValue || x.OutletId == request.OutletId.Value) &&
                        (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.TillName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.TillCode, x.TillName, x.Status, x.OutletId.ToString(), x.TillAreaName, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var departments = await _dbContext.Departments.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.DepartmentName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.DepartmentCode, x.DepartmentName, x.Status, null, null, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var categories = await _dbContext.Categories.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId &&
                        x.ParentCategoryId == null &&
                        (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.CategoryName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.CategoryCode, x.CategoryName, x.Status, x.ParentCategoryId.HasValue ? x.ParentCategoryId.Value.ToString() : null, null, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var subcategories = await _dbContext.Categories.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId &&
                        (!request.CategoryId.HasValue || x.ParentCategoryId == request.CategoryId.Value) &&
                        x.ParentCategoryId != null &&
                        (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.CategoryName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.CategoryCode, x.CategoryName, x.Status, x.ParentCategoryId!.Value.ToString(), null, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var brands = await _dbContext.Brands.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.BrandName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.BrandCode, x.BrandName, x.Status, null, null, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var productQuery = _dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && (includeInactive || x.Status == ActiveStatus));
        if (!string.IsNullOrWhiteSpace(search))
        {
            productQuery = productQuery.Where(x => x.ProductName.Contains(search) || x.ProductCode.Contains(search));
        }

        var products = await productQuery
            .OrderBy(x => x.ProductName)
            .Skip(productSkip)
            .Take(productTake)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.ProductCode, x.ProductName, x.Status, null, x.ProductType, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var variants = await _dbContext.ProductVariants.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId &&
                        (!request.ProductId.HasValue || x.ProductId == request.ProductId.Value) &&
                        (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.VariantName)
            .Take(productTake)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.Sku, x.VariantName, x.Status, x.ProductId.ToString(), null, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var paymentMethods = await _dbContext.PaymentMethods.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.MethodName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.MethodCode, x.MethodName, x.Status, null, x.MethodType, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var channels = await _dbContext.SalesChannels.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.SortOrder)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), null, x.CustomName, x.Status, null, null, x.Status == ActiveStatus))
            .ToListAsync(cancellationToken);

        var groups = new Dictionary<string, IReadOnlyList<ReportFilterOptionDto>>
        {
            ["outlets"] = outlets,
            ["tills"] = tills,
            ["cashiers"] = await GetCashierOptionsAsync(context, outletIds, request.OutletId, cancellationToken),
            ["customers"] = [],
            ["departments"] = departments,
            ["categories"] = categories,
            ["subcategories"] = subcategories,
            ["brands"] = brands,
            ["products"] = products,
            ["variants"] = variants,
            ["productVariants"] = variants,
            ["salesChannels"] = channels,
            ["paymentMethods"] = paymentMethods,
            ["orderStatuses"] = StaticOptions("COMPLETED", "CONFIRMED", "ACCEPTED", "DRAFT", "CANCELLED", "VOIDED"),
            // Values written by SalesPayment / SalesOrder; PENDING and FAILED never count as receipts.
            ["paymentStatuses"] = StaticOptions("PAID", "PARTIALLY_REFUNDED", "REFUNDED", "PENDING", "PAYMENT_SUBMITTED", "UNPAID", "FAILED", "CANCELLED"),
            ["refundStatuses"] = StaticOptions("COMPLETED"),
            ["returnStatuses"] = StaticOptions("COMPLETED"),
            ["fulfilmentStatuses"] = StaticOptions("PENDING", "ACCEPTED", "PREPARING", "READY_FOR_COLLECTION", "COLLECTED", "CANCELLED"),
            ["stockStatuses"] = StaticOptions("IN_STOCK", "LOW_STOCK", "OUT_OF_STOCK"),
            ["expiryStatuses"] = StaticOptions("VALID", "EXPIRING_SOON", "EXPIRED", "NOT_APPLICABLE"),
            // Movement types written to the stock ledger by opening stock, POS sale, return and inventory flows.
            ["movementTypes"] = StaticOptions(StockMovementConstants.StockIn, ReportRules.SaleMovementType, StockMovementConstants.StockOut,
                ReportRules.ReturnMovementType, StockMovementConstants.Adjustment, StockMovementConstants.Transfer),
            ["returnReasons"] = await _dbContext.ReturnReasons.AsNoTracking()
                .Where(x => x.TenantId == context.TenantId && (includeInactive || x.IsActive))
                .OrderBy(x => x.SortOrder).ThenBy(x => x.ReasonName)
                .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.ReasonCode, x.ReasonName, x.IsActive ? ActiveStatus : "INACTIVE", null, x.AppliesTo, x.IsActive))
                .ToListAsync(cancellationToken)
        };

        return new ReportFilterOptionsResponse(
            ReportBusinessDateCalculator.FromInstant(DateTimeOffset.UtcNow, tenantInfo.Timezone),
            tenantInfo.Timezone,
            tenantInfo.CurrencyCode,
            tenantInfo.Locale,
            groups);
    }

    private async Task<ReportResultDto> GetDashboardCoreAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var orders = ApplyPostedSalesFilters(await BuildOrderQueryAsync(context, request, cancellationToken), request, tenantInfo.Timezone);
        var currentOrders = await orders.ToListAsync(cancellationToken);
        var summary = await ReconciledSalesSummaryAsync(tenantInfo, context, request, currentOrders, cancellationToken);
        var paymentBreakdown = await BuildPaymentBreakdownAsync(context, request, currentOrders.Select(x => x.Id).ToList(), cancellationToken);
        var topProducts = await BuildTopProductsAsync(context.TenantId, currentOrders.Select(x => x.Id).ToList(), cancellationToken);

        return new ReportResultDto(
            "dashboard",
            tenantInfo.CurrencyCode,
            tenantInfo.Timezone,
            request.From,
            request.To,
            summary,
            new Dictionary<string, object?>
            {
                ["salesTrend"] = BuildDailyTrend(currentOrders),
                ["paymentBreakdown"] = paymentBreakdown,
                ["topSellingProducts"] = topProducts,
                ["outletPerformance"] = BuildOutletPerformance(currentOrders)
            },
            [],
            null,
            DateTimeOffset.UtcNow);
    }

    private async Task<ReportResultDto> GetSalesCoreAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var section = request.Section ?? "summary";

        if (section is "payments" or "payment-transactions") return await BuildPaymentEventsAsync(tenantInfo, section, request, context, cancellationToken);
        if (section == "returns") return await BuildReturnsResultAsync(tenantInfo, section, request, context, cancellationToken);
        if (section == "online") return await BuildOnlineOrdersResultAsync(tenantInfo, section, request, context, cancellationToken);
        if (section == "collections") return await BuildCollectionsResultAsync(tenantInfo, section, request, context, cancellationToken);

        var orders = ApplyPostedSalesFilters(await BuildOrderQueryAsync(context, request, cancellationToken), section == "products" ? request with { Search = null } : request, tenantInfo.Timezone);
        var orderRows = await orders.ToListAsync(cancellationToken);
        var orderIds = orderRows.Select(x => x.Id).ToList();

        return section switch
        {
            "transactions" => await BuildTransactionsResultAsync(tenantInfo, section, request, orders, context, cancellationToken),
            "products" => await BuildProductSalesResultAsync(tenantInfo, section, request, context, orderIds, cancellationToken),
            "channels" => await BuildChannelSalesResultAsync(tenantInfo, section, request, context, orderIds, cancellationToken),
            "categories" => await BuildCategorySalesResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),
            "tax" => await BuildTaxResultAsync(tenantInfo, section, request, context, orderIds, cancellationToken),
            "discounts" => await BuildDiscountResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),
            "cashiers" => BuildDictionaryResult(tenantInfo, section, request, BuildCashierRows(orderRows)),
            "daily" => BuildDictionaryResult(tenantInfo, section, request, BuildDailyRows(orderRows)),
            _ => new ReportResultDto(section, tenantInfo.CurrencyCode, tenantInfo.Timezone, request.From, request.To, await ReconciledSalesSummaryAsync(tenantInfo, context, request, orderRows, cancellationToken),
                new Dictionary<string, object?> { ["salesTrend"] = BuildDailyTrend(orderRows), ["paymentBreakdown"] = await BuildPaymentBreakdownAsync(context, request, orderIds, cancellationToken), ["topSellingProducts"] = await BuildTopProductsAsync(context.TenantId, orderIds, cancellationToken) },
                [], null, DateTimeOffset.UtcNow)
        };
    }

    private async Task<ReportResultDto> GetStockCoreAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var canViewValue = context.HasPermission(StockPermissions.ValueView);
        var tenantToday = ReportBusinessDateCalculator.FromInstant(DateTimeOffset.UtcNow, tenantInfo.Timezone);
        var section = request.Section ?? "current";
        var stockStatusFilter = request.StockStatus;
        var expiryStatusFilter = request.ExpiryStatus;
        var batchNumberFilter = request.BatchNumber;
        var searchFilter = request.Search;

        if (section == "movements")
        {
            return await BuildStockMovementsResultAsync(tenantInfo, context, request, outletIds, canViewValue, cancellationToken);
        }

        var query =
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking() on balance.InventoryLocationId equals location.Id
            join outlet in _dbContext.Outlets.AsNoTracking() on location.OutletId equals outlet.Id
            join product in _dbContext.Products.AsNoTracking() on balance.ProductId equals product.Id
            join variant in _dbContext.ProductVariants.AsNoTracking() on balance.ProductVariantId equals variant.Id into variants
            from variant in variants.DefaultIfEmpty()
            join batch in _dbContext.ProductBatches.AsNoTracking() on balance.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            join reorder in _dbContext.InventoryReorderRules.AsNoTracking()
                on new { balance.TenantId, balance.InventoryLocationId, balance.ProductId, balance.ProductVariantId }
                equals new { reorder.TenantId, reorder.InventoryLocationId, reorder.ProductId, reorder.ProductVariantId } into reorders
            from reorder in reorders.DefaultIfEmpty()
            where balance.TenantId == context.TenantId &&
                  outletIds.Contains(location.OutletId) &&
                  (!request.OutletId.HasValue || location.OutletId == request.OutletId.Value) &&
                  (!request.InventoryLocationId.HasValue || balance.InventoryLocationId == request.InventoryLocationId.Value) &&
                  (!request.BrandId.HasValue || product.BrandId == request.BrandId.Value) &&
                  (!request.ProductId.HasValue || balance.ProductId == request.ProductId.Value) &&
                  (!request.ProductVariantId.HasValue || balance.ProductVariantId == request.ProductVariantId.Value)
            let reorderPoint = reorder == null ? 0 : reorder.ReorderPointQuantity
            let expiryStatus = batch == null || batch.ExpiryDate == null ? "NOT_APPLICABLE" :
                batch.ExpiryDate.Value < tenantToday ? "EXPIRED" :
                batch.ExpiryDate.Value <= tenantToday.AddDays(30) ? "EXPIRING_SOON" : "VALID"
            let stockStatus = balance.AvailableQuantity <= 0 ? "OUT_OF_STOCK" :
                balance.AvailableQuantity <= reorderPoint ? "LOW_STOCK" : "IN_STOCK"
            where (section != "low-stock" || (balance.AvailableQuantity > 0 && balance.AvailableQuantity <= reorderPoint)) &&
                  (section != "out-of-stock" || balance.AvailableQuantity <= 0) &&
                  (section != "batch-expiry" || balance.ProductBatchId != null) &&
                  (!string.IsNullOrWhiteSpace(stockStatusFilter) ? stockStatus == stockStatusFilter : true) &&
                  (!string.IsNullOrWhiteSpace(expiryStatusFilter) ? expiryStatus == expiryStatusFilter : true) &&
                  (!string.IsNullOrWhiteSpace(batchNumberFilter) ? batch != null && batch.BatchNumber.Contains(batchNumberFilter) : true) &&
                  (!string.IsNullOrWhiteSpace(searchFilter) ? product.ProductName.Contains(searchFilter!) || product.ProductCode.Contains(searchFilter!) || (variant != null && (variant.Sku ?? string.Empty).Contains(searchFilter!)) : true)
            select new
            {
                balance,
                location,
                outlet,
                product,
                variant,
                batch,
                reorder,
                reorderPoint,
                expiryStatus,
                stockStatus
            };

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderBy(x => x.product.ProductName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var balanceIds = page.Select(x => x.balance.Id).ToList();
        var costByBalance = await _dbContext.InventoryCostLayers.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && balanceIds.Contains(x.InventoryBalanceId) && x.Status != "DELETED")
            .GroupBy(x => x.InventoryBalanceId)
            .Select(g => new { BalanceId = g.Key, Qty = g.Sum(x => x.RemainingQuantity), Value = g.Sum(x => x.RemainingQuantity * x.UnitCost) })
            .ToDictionaryAsync(x => x.BalanceId, cancellationToken);
        var lastMovementByBalance = await _dbContext.StockMovements.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && balanceIds.Contains(x.InventoryBalanceId))
            .GroupBy(x => x.InventoryBalanceId)
            .Select(g => new { BalanceId = g.Key, Last = g.Max(x => x.OccurredAt) })
            .ToDictionaryAsync(x => x.BalanceId, x => x.Last, cancellationToken);

        var records = page.Select(x =>
        {
            costByBalance.TryGetValue(x.balance.Id, out var cost);
            lastMovementByBalance.TryGetValue(x.balance.Id, out var lastMovementAt);
            var unitCost = cost == null || cost.Qty == 0 ? (decimal?)null : cost.Value / cost.Qty;
            var stockValue = cost?.Value;
            return section switch
            {
                "low-stock" => Row(("productId", x.product.Id), ("productName", x.product.ProductName), ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("sku", x.variant?.Sku), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName), ("availableQuantity", x.balance.AvailableQuantity), ("reorderPointQuantity", x.reorderPoint), ("reorderQuantity", x.reorder?.ReorderQuantity), ("safetyStockQuantity", x.reorder?.SafetyStockQuantity), ("shortageQuantity", Math.Max(x.reorderPoint - x.balance.AvailableQuantity, 0)), ("lastMovementAt", lastMovementAt), ("status", x.stockStatus)),
                "out-of-stock" => Row(("productId", x.product.Id), ("productName", x.product.ProductName), ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("sku", x.variant?.Sku), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName), ("availableQuantity", x.balance.AvailableQuantity), ("reorderPointQuantity", x.reorderPoint), ("lastInStockAt", null), ("lastMovementAt", lastMovementAt), ("status", x.stockStatus)),
                "batch-expiry" => Row(("productBatchId", x.batch?.Id), ("productId", x.product.Id), ("productName", x.product.ProductName), ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("batchNumber", x.batch?.BatchNumber), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName), ("inventoryLocationId", x.location.Id), ("inventoryLocationName", x.location.LocationName), ("manufacturedAt", x.batch?.ManufacturedAt), ("firstReceivedAt", x.batch?.FirstReceivedAt), ("expiryDate", x.batch?.ExpiryDate), ("daysUntilExpiry", x.batch?.ExpiryDate == null ? null : x.batch.ExpiryDate.Value.DayNumber - tenantToday.DayNumber), ("onHandQuantity", x.balance.OnHandQuantity), ("availableQuantity", x.balance.AvailableQuantity), ("batchStatus", x.batch?.Status), ("expiryStatus", x.expiryStatus)),
                "valuation" => Row(("productId", x.product.Id), ("productName", x.product.ProductName), ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName), ("inventoryLocationId", x.location.Id), ("inventoryLocationName", x.location.LocationName), ("onHandQuantity", x.balance.OnHandQuantity), ("availableQuantity", x.balance.AvailableQuantity), ("costingMethod", "COST_LAYER"), ("remainingCostLayerQuantity", cost?.Qty ?? 0), ("averageUnitCost", canViewValue ? unitCost : null), ("totalInventoryValue", canViewValue ? stockValue : null), ("currencyCode", tenantInfo.CurrencyCode)),
                _ => Row(("inventoryBalanceId", x.balance.Id), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName), ("inventoryLocationId", x.location.Id), ("inventoryLocationName", x.location.LocationName), ("productId", x.product.Id), ("productName", x.product.ProductName), ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("sku", x.variant?.Sku), ("barcode", null), ("productBatchId", x.batch?.Id), ("batchNumber", x.batch?.BatchNumber), ("expiryDate", x.batch?.ExpiryDate), ("onHandQuantity", x.balance.OnHandQuantity), ("reservedQuantity", x.balance.ReservedQuantity), ("damagedQuantity", x.balance.DamagedQuantity), ("quarantineQuantity", x.balance.QuarantineQuantity), ("availableQuantity", x.balance.AvailableQuantity), ("reorderPointQuantity", x.reorderPoint), ("reorderQuantity", x.reorder?.ReorderQuantity), ("unitCost", canViewValue ? unitCost : null), ("stockValue", canViewValue ? stockValue : null), ("stockStatus", x.stockStatus), ("expiryStatus", x.expiryStatus), ("lastMovementAt", lastMovementAt), ("currencyCode", tenantInfo.CurrencyCode), ("rowVersion", x.balance.RowVersion))
            };
        }).ToList();

        // KPIs cover the entire authorized filtered set, independently of the visible page.
        var totals = await query.GroupBy(x => 1).Select(g => new {
            OnHand = g.Sum(x => x.balance.OnHandQuantity), Available = g.Sum(x => x.balance.AvailableQuantity),
            Reserved = g.Sum(x => x.balance.ReservedQuantity), Damaged = g.Sum(x => x.balance.DamagedQuantity),
            Quarantine = g.Sum(x => x.balance.QuarantineQuantity),
            Low = g.Count(x => x.stockStatus == "LOW_STOCK"), Out = g.Count(x => x.stockStatus == "OUT_OF_STOCK"),
            Expiring = g.Count(x => x.expiryStatus == "EXPIRING_SOON")
        }).SingleOrDefaultAsync(cancellationToken);
        var matchingBalances = query.Select(x => x.balance.Id);
        decimal? totalValue = canViewValue ? await _dbContext.InventoryCostLayers.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && matchingBalances.Contains(x.InventoryBalanceId) && x.Status != "DELETED")
            .SumAsync(x => (decimal?)(x.RemainingQuantity * x.UnitCost), cancellationToken) ?? 0m : null;
        var summary = new Dictionary<string, object?>
        {
            ["totalStockQuantity"] = totals?.OnHand ?? 0m, ["availableQuantity"] = totals?.Available ?? 0m,
            ["reservedQuantity"] = totals?.Reserved ?? 0m, ["damagedQuantity"] = totals?.Damaged ?? 0m,
            ["quarantineQuantity"] = totals?.Quarantine ?? 0m, ["lowStockItemCount"] = totals?.Low ?? 0,
            ["outOfStockItemCount"] = totals?.Out ?? 0, ["expiringSoonItemCount"] = totals?.Expiring ?? 0,
            ["totalStockValue"] = totalValue, ["dateBasis"] = "CURRENT AS-OF (NOT HISTORICAL)",
            ["asOf"] = DateTimeOffset.UtcNow, ["balanceRowCount"] = total,
            ["inventoryScope"] = "TRACKED PRODUCTS WITH AN INVENTORY BALANCE ONLY",
            ["availableQuantityFormula"] = "onHand - reserved - damaged - quarantine"
        };
        return Result(tenantInfo, section, request, summary, records, total);
    }

    private async Task<ReportResultDto> GetOutletsCoreAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var section = request.Section ?? "performance";
        if (section == "tills")
        {
            return await BuildTillSummaryResultAsync(tenantInfo, context, request, cancellationToken);
        }

        var orders = (await BuildOrderQueryAsync(context, request, cancellationToken));
        orders = ApplySalesFilters(orders, request);
        var orderRows = await orders.ToListAsync(cancellationToken);
        var records = section == "cashiers" ? BuildCashierRows(orderRows) : BuildOutletPerformance(orderRows);
        var summary = new Dictionary<string, object?>
        {
            ["totalOutletSales"] = orderRows.Sum(x => x.TotalAmount - x.RefundedAmount),
            ["totalTransactions"] = orderRows.Count,
            ["averageOrderValue"] = orderRows.Count == 0 ? 0 : orderRows.Sum(x => x.TotalAmount - x.RefundedAmount) / orderRows.Count,
            ["totalRefunds"] = orderRows.Sum(x => x.RefundedAmount),
            ["totalDiscounts"] = orderRows.Sum(x => x.DiscountAmount),
            ["totalTax"] = orderRows.Sum(x => x.TaxAmount)
        };
        return Result(tenantInfo, section, request, summary, records, records.Count);
    }

    public async Task<SalesTransactionDetailDto?> GetSalesTransactionDetailAsync(
        TenantRequestContext context,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var request = new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var order = await (await BuildOrderQueryAsync(context, request, cancellationToken))
            .Where(x => x.Id == orderId)
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null) return null;

        var canViewPii = context.HasPermission("tenant.reports.customer-pii.view");
        var itemRows = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && x.SalesOrderId == order.Id)
            .OrderBy(x => x.LineNumber)
            .Select(x => new
            {
                x.LineNumber,
                x.ProductNameSnapshot,
                x.VariantNameSnapshot,
                x.SkuSnapshot,
                x.BarcodeSnapshot,
                x.Quantity,
                x.ReturnedQuantity,
                x.UnitPrice,
                x.LineSubtotalAmount,
                x.LineDiscountAmount,
                x.LineTaxAmount,
                x.LineTotalAmount,
                x.LineStatus
            })
            .ToListAsync(cancellationToken);
        var items = itemRows
            .Select(x => Row(("lineNumber", x.LineNumber), ("productName", x.ProductNameSnapshot), ("variantName", x.VariantNameSnapshot),
                ("sku", x.SkuSnapshot), ("barcode", x.BarcodeSnapshot), ("quantity", x.Quantity), ("returnedQuantity", x.ReturnedQuantity),
                ("unitPrice", x.UnitPrice), ("lineSubtotalAmount", x.LineSubtotalAmount), ("lineDiscountAmount", x.LineDiscountAmount),
                ("lineTaxAmount", x.LineTaxAmount), ("lineTotalAmount", x.LineTotalAmount), ("lineStatus", x.LineStatus)))
            .ToList();

        var paymentRows = await _dbContext.SalesPayments.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && x.SalesOrderId == order.Id)
            .Join(_dbContext.PaymentMethods.AsNoTracking(), p => p.PaymentMethodId, m => m.Id, (p, m) => new { p, m })
            .Select(x => new
            {
                x.p.PaymentNumber,
                x.m.MethodName,
                x.p.PaymentStatus,
                x.p.RequestedAmount,
                x.p.TenderedAmount,
                x.p.PaidAmount,
                x.p.ChangeAmount,
                x.p.RefundedAmount,
                x.p.PaidAt
            })
            .ToListAsync(cancellationToken);
        var payments = paymentRows
            .Select(x => Row(("paymentNumber", x.PaymentNumber), ("paymentMethodName", x.MethodName), ("status", x.PaymentStatus),
                ("requestedAmount", x.RequestedAmount), ("tenderedAmount", x.TenderedAmount), ("paidAmount", x.PaidAmount),
                ("changeAmount", x.ChangeAmount), ("refundedAmount", x.RefundedAmount), ("paidAt", x.PaidAt)))
            .ToList();

        return new SalesTransactionDetailDto(
            order.Id,
            order.OrderNumber,
            new Dictionary<string, object?>
            {
                ["orderNumber"] = order.OrderNumber,
                ["businessDate"] = order.BusinessDate,
                ["completedAt"] = order.CompletedAt,
                ["customerName"] = order.CustomerNameSnapshot,
                ["customerEmail"] = canViewPii ? order.CustomerEmailSnapshot : null,
                ["customerPhone"] = canViewPii ? order.CustomerPhoneSnapshot : null
            },
            new Dictionary<string, object?>
            {
                ["subtotalAmount"] = order.SubtotalAmount,
                ["discountAmount"] = order.DiscountAmount,
                ["taxAmount"] = order.TaxAmount,
                ["totalAmount"] = order.TotalAmount,
                ["paidAmount"] = order.PaidAmount,
                ["refundedAmount"] = order.RefundedAmount,
                ["netAmount"] = order.TotalAmount - order.RefundedAmount
            },
            items,
            payments,
            await GetDiscountRowsAsync(context.TenantId, [order.Id], cancellationToken),
            await GetTaxRowsAsync(context.TenantId, [order.Id], cancellationToken),
            await GetReturnRowsAsync(context.TenantId, [order.Id], cancellationToken),
            BuildNotes(order),
            order.CurrencyCode);
    }

    private async Task<IQueryable<OrderProjection>> BuildOrderQueryAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken)
    {
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var (tillIds, tillScope) = await GetAccessibleTillIdsAsync(context, outletIds, cancellationToken);
        var successfulPaymentStatuses = ReportRules.SuccessfulPaymentStatuses;
        const string clickAndCollect = ReportRules.ClickAndCollectOrderType;
        var query =
            from order in _dbContext.SalesOrders.AsNoTracking()
            join channel in _dbContext.SalesChannels.AsNoTracking() on order.SalesChannelId equals channel.Id
            join platformChannel in _dbContext.PlatformSalesChannels.AsNoTracking() on channel.PlatformSalesChannelId equals platformChannel.Id into platformChannels
            from platformChannel in platformChannels.DefaultIfEmpty()
            join till in _dbContext.Tills.AsNoTracking() on order.TillId equals till.Id into tills
            from till in tills.DefaultIfEmpty()
            join user in _dbContext.TenantUsers.AsNoTracking() on order.CreatedByTenantUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            let effectiveOutletId = order.ReportingOutletId ?? (till == null ? null : till.OutletId)
            where order.TenantId == context.TenantId &&
                  (effectiveOutletId != null && outletIds.Contains(effectiveOutletId.Value)) &&
                  ((order.TillId == null && tillScope == E_POS.Domain.Modules.Tenant.AccessControl.Constants.TenantUserAccessScopes.AllAccessibleTills) || (order.TillId != null && tillIds.Contains(order.TillId.Value)))
            select new OrderProjection
            {
                Id = order.Id,
                TenantId = order.TenantId,
                OrderNumber = order.OrderNumber,
                ExternalOrderReference = order.ExternalOrderReference,
                BusinessDate = order.BusinessDate,
                PlacedAt = order.PlacedAt,
                CompletedAt = order.CompletedAt,
                // A click-and-collect sale posts when it is paid; collection later only changes fulfilment.
                PostingAt = order.OrderType == clickAndCollect
                    ? _dbContext.SalesPayments.Where(p => p.TenantId == context.TenantId && p.SalesOrderId == order.Id && successfulPaymentStatuses.Contains(p.PaymentStatus)).Max(p => p.PaidAt)
                    : order.CompletedAt,
                OrderType = order.OrderType,
                RequestedCollectionAt = order.RequestedCollectionAt,
                CancelledAt = order.CancelledAt,
                UpdatedByUserId = order.UpdatedByTenantUserId,
                ChannelCode = platformChannel == null ? null : platformChannel.ChannelCode,
                TillType = till == null ? null : till.TillType,
                SalesChannelId = order.SalesChannelId,
                SalesChannelName = channel.CustomName,
                ReportingOutletId = effectiveOutletId,
                OutletName = order.ReportingOutletNameSnapshot,
                TillId = order.TillId,
                TillCode = till == null ? null : till.TillCode,
                TillName = till == null ? null : till.TillName,
                TillSessionId = order.TillSessionId,
                CashierId = order.CreatedByTenantUserId,
                CashierName = user == null ? null : user.DisplayName ?? user.FullName,
                CustomerId = order.CustomerId,
                CustomerNameSnapshot = order.CustomerNameSnapshot,
                CustomerEmailSnapshot = order.CustomerEmailSnapshot,
                CustomerPhoneSnapshot = order.CustomerPhoneSnapshot,
                CurrencyCode = order.CurrencyCode,
                SubtotalAmount = order.SubtotalAmount,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                ChargeAmount = order.ChargeAmount,
                RoundingAmount = order.RoundingAmount,
                TotalAmount = order.TotalAmount,
                PaidAmount = order.PaidAmount,
                RefundedAmount = order.RefundedAmount,
                PaymentStatus = order.PaymentStatus,
                FulfillmentStatus = order.FulfillmentStatus,
                OrderStatus = order.Status,
                CustomerNote = order.CustomerNote,
                InternalNote = order.InternalNote
            };
        return query;
    }

    private static IQueryable<OrderProjection> ApplySalesFilters(IQueryable<OrderProjection> query, ReportQueryRequest request)
    {
        if (request.From.HasValue) query = query.Where(x => x.BusinessDate >= request.From.Value);
        if (request.To.HasValue) query = query.Where(x => x.BusinessDate <= request.To.Value);
        if (request.OutletId.HasValue) query = query.Where(x => x.ReportingOutletId == request.OutletId.Value);
        if (request.TillId.HasValue) query = query.Where(x => x.TillId == request.TillId.Value);
        if (request.CashierId.HasValue) query = query.Where(x => x.CashierId == request.CashierId.Value);
        if (request.CustomerId.HasValue) query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        if (request.SalesChannelId.HasValue) query = query.Where(x => x.SalesChannelId == request.SalesChannelId.Value);
        if (!string.IsNullOrWhiteSpace(request.OrderStatus)) query = query.Where(x => x.OrderStatus == request.OrderStatus);
        if (!string.IsNullOrWhiteSpace(request.PaymentStatus)) query = query.Where(x => x.PaymentStatus == request.PaymentStatus);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(search) || (x.CustomerNameSnapshot != null && x.CustomerNameSnapshot.Contains(search)));
        }
        return query;
    }

    private async Task<ReportResultDto> BuildTransactionsResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, IQueryable<OrderProjection> query, TenantRequestContext context, CancellationToken cancellationToken)
    {
        query = ApplyOrderSort(query, request.SortBy, request.SortDirection);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.ToListAsync(cancellationToken);
        var orderIds = page.Select(x => x.Id).ToList();
        var lineCounts = await _dbContext.SalesOrderLines.AsNoTracking().Where(x => x.TenantId == tenantInfo.TenantId && x.SalesOrderId.HasValue && orderIds.Contains(x.SalesOrderId.Value)).GroupBy(x => x.SalesOrderId!.Value).Select(g => new { Id = g.Key, Count = g.Count(), Qty = g.Sum(x => x.Quantity) }).ToListAsync(cancellationToken);
        var paymentNames = await GetPaymentMethodNamesAsync(tenantInfo.TenantId, orderIds, cancellationToken);
        var records = page.Select(x =>
        {
            var lines = lineCounts.FirstOrDefault(l => l.Id == x.Id);
                        return new Dictionary<string, object?> {
                { "orderId", x.Id }, { "orderNumber", x.OrderNumber }, { "externalReference", x.ExternalOrderReference },
                { "businessDate", x.BusinessDate }, { "placedAt", x.PlacedAt }, { "paidAt", x.PostingAt }, { "postedAt", x.PostingAt },
                { "completedAt", x.CompletedAt }, { "rowType", "SALE" }, { "orderType", x.OrderType },
                { "salesChannelId", x.SalesChannelId }, { "salesChannelName", x.SalesChannelName }, { "channelCode", x.ChannelCode },
                { "salesExcludingTax", x.TotalAmount - x.TaxAmount },
                { "outletId", x.ReportingOutletId }, { "outletName", x.OutletName }, { "tillId", x.TillId },
                { "tillCode", x.TillCode }, { "tillName", x.TillName }, { "tillSessionId", x.TillSessionId },
                { "cashierId", x.CashierId }, { "cashierName", x.CashierName }, { "customerId", x.CustomerId },
                { "customerName", x.CustomerNameSnapshot }, { "lineCount", lines?.Count ?? 0 }, { "totalQuantity", lines?.Qty ?? 0 },
                { "currencyCode", x.CurrencyCode }, { "subtotalAmount", x.SubtotalAmount }, { "discountAmount", x.DiscountAmount },
                { "taxAmount", x.TaxAmount }, { "chargeAmount", x.ChargeAmount }, { "roundingAmount", x.RoundingAmount },
                { "totalAmount", x.TotalAmount }, { "paidAmount", x.PaidAmount }, { "refundedAmount", x.RefundedAmount },
                { "netAmount", x.TotalAmount }, { "paymentMethodNames", paymentNames.GetValueOrDefault(x.Id, "") },
                { "paymentStatus", x.PaymentStatus }, { "fulfilmentStatus", x.FulfillmentStatus }, { "orderStatus", x.OrderStatus }
            };
        }).ToList();
        var adjustments = await ReturnAdjustmentsAsync(tenantInfo, context, request, cancellationToken);
        // Return adjustments post on their own date and link back to the original sale.
        foreach (var group in adjustments.GroupBy(x => new { x.ReturnId, x.OrderId, x.ReturnNumber, x.OriginalOrderNumber, x.PostedAt, x.ChannelId, x.ChannelName, x.OutletId }))
            records.Add(new Dictionary<string, object?> {
                ["rowType"] = "RETURN", ["returnId"] = group.Key.ReturnId, ["returnNumber"] = group.Key.ReturnNumber,
                ["orderId"] = group.Key.OrderId, ["originalOrderId"] = group.Key.OrderId, ["orderNumber"] = group.Key.ReturnNumber,
                ["originalOrderNumber"] = group.Key.OriginalOrderNumber, ["paidAt"] = group.Key.PostedAt, ["postedAt"] = group.Key.PostedAt,
                ["salesChannelId"] = group.Key.ChannelId, ["salesChannelName"] = group.Key.ChannelName, ["outletId"] = group.Key.OutletId,
                ["totalQuantity"] = -group.Sum(x => x.Quantity), ["subtotalAmount"] = -group.Sum(x => x.Base),
                ["salesExcludingTax"] = -group.Sum(x => x.Base),
                ["taxAmount"] = -group.Sum(x => x.Tax), ["totalAmount"] = -group.Sum(x => x.Base + x.Tax),
                ["netAmount"] = -group.Sum(x => x.Base + x.Tax), ["currencyCode"] = tenantInfo.CurrencyCode,
                ["orderStatus"] = "COMPLETED", ["paymentStatus"] = null
            });
        return Result(tenantInfo, section, request, await ReconciledSalesSummaryAsync(tenantInfo, context, request, page, cancellationToken),
            records.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(), records.Count);
    }

    private async Task<ReportResultDto> BuildProductSalesResultAsync(TenantInfo tenantInfo, string section,
        ReportQueryRequest request, TenantRequestContext context, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var lines = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && x.SalesOrderId.HasValue && orderIds.Contains(x.SalesOrderId.Value))
            .ToListAsync(cancellationToken);
        var returns = await ReturnAdjustmentsAsync(tenantInfo, context, request with { Search = null }, cancellationToken);
        var events = lines.Select(x => new { Line = x, Sold = x.Quantity, Returned = 0m,
            Sales = x.LineTotalAmount - x.LineTaxAmount, Return = 0m }).Concat(
            returns.Select(x => new { Line = x.Line, Sold = 0m, Returned = x.Quantity, Sales = 0m, Return = x.Base }));
        if (request.ProductId.HasValue) events = events.Where(x => x.Line.ProductId == request.ProductId);
        if (request.ProductVariantId.HasValue) events = events.Where(x => x.Line.ProductVariantId == request.ProductVariantId);
        var categoryId = request.SubcategoryId ?? request.CategoryId;
        if (categoryId.HasValue)
        {
            // Lines store category names only; the category identity comes from the product's category assignment
            // (a parent category also matches products assigned to its subcategories).
            var categoryIds = await _dbContext.Categories.AsNoTracking()
                .Where(x => x.TenantId == context.TenantId && (x.Id == categoryId.Value || x.ParentCategoryId == categoryId.Value))
                .Select(x => x.Id).ToListAsync(cancellationToken);
            var productIds = (await _dbContext.ProductCategories.AsNoTracking()
                .Where(x => x.TenantId == context.TenantId && categoryIds.Contains(x.CategoryId))
                .Select(x => x.ProductId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
            events = events.Where(x => productIds.Contains(x.Line.ProductId));
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
            events = events.Where(x => x.Line.ProductNameSnapshot.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                || (x.Line.VariantNameSnapshot?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.Line.SkuSnapshot?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.Line.BarcodeSnapshot?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false));
        var records = events.GroupBy(x => new { x.Line.ProductId, x.Line.ProductVariantId, x.Line.UomId }).Select(g => {
            var line = g.First().Line;
            return Row(("productId", line.ProductId), ("productVariantId", line.ProductVariantId),
                ("productName", line.ProductNameSnapshot), ("variantName", line.VariantNameSnapshot),
                ("sku", line.SkuSnapshot), ("barcode", line.BarcodeSnapshot), ("unit", line.UomCodeSnapshot),
                ("quantitySold", g.Sum(x => x.Sold)), ("quantityReturned", g.Sum(x => x.Returned)),
                ("netQuantity", g.Sum(x => x.Sold - x.Returned)), ("salesValueExTax", g.Sum(x => x.Sales)),
                ("returnValueExTax", g.Sum(x => x.Return)), ("netValueExTax", g.Sum(x => x.Sales - x.Return)),
                ("grossSalesAmount", g.Sum(x => x.Sales)), ("refundAmount", g.Sum(x => x.Return)),
                ("netSalesAmount", g.Sum(x => x.Sales - x.Return)), ("currencyCode", tenantInfo.CurrencyCode));
        }).ToList();
        var sort = request.SortBy == "netQuantity" ? "netQuantity" : "netValueExTax";
        records = (request.SortDirection == "desc" ? records.OrderByDescending(x => (decimal)x[sort]!)
            : records.OrderBy(x => (decimal)x[sort]!)).ThenBy(x => (string?)x["productName"]).ThenBy(x => (string?)x["variantName"]).ToList();
        // Quantities are only meaningful per unit, so the summary totals values and reports quantity per unit.
        var quantityByUnit = records.GroupBy(x => (string?)x["unit"] ?? string.Empty)
            .Select(g => (IReadOnlyDictionary<string, object?>)Row(("unit", g.Key), ("quantitySold", g.Sum(x => (decimal)x["quantitySold"]!)),
                ("quantityReturned", g.Sum(x => (decimal)x["quantityReturned"]!)), ("netQuantity", g.Sum(x => (decimal)x["netQuantity"]!)))).ToList();
        return Result(tenantInfo, section, request, Row(("salesValueExTax", records.Sum(x => (decimal)x["salesValueExTax"]!)),
                ("returnValueExTax", records.Sum(x => (decimal)x["returnValueExTax"]!)),
                ("netValueExTax", records.Sum(x => (decimal)x["netValueExTax"]!)), ("variantCount", records.Count),
                ("quantityByUnit", quantityByUnit), ("dateBasis", "SALE POSTING DATE / RETURN POSTING DATE")),
            records.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(), records.Count);
    }

    private async Task<ReportResultDto> BuildChannelSalesResultAsync(TenantInfo tenantInfo, string section,
        ReportQueryRequest request, TenantRequestContext context, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var sales = await (from o in _dbContext.SalesOrders.AsNoTracking()
                           join c in _dbContext.SalesChannels.AsNoTracking() on o.SalesChannelId equals c.Id
                           where o.TenantId == context.TenantId && orderIds.Contains(o.Id)
                           select new { o.SalesChannelId, c.CustomName, Base = o.TotalAmount - o.TaxAmount, o.TaxAmount }).ToListAsync(cancellationToken);
        // Return adjustments are allocated to the ORIGINAL sale's channel on the return posting date.
        var returns = await ReturnAdjustmentsAsync(tenantInfo, context, request, cancellationToken);
        var channels = sales.Select(x => x.SalesChannelId).Union(returns.Select(x => x.ChannelId)).ToList();
        var channelCodes = await (from c in _dbContext.SalesChannels.AsNoTracking()
                                  join p in _dbContext.PlatformSalesChannels.AsNoTracking() on c.PlatformSalesChannelId equals p.Id
                                  where c.TenantId == context.TenantId && channels.Contains(c.Id)
                                  select new { c.Id, p.ChannelCode, p.ChannelType }).ToDictionaryAsync(x => x.Id, cancellationToken);
        var rows = channels.Select(id => {
            var posted = sales.Where(x => x.SalesChannelId == id).ToList();
            var returned = returns.Where(x => x.ChannelId == id).ToList();
            var saleBase = posted.Sum(x => x.Base); var returnBase = returned.Sum(x => x.Base);
            var saleTax = posted.Sum(x => x.TaxAmount); var returnTax = returned.Sum(x => x.Tax);
            var netTax = saleTax - returnTax;
            var netSales = ReportRules.CalculateNetSales(saleBase, returnBase);
            channelCodes.TryGetValue(id, out var code);
            return Row(("salesChannelId", id), ("salesChannelName", posted.FirstOrDefault()?.CustomName ?? returned.First().ChannelName),
                ("channelCode", code?.ChannelCode), ("channelType", code?.ChannelType),
                ("saleCount", posted.Count), ("salesExcludingTax", saleBase), ("returnAdjustment", returnBase),
                ("netSalesExcludingTax", netSales), ("saleTax", saleTax), ("returnTax", returnTax), ("taxAmount", netTax),
                ("salesIncludingTax", posted.Sum(x => x.Base + x.TaxAmount)), ("netAmount", netSales + netTax),
                ("currencyCode", tenantInfo.CurrencyCode));
        }).OrderBy(x => (string?)x["salesChannelName"]).ToList();
        return Result(tenantInfo, section, request, Row(("salesExcludingTax", rows.Sum(x => (decimal)x["salesExcludingTax"]!)),
            ("returnAdjustment", rows.Sum(x => (decimal)x["returnAdjustment"]!)),
            ("netSalesExcludingTax", rows.Sum(x => (decimal)x["netSalesExcludingTax"]!)),
            ("netTax", rows.Sum(x => (decimal)x["taxAmount"]!)), ("saleCount", rows.Sum(x => (int)x["saleCount"]!)),
            ("netSalesIncludingTax", rows.Sum(x => (decimal)x["netAmount"]!)),
            ("dateBasis", "SALE POSTING DATE / RETURN POSTING DATE")), rows, rows.Count);
    }
    private async Task<ReportResultDto> BuildCategorySalesResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId.HasValue && orderIds.Contains(x.SalesOrderId.Value))
            .GroupBy(x => new { x.DepartmentNameSnapshot, x.CategoryNameSnapshot, x.SubcategoryNameSnapshot })
            .Select(g => new { g.Key, Qty = g.Sum(x => x.Quantity), Returned = g.Sum(x => x.ReturnedQuantity), Gross = g.Sum(x => x.LineSubtotalAmount), Discount = g.Sum(x => x.LineDiscountAmount), Transactions = g.Select(x => x.SalesOrderId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var total = rows.Sum(x => x.Gross - x.Discount);
        var records = rows.Select(x => Row(("departmentName", x.Key.DepartmentNameSnapshot), ("categoryName", x.Key.CategoryNameSnapshot), ("subcategoryName", x.Key.SubcategoryNameSnapshot), ("quantitySold", x.Qty), ("quantityReturned", x.Returned), ("grossSalesAmount", x.Gross), ("discountAmount", x.Discount), ("refundAmount", 0m), ("netSalesAmount", x.Gross - x.Discount), ("transactionCount", x.Transactions), ("percentageOfTotal", total == 0 ? 0 : (x.Gross - x.Discount) / total * 100), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, section, request, new Dictionary<string, object?>(), records, records.Count);
    }

    private async Task<ReportResultDto> BuildTaxResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request,
        TenantRequestContext context, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var returns = await ReturnAdjustmentsAsync(tenantInfo, context, request, cancellationToken);
        var returnOrderIds = returns.Select(x => x.OrderId).Distinct().ToList();
        var taxes = await _dbContext.SalesOrderTaxes.AsNoTracking().Where(x => x.TenantId == context.TenantId
            && (orderIds.Contains(x.SalesOrderId) || returnOrderIds.Contains(x.SalesOrderId))).ToListAsync(cancellationToken);
        var entries = taxes.Where(x => orderIds.Contains(x.SalesOrderId)).Select(x => new TaxEntry(
            x.TaxRateCodeSnapshot, x.TaxTreatmentSnapshot, x.TaxNameSnapshot, x.TaxRatePercent, x.TaxableAmount, 0m, x.TaxAmount, 0m)).ToList();
        foreach (var adjustment in returns)
        {
            var originalTaxes = taxes.Where(x => x.SalesOrderId == adjustment.OrderId && x.SalesOrderLineId == adjustment.Line.Id).ToList();
            // A legacy order-level tax cannot safely identify a returned line's code or rate.
            // Keep the stored reversal visible as unallocated instead of inventing a rate.
            if (originalTaxes.Count != 1)
                entries.Add(new(null, "UNALLOCATED_RETURN", "Stored return tax (code unavailable)", null, 0m, adjustment.Base, 0m, adjustment.Tax));
            else
            {
                var original = originalTaxes[0];
                entries.Add(new(original.TaxRateCodeSnapshot, original.TaxTreatmentSnapshot, original.TaxNameSnapshot,
                    original.TaxRatePercent, 0m, adjustment.Base, 0m, adjustment.Tax));
            }
        }
        var rows = entries.GroupBy(x => new { x.Code, x.Treatment, x.Name, x.Rate }).Select(g => Row(
            ("taxCode", g.Key.Code), ("taxTreatment", g.Key.Treatment), ("taxName", g.Key.Name), ("taxRate", g.Key.Rate),
            ("taxableAmount", g.Sum(x => x.SalesBase)), ("returnBase", g.Sum(x => x.ReturnBase)),
            ("netBase", g.Sum(x => x.SalesBase-x.ReturnBase)), ("taxAmount", g.Sum(x => x.SaleTax)),
            ("refundedTaxAmount", g.Sum(x => x.ReturnTax)), ("netTaxAmount", g.Sum(x => x.SaleTax-x.ReturnTax)),
            ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, section, request, Row(("taxableSales", entries.Sum(x => x.SalesBase)),
            ("taxCollected", entries.Sum(x => x.SaleTax)), ("refundedTax", entries.Sum(x => x.ReturnTax)),
            ("netTax", entries.Sum(x => x.SaleTax-x.ReturnTax))), rows, rows.Count);
    }

    private sealed record TaxEntry(string? Code, string? Treatment, string Name, decimal? Rate,
        decimal SalesBase, decimal ReturnBase, decimal SaleTax, decimal ReturnTax);

    private async Task<ReportResultDto> BuildDiscountResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.SalesOrderDiscounts.AsNoTracking()
            .Where(x => x.TenantId == tenantId && orderIds.Contains(x.SalesOrderId))
            .GroupBy(x => new { x.DiscountPolicyId, x.DiscountNameSnapshot, x.DiscountCodeSnapshot, x.DiscountTargetScope })
            .Select(g => new { g.Key, Count = g.Count(), Amount = g.Sum(x => x.DiscountAmount), Manual = g.Count(x => x.ManualDiscountReason != null), Approved = g.Count(x => x.ApprovedAt != null) })
            .ToListAsync(cancellationToken);
        var records = rows.Select(x => Row(("discountPolicyId", x.Key.DiscountPolicyId), ("discountName", x.Key.DiscountNameSnapshot), ("discountCode", x.Key.DiscountCodeSnapshot), ("discountScope", x.Key.DiscountTargetScope), ("usageCount", x.Count), ("discountAmount", x.Amount), ("averageDiscountAmount", x.Count == 0 ? 0 : x.Amount / x.Count), ("manualDiscountCount", x.Manual), ("managerApprovalCount", x.Approved), ("netSalesAfterDiscount", 0m), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, section, request, new Dictionary<string, object?> { ["totalDiscounts"] = rows.Sum(x => x.Amount), ["transactionsWithDiscount"] = rows.Sum(x => x.Count), ["averageDiscountAmount"] = rows.Sum(x => x.Count) == 0 ? 0 : rows.Sum(x => x.Amount) / rows.Sum(x => x.Count), ["managerApprovedDiscountCount"] = rows.Sum(x => x.Approved) }, records, records.Count);
    }
    private static IReadOnlyDictionary<string, object?> BuildSalesSummary(IReadOnlyList<OrderProjection> orders)
    {
        var gross = orders.Sum(x => x.SubtotalAmount);
        var discounts = orders.Sum(x => x.DiscountAmount);
        var refunds = orders.Sum(x => x.RefundedAmount);
        var net = orders.Sum(x => x.TotalAmount - x.RefundedAmount);
        var count = orders.Count(x => x.OrderStatus == CompletedStatus);
        return new Dictionary<string, object?> { ["grossSales"] = gross, ["netSales"] = net, ["transactionCount"] = count, ["averageOrderValue"] = count == 0 ? 0 : net / count, ["totalDiscounts"] = discounts, ["totalTax"] = orders.Sum(x => x.TaxAmount), ["totalRefunds"] = refunds, ["totalCollected"] = orders.Sum(x => x.PaidAmount) };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildDailyRows(IReadOnlyList<OrderProjection> orders) =>
        BuildDailyTrend(orders);

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildDailyTrend(IReadOnlyList<OrderProjection> orders) =>
        orders.GroupBy(x => x.BusinessDate).OrderBy(x => x.Key).Select(g => Row(("businessDate", g.Key), ("grossSalesAmount", g.Sum(x => x.SubtotalAmount)), ("discountAmount", g.Sum(x => x.DiscountAmount)), ("refundAmount", g.Sum(x => x.RefundedAmount)), ("taxAmount", g.Sum(x => x.TaxAmount)), ("netSalesAmount", g.Sum(x => x.TotalAmount - x.RefundedAmount)), ("totalCollectedAmount", g.Sum(x => x.PaidAmount)), ("transactionCount", g.Count()), ("averageOrderValue", g.Count() == 0 ? 0 : g.Sum(x => x.TotalAmount - x.RefundedAmount) / g.Count()), ("currencyCode", g.First().CurrencyCode))).ToList();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildCashierRows(IReadOnlyList<OrderProjection> orders) =>
        orders.GroupBy(x => new { x.CashierId, x.CashierName, x.ReportingOutletId, x.OutletName }).Select(g => Row(("cashierId", g.Key.CashierId), ("cashierName", g.Key.CashierName), ("outletId", g.Key.ReportingOutletId), ("outletName", g.Key.OutletName), ("transactionCount", g.Count()), ("grossSalesAmount", g.Sum(x => x.SubtotalAmount)), ("discountAmount", g.Sum(x => x.DiscountAmount)), ("refundAmount", g.Sum(x => x.RefundedAmount)), ("netSalesAmount", g.Sum(x => x.TotalAmount - x.RefundedAmount)), ("averageOrderValue", g.Count() == 0 ? 0 : g.Sum(x => x.TotalAmount - x.RefundedAmount) / g.Count()), ("cashDifference", 0m), ("currencyCode", g.First().CurrencyCode))).ToList();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildOutletPerformance(IReadOnlyList<OrderProjection> orders) =>
        orders.GroupBy(x => new { x.ReportingOutletId, x.OutletName }).Select(g => Row(("outletId", g.Key.ReportingOutletId), ("outletName", g.Key.OutletName), ("grossSalesAmount", g.Sum(x => x.SubtotalAmount)), ("netSalesAmount", g.Sum(x => x.TotalAmount - x.RefundedAmount)), ("transactionCount", g.Count()), ("averageOrderValue", g.Count() == 0 ? 0 : g.Sum(x => x.TotalAmount - x.RefundedAmount) / g.Count()), ("currencyCode", g.First().CurrencyCode))).ToList();

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildPaymentBreakdownAsync(TenantRequestContext context, ReportQueryRequest request, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var tenant = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var report = await BuildPaymentEventsAsync(tenant, "payments", request with { Page = 1, PageSize = int.MaxValue }, context, cancellationToken);
        return report.Records.Select(x => (IReadOnlyDictionary<string, object?>)Row(
            ("paymentMethodName", x["paymentMethodName"]), ("amount", x["netCollectedAmount"]))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildTopProductsAsync(Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken) =>
        (await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId.HasValue && orderIds.Contains(x.SalesOrderId.Value))
            .GroupBy(x => new { x.ProductId, x.ProductNameSnapshot })
            .Select(g => new { g.Key.ProductId, g.Key.ProductNameSnapshot, Quantity = g.Sum(x => x.Quantity), Net = g.Sum(x => x.LineTotalAmount) })
            .OrderByDescending(x => x.Net)
            .Take(10)
            .ToListAsync(cancellationToken))
        .Select(x => Row(("productId", x.ProductId), ("productName", x.ProductNameSnapshot), ("quantitySold", x.Quantity), ("netSalesAmount", x.Net))).ToList();

    private async Task<Dictionary<Guid, string>> GetPaymentMethodNamesAsync(Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await (from payment in _dbContext.SalesPayments.AsNoTracking()
                          join method in _dbContext.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
                          where payment.TenantId == tenantId && orderIds.Contains(payment.SalesOrderId)
                          select new { payment.SalesOrderId, method.MethodName }).ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.SalesOrderId).ToDictionary(x => x.Key, x => string.Join(", ", x.Select(v => v.MethodName).Distinct()));
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetDiscountRowsAsync(Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken) =>
        (await _dbContext.SalesOrderDiscounts.AsNoTracking().Where(x => x.TenantId == tenantId && orderIds.Contains(x.SalesOrderId)).ToListAsync(cancellationToken))
        .Select(x => Row(("discountName", x.DiscountNameSnapshot), ("discountCode", x.DiscountCodeSnapshot), ("discountScope", x.DiscountTargetScope), ("discountAmount", x.DiscountAmount), ("appliedAt", x.AppliedAt))).ToList();

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetTaxRowsAsync(Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken) =>
        (await _dbContext.SalesOrderTaxes.AsNoTracking().Where(x => x.TenantId == tenantId && orderIds.Contains(x.SalesOrderId)).ToListAsync(cancellationToken))
        .Select(x => Row(("taxClassName", x.TaxClassCodeSnapshot), ("taxName", x.TaxNameSnapshot), ("taxRate", x.TaxRatePercent), ("taxableAmount", x.TaxableAmount), ("taxAmount", x.TaxAmount), ("isTaxIncluded", x.IsTaxIncluded))).ToList();

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetReturnRowsAsync(Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken) =>
        (await (from salesReturn in _dbContext.SalesReturns.AsNoTracking()
                join refund in _dbContext.SalesRefunds.AsNoTracking() on salesReturn.Id equals refund.SalesReturnId into refunds
                from refund in refunds.DefaultIfEmpty()
                where salesReturn.TenantId == tenantId && orderIds.Contains(salesReturn.SalesOrderId)
                select new { salesReturn, refund }).ToListAsync(cancellationToken))
        .Select(x => Row(("returnId", x.salesReturn.Id), ("returnNumber", x.salesReturn.ReturnNumber), ("originalOrderId", x.salesReturn.SalesOrderId), ("processingOutletId", x.salesReturn.OutletId), ("processingOutletName", x.salesReturn.ProcessingOutletNameSnapshot), ("returnReasonCode", x.salesReturn.ReturnReasonCodeSnapshot), ("returnReasonName", x.salesReturn.ReturnReasonNameSnapshot), ("requestedQuantity", x.salesReturn.TotalRequestedQty), ("receivedQuantity", x.salesReturn.TotalReceivedQty), ("approvedQuantity", x.salesReturn.TotalApprovedQty), ("approvedAmount", x.refund == null ? 0m : x.refund.ApprovedAmount), ("refundedAmount", x.refund == null ? 0m : x.refund.RefundedAmount), ("returnStatus", x.salesReturn.ReturnStatus), ("refundStatus", x.refund == null ? null : x.refund.RefundStatus), ("completedAt", x.salesReturn.CompletedAt), ("currencyCode", x.refund == null ? null : x.refund.CurrencyCode))).ToList();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildNotes(OrderProjection order)
    {
        var notes = new List<IReadOnlyDictionary<string, object?>>();
        if (!string.IsNullOrWhiteSpace(order.CustomerNote)) notes.Add(Row(("type", "customer"), ("text", order.CustomerNote)));
        if (!string.IsNullOrWhiteSpace(order.InternalNote)) notes.Add(Row(("type", "internal"), ("text", order.InternalNote)));
        return notes;
    }

    private static IQueryable<OrderProjection> ApplyOrderSort(IQueryable<OrderProjection> query, string? sortBy, string? direction)
    {
        var desc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy switch
        {
            "orderNumber" => desc ? query.OrderByDescending(x => x.OrderNumber) : query.OrderBy(x => x.OrderNumber),
            "totalAmount" => desc ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            "businessDate" => desc ? query.OrderByDescending(x => x.BusinessDate) : query.OrderBy(x => x.BusinessDate),
            _ => query.OrderByDescending(x => x.CompletedAt)
        };
    }

    private ReportResultDto BuildDictionaryResult(TenantInfo tenantInfo, string section, ReportQueryRequest request, IReadOnlyList<IReadOnlyDictionary<string, object?>> records) =>
        Result(tenantInfo, section, request, new Dictionary<string, object?>(), records, records.Count);

    private static ReportResultDto Result(TenantInfo tenantInfo, string section, ReportQueryRequest request, IReadOnlyDictionary<string, object?> summary, IReadOnlyList<IReadOnlyDictionary<string, object?>> records, int total) =>
        new(section, tenantInfo.CurrencyCode, tenantInfo.Timezone, request.From, request.To, summary, new Dictionary<string, object?>(), records, new ReportPageDto(request.Page, request.PageSize, total, (int)Math.Ceiling(total / (double)request.PageSize)), DateTimeOffset.UtcNow);

    private async Task<TenantInfo> GetTenantInfoAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants.AsNoTracking().Where(x => x.Id == tenantId).Select(x => new { x.Id, x.BaseCurrencyCode, x.DefaultTimezone }).FirstAsync(cancellationToken);
        return new TenantInfo(tenant.Id, string.IsNullOrWhiteSpace(tenant.DefaultTimezone) ? "UTC" : tenant.DefaultTimezone, string.IsNullOrWhiteSpace(tenant.BaseCurrencyCode) ? "LKR" : tenant.BaseCurrencyCode, "en-LK");
    }

    private async Task<List<Guid>> GetAccessibleOutletIdsAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var user = await _dbContext.TenantUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == context.UserId && x.TenantId == context.TenantId, cancellationToken);
        if (user == null || user.AccountStatus != ActiveStatus || user.OutletAccessScope == E_POS.Domain.Modules.Tenant.AccessControl.Constants.TenantUserAccessScopes.NoOutletAccess)
        {
            return new List<Guid>();
        }

        if (user.OutletAccessScope == E_POS.Domain.Modules.Tenant.AccessControl.Constants.TenantUserAccessScopes.AllOutlets)
        {
            return await _dbContext.Outlets.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.Status != "DELETED").Select(x => x.Id).ToListAsync(cancellationToken);
        }

        return await _dbContext.OutletUserRoles.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.TenantUserId == context.UserId && x.RevokedAt == null).Select(x => x.OutletId)
            .Union(_dbContext.OutletUserPermissions.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.TenantUserId == context.UserId && x.RevokedAt == null).Select(x => x.OutletId))
            .Distinct().ToListAsync(cancellationToken);
    }

    private async Task<(List<Guid> TillIds, string Scope)> GetAccessibleTillIdsAsync(TenantRequestContext context, List<Guid> accessibleOutletIds, CancellationToken cancellationToken)
    {
        var user = await _dbContext.TenantUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == context.UserId && x.TenantId == context.TenantId, cancellationToken);
        if (user == null || user.AccountStatus != ActiveStatus || user.TillAccessScope == E_POS.Domain.Modules.Tenant.AccessControl.Constants.TenantUserAccessScopes.NoTillAccess || accessibleOutletIds.Count == 0)
        {
            return (new List<Guid>(), E_POS.Domain.Modules.Tenant.AccessControl.Constants.TenantUserAccessScopes.NoTillAccess);
        }

        var tillsInOutlets = _dbContext.Tills.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.Status != "DELETED" && accessibleOutletIds.Contains(x.OutletId)).Select(x => x.Id);

        if (user.TillAccessScope == E_POS.Domain.Modules.Tenant.AccessControl.Constants.TenantUserAccessScopes.AllAccessibleTills)
        {
            return (await tillsInOutlets.ToListAsync(cancellationToken), user.TillAccessScope);
        }

        var assignedTills = await _dbContext.TenantUserTillAccess.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.TenantUserId == context.UserId && x.RevokedAt == null).Select(x => x.TillId)
            .Distinct().ToListAsync(cancellationToken);

        return (assignedTills.Intersect(await tillsInOutlets.ToListAsync(cancellationToken)).ToList(), user.TillAccessScope);
    }

    /// <summary>
    /// Cashiers visible to the caller: users granted a role/permission at one of the caller's outlets,
    /// or who have recorded sales there. Email is not exposed as an option label.
    /// </summary>
    private async Task<IReadOnlyList<ReportFilterOptionDto>> GetCashierOptionsAsync(TenantRequestContext context,
        List<Guid> accessibleOutletIds, Guid? outletId, CancellationToken cancellationToken)
    {
        var outlets = outletId.HasValue ? accessibleOutletIds.Where(x => x == outletId.Value).ToList() : accessibleOutletIds;
        var tenantId = context.TenantId;
        var outletUsers = _dbContext.OutletUserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.RevokedAt == null && outlets.Contains(x.OutletId)).Select(x => x.TenantUserId)
            .Union(_dbContext.OutletUserPermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.RevokedAt == null && outlets.Contains(x.OutletId)).Select(x => x.TenantUserId))
            .Union(_dbContext.SalesOrders.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CreatedByTenantUserId != null && x.ReportingOutletId != null && outlets.Contains(x.ReportingOutletId.Value))
                .Select(x => x.CreatedByTenantUserId!.Value));
        var ids = await outletUsers.Distinct().ToListAsync(cancellationToken);
        return await _dbContext.TenantUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.StaffCode, x.DisplayName ?? x.FullName, x.AccountStatus, null, null, x.AccountStatus == ActiveStatus))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<ReportFilterOptionDto> StaticOptions(params string[] values) =>
        values.Select(x => new ReportFilterOptionDto(null, x, x, ActiveStatus, null, null, true)).ToList();

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(x => x.Key, x => x.Value);

    private sealed record TenantInfo(Guid TenantId, string Timezone, string CurrencyCode, string Locale);

    private sealed class OrderProjection
    {
        public Guid Id { get; init; }
        public Guid TenantId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string? ExternalOrderReference { get; init; }
        public DateOnly? BusinessDate { get; init; }
        public DateTimeOffset? PlacedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public DateTimeOffset? PostingAt { get; init; }
        public string OrderType { get; init; } = string.Empty;
        public DateTimeOffset? RequestedCollectionAt { get; init; }
        public DateTimeOffset? CancelledAt { get; init; }
        public Guid? UpdatedByUserId { get; init; }
        public string? ChannelCode { get; init; }
        public string? TillType { get; init; }
        public Guid SalesChannelId { get; init; }
        public string SalesChannelName { get; init; } = string.Empty;
        public Guid? ReportingOutletId { get; init; }
        public string? OutletName { get; init; }
        public Guid? TillId { get; init; }
        public string? TillCode { get; init; }
        public string? TillName { get; init; }
        public Guid? TillSessionId { get; init; }
        public Guid? CashierId { get; init; }
        public string? CashierName { get; init; }
        public Guid? CustomerId { get; init; }
        public string? CustomerNameSnapshot { get; init; }
        public string? CustomerEmailSnapshot { get; init; }
        public string? CustomerPhoneSnapshot { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public decimal SubtotalAmount { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal TaxAmount { get; init; }
        public decimal ChargeAmount { get; init; }
        public decimal RoundingAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RefundedAmount { get; init; }
        public string PaymentStatus { get; init; } = string.Empty;
        public string FulfillmentStatus { get; init; } = string.Empty;
        public string OrderStatus { get; init; } = string.Empty;
        public string? CustomerNote { get; init; }
        public string? InternalNote { get; init; }
    }
}















