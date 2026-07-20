using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed class TenantAdminReportsRepository : ITenantAdminReportsRepository
{
    private const string ActiveStatus = "ACTIVE";
    private const string CompletedStatus = "COMPLETED";
    private readonly EPosDbContext _dbContext;

    public TenantAdminReportsRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var tills = await _dbContext.Tills.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId &&
                        outletIds.Contains(x.OutletId) &&
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
                        (!request.DepartmentId.HasValue || x.DepartmentId == request.DepartmentId.Value) &&
                        x.ParentCategoryId == null &&
                        (includeInactive || x.Status == ActiveStatus))
            .OrderBy(x => x.CategoryName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), x.CategoryCode, x.CategoryName, x.Status, x.DepartmentId.ToString(), null, x.Status == ActiveStatus))
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
            ["cashiers"] = await GetCashierOptionsAsync(context.TenantId, cancellationToken),
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
            ["orderStatuses"] = StaticOptions("COMPLETED", "DRAFT", "CANCELLED", "VOIDED"),
            ["paymentStatuses"] = StaticOptions("PAID", "PARTIALLY_REFUNDED", "REFUNDED", "UNPAID"),
            ["stockStatuses"] = StaticOptions("IN_STOCK", "LOW_STOCK", "OUT_OF_STOCK"),
            ["expiryStatuses"] = StaticOptions("VALID", "EXPIRING_SOON", "EXPIRED", "NOT_APPLICABLE"),
            ["movementTypes"] = StaticOptions("SALE", "RETURN", "ADJUSTMENT", "TRANSFER")
        };

        return new ReportFilterOptionsResponse(
            ReportBusinessDateCalculator.FromInstant(DateTimeOffset.UtcNow, tenantInfo.Timezone),
            tenantInfo.Timezone,
            tenantInfo.CurrencyCode,
            tenantInfo.Locale,
            groups);
    }

    public async Task<ReportResultDto> GetDashboardAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var orders = ApplySalesFilters(await BuildOrderQueryAsync(context, request, cancellationToken), request);
        var currentOrders = await orders.ToListAsync(cancellationToken);
        var summary = BuildSalesSummary(currentOrders);
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

    public async Task<ReportResultDto> GetSalesAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var section = request.Section ?? "summary";
        var orders = ApplySalesFilters(await BuildOrderQueryAsync(context, request, cancellationToken), request);
        var orderRows = await orders.ToListAsync(cancellationToken);
        var orderIds = orderRows.Select(x => x.Id).ToList();

        return section switch
        {
            "transactions" => await BuildTransactionsResultAsync(tenantInfo, section, request, orders, cancellationToken),
            "products" => await BuildProductSalesResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),
            "categories" => await BuildCategorySalesResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),
            "payments" => await BuildPaymentResultAsync(tenantInfo, section, request, context, orderIds, cancellationToken),
            "tax" => await BuildTaxResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),
            "discounts" => await BuildDiscountResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),
            "returns" => await BuildReturnsResultAsync(tenantInfo, section, request, context, orderIds, cancellationToken),
            "cashiers" => BuildDictionaryResult(tenantInfo, section, request, BuildCashierRows(orderRows)),
            "daily" => BuildDictionaryResult(tenantInfo, section, request, BuildDailyRows(orderRows)),
            _ => new ReportResultDto(section, tenantInfo.CurrencyCode, tenantInfo.Timezone, request.From, request.To, BuildSalesSummary(orderRows),
                new Dictionary<string, object?> { ["salesTrend"] = BuildDailyTrend(orderRows), ["paymentBreakdown"] = await BuildPaymentBreakdownAsync(context, request, orderIds, cancellationToken), ["topSellingProducts"] = await BuildTopProductsAsync(context.TenantId, orderIds, cancellationToken) },
                [], null, DateTimeOffset.UtcNow)
        };
    }

    public async Task<ReportResultDto> GetStockAsync(
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantInfo = await GetTenantInfoAsync(context.TenantId, cancellationToken);
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var canViewValue = context.HasPermission(TenantAdminStockPermissions.ValueView);
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
                batch.ExpiryDate.GetValueOrDefault() < tenantToday ? "EXPIRED" :
                batch.ExpiryDate.GetValueOrDefault() <= tenantToday.AddDays(30) ? "EXPIRING_SOON" : "VALID"
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

        var summary = new Dictionary<string, object?>
        {
            ["totalStockQuantity"] = page.Sum(x => x.balance.OnHandQuantity),
            ["availableQuantity"] = page.Sum(x => x.balance.AvailableQuantity),
            ["reservedQuantity"] = page.Sum(x => x.balance.ReservedQuantity),
            ["damagedQuantity"] = page.Sum(x => x.balance.DamagedQuantity),
            ["quarantineQuantity"] = page.Sum(x => x.balance.QuarantineQuantity),
            ["lowStockItemCount"] = page.Count(x => x.stockStatus == "LOW_STOCK"),
            ["outOfStockItemCount"] = page.Count(x => x.stockStatus == "OUT_OF_STOCK"),
            ["expiringSoonItemCount"] = page.Count(x => x.expiryStatus == "EXPIRING_SOON"),
            ["totalStockValue"] = canViewValue ? costByBalance.Values.Sum(x => x.Value) : null
        };
        return Result(tenantInfo, section, request, summary, records, total);
    }

    public async Task<ReportResultDto> GetOutletsAsync(
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
        var query =
            from order in _dbContext.SalesOrders.AsNoTracking()
            join channel in _dbContext.SalesChannels.AsNoTracking() on order.SalesChannelId equals channel.Id
            join till in _dbContext.Tills.AsNoTracking() on order.TillId equals till.Id into tills
            from till in tills.DefaultIfEmpty()
            join user in _dbContext.TenantUsers.AsNoTracking() on order.CreatedByTenantUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            let effectiveOutletId = order.ReportingOutletId ?? (till == null ? null : till.OutletId)
            where order.TenantId == context.TenantId &&
                  (effectiveOutletId == null || outletIds.Contains(effectiveOutletId.GetValueOrDefault()))
            select new OrderProjection
            {
                Id = order.Id,
                TenantId = order.TenantId,
                OrderNumber = order.OrderNumber,
                ExternalOrderReference = order.ExternalOrderReference,
                BusinessDate = order.BusinessDate,
                PlacedAt = order.PlacedAt,
                CompletedAt = order.CompletedAt,
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

    private async Task<ReportResultDto> BuildTransactionsResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, IQueryable<OrderProjection> query, CancellationToken cancellationToken)
    {
        query = ApplyOrderSort(query, request.SortBy, request.SortDirection);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        var orderIds = page.Select(x => x.Id).ToList();
        var lineCounts = await _dbContext.SalesOrderLines.AsNoTracking().Where(x => x.TenantId == tenantInfo.TenantId && x.SalesOrderId.HasValue && orderIds.Contains(x.SalesOrderId.Value)).GroupBy(x => x.SalesOrderId!.Value).Select(g => new { Id = g.Key, Count = g.Count(), Qty = g.Sum(x => x.Quantity) }).ToListAsync(cancellationToken);
        var paymentNames = await GetPaymentMethodNamesAsync(tenantInfo.TenantId, orderIds, cancellationToken);
        var records = page.Select(x =>
        {
            var lines = lineCounts.FirstOrDefault(l => l.Id == x.Id);
            return Row(
                ("orderId", x.Id), ("orderNumber", x.OrderNumber), ("externalReference", x.ExternalOrderReference),
                ("businessDate", x.BusinessDate), ("placedAt", x.PlacedAt), ("completedAt", x.CompletedAt),
                ("salesChannelId", x.SalesChannelId), ("salesChannelName", x.SalesChannelName),
                ("outletId", x.ReportingOutletId), ("outletName", x.OutletName), ("tillId", x.TillId),
                ("tillCode", x.TillCode), ("tillName", x.TillName), ("tillSessionId", x.TillSessionId),
                ("cashierId", x.CashierId), ("cashierName", x.CashierName), ("customerId", x.CustomerId),
                ("customerName", x.CustomerNameSnapshot), ("lineCount", lines?.Count ?? 0), ("totalQuantity", lines?.Qty ?? 0),
                ("currencyCode", x.CurrencyCode), ("subtotalAmount", x.SubtotalAmount), ("discountAmount", x.DiscountAmount),
                ("taxAmount", x.TaxAmount), ("chargeAmount", x.ChargeAmount), ("roundingAmount", x.RoundingAmount),
                ("totalAmount", x.TotalAmount), ("paidAmount", x.PaidAmount), ("refundedAmount", x.RefundedAmount),
                ("netAmount", x.TotalAmount - x.RefundedAmount), ("paymentMethodNames", paymentNames.GetValueOrDefault(x.Id, "")),
                ("paymentStatus", x.PaymentStatus), ("fulfilmentStatus", x.FulfillmentStatus), ("orderStatus", x.OrderStatus));
        }).ToList();
        return Result(tenantInfo, section, request, BuildSalesSummary(page), records, total);
    }

    private async Task<ReportResultDto> BuildProductSalesResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId.HasValue && orderIds.Contains(x.SalesOrderId.Value))
            .GroupBy(x => new { x.ProductId, x.ProductVariantId, x.ProductNameSnapshot, x.VariantNameSnapshot, x.SkuSnapshot, x.BarcodeSnapshot, x.BrandNameSnapshot, x.DepartmentNameSnapshot, x.CategoryNameSnapshot, x.SubcategoryNameSnapshot })
            .Select(g => new
            {
                g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                QuantityReturned = g.Sum(x => x.ReturnedQuantity),
                Gross = g.Sum(x => x.LineSubtotalAmount),
                Discount = g.Sum(x => x.LineDiscountAmount),
                Tax = g.Sum(x => x.LineTaxAmount),
                Transactions = g.Select(x => x.SalesOrderId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);
        var records = rows.Select(x => Row(("productId", x.Key.ProductId), ("productName", x.Key.ProductNameSnapshot), ("productVariantId", x.Key.ProductVariantId), ("variantName", x.Key.VariantNameSnapshot), ("sku", x.Key.SkuSnapshot), ("barcode", x.Key.BarcodeSnapshot), ("brandName", x.Key.BrandNameSnapshot), ("departmentName", x.Key.DepartmentNameSnapshot), ("categoryName", x.Key.CategoryNameSnapshot), ("subcategoryName", x.Key.SubcategoryNameSnapshot), ("quantitySold", x.QuantitySold), ("quantityReturned", x.QuantityReturned), ("netQuantity", x.QuantitySold - x.QuantityReturned), ("grossSalesAmount", x.Gross), ("discountAmount", x.Discount), ("taxAmount", x.Tax), ("refundAmount", 0m), ("netSalesAmount", x.Gross - x.Discount + x.Tax), ("transactionCount", x.Transactions), ("averageSellingPrice", x.QuantitySold - x.QuantityReturned == 0 ? 0 : (x.Gross - x.Discount + x.Tax) / (x.QuantitySold - x.QuantityReturned)), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, section, request, new Dictionary<string, object?>(), records, records.Count);
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

    private async Task<ReportResultDto> BuildPaymentResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, TenantRequestContext context, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await (from payment in _dbContext.SalesPayments.AsNoTracking()
                          join method in _dbContext.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
                          where payment.TenantId == context.TenantId && orderIds.Contains(payment.SalesOrderId) && payment.PaymentStatus != "FAILED"
                          group new { payment, method } by new { payment.PaymentMethodId, method.MethodCode, method.MethodName, method.MethodType } into g
                          select new { g.Key, Count = g.Count(), Requested = g.Sum(x => x.payment.RequestedAmount), Tendered = g.Sum(x => x.payment.TenderedAmount ?? 0), Paid = g.Sum(x => x.payment.PaidAmount), Change = g.Sum(x => x.payment.ChangeAmount), Refunded = g.Sum(x => x.payment.RefundedAmount) })
            .ToListAsync(cancellationToken);
        var total = rows.Sum(x => x.Paid - x.Refunded - x.Change);
        var records = rows.Select(x => Row(("paymentMethodId", x.Key.PaymentMethodId), ("paymentMethodCode", x.Key.MethodCode), ("paymentMethodName", x.Key.MethodName), ("paymentType", x.Key.MethodType), ("transactionCount", x.Count), ("requestedAmount", x.Requested), ("tenderedAmount", x.Tendered), ("paidAmount", x.Paid), ("changeAmount", x.Change), ("refundedAmount", x.Refunded), ("netCollectedAmount", x.Paid - x.Refunded - x.Change), ("percentage", total == 0 ? 0 : (x.Paid - x.Refunded - x.Change) / total * 100), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, section, request, new Dictionary<string, object?> { ["totalCollected"] = rows.Sum(x => x.Paid), ["refundedAmount"] = rows.Sum(x => x.Refunded), ["netCollected"] = total, ["transactionCount"] = rows.Sum(x => x.Count) }, records, records.Count);
    }

    private async Task<ReportResultDto> BuildTaxResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.SalesOrderTaxes.AsNoTracking()
            .Where(x => x.TenantId == tenantId && orderIds.Contains(x.SalesOrderId))
            .GroupBy(x => new { x.TaxClassId, x.TaxClassCodeSnapshot, x.TaxRateCodeSnapshot, x.TaxNameSnapshot, x.TaxRatePercent })
            .Select(g => new { g.Key, Taxable = g.Sum(x => x.TaxableAmount), Tax = g.Sum(x => x.TaxAmount), Transactions = g.Select(x => x.SalesOrderId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var records = rows.Select(x => Row(("taxClassId", x.Key.TaxClassId), ("taxClassName", x.Key.TaxClassCodeSnapshot), ("taxCode", x.Key.TaxRateCodeSnapshot), ("taxName", x.Key.TaxNameSnapshot), ("taxRate", x.Key.TaxRatePercent), ("taxableAmount", x.Taxable), ("taxAmount", x.Tax), ("refundedTaxAmount", 0m), ("netTaxAmount", x.Tax), ("transactionCount", x.Transactions), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, section, request, new Dictionary<string, object?> { ["taxableSales"] = rows.Sum(x => x.Taxable), ["taxCollected"] = rows.Sum(x => x.Tax), ["refundedTax"] = 0m, ["netTax"] = rows.Sum(x => x.Tax) }, records, records.Count);
    }

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

    private async Task<ReportResultDto> BuildReturnsResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, TenantRequestContext context, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await GetReturnRowsAsync(context.TenantId, orderIds, cancellationToken);
        return Result(tenantInfo, section, request, new Dictionary<string, object?> { ["returnCount"] = rows.Count, ["completedRefundAmount"] = rows.Sum(x => x.TryGetValue("refundedAmount", out var value) ? (decimal)value! : 0m) }, rows, rows.Count);
    }

    private async Task<ReportResultDto> BuildStockMovementsResultAsync(
        TenantInfo tenantInfo,
        TenantRequestContext context,
        ReportQueryRequest request,
        List<Guid> outletIds,
        bool canViewValue,
        CancellationToken cancellationToken)
    {
        var query =
            from movement in _dbContext.StockMovements.AsNoTracking()
            join balance in _dbContext.InventoryBalances.AsNoTracking() on movement.InventoryBalanceId equals balance.Id
            join location in _dbContext.InventoryLocations.AsNoTracking() on balance.InventoryLocationId equals location.Id
            join outlet in _dbContext.Outlets.AsNoTracking() on location.OutletId equals outlet.Id
            join product in _dbContext.Products.AsNoTracking() on balance.ProductId equals product.Id
            join variant in _dbContext.ProductVariants.AsNoTracking() on balance.ProductVariantId equals variant.Id into variants
            from variant in variants.DefaultIfEmpty()
            join batch in _dbContext.ProductBatches.AsNoTracking() on balance.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            join user in _dbContext.TenantUsers.AsNoTracking() on movement.CreatedByTenantUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            where movement.TenantId == context.TenantId &&
                  outletIds.Contains(location.OutletId) &&
                  (!request.OutletId.HasValue || location.OutletId == request.OutletId.Value) &&
                  (!request.InventoryLocationId.HasValue || location.Id == request.InventoryLocationId.Value) &&
                  (!request.ProductId.HasValue || balance.ProductId == request.ProductId.Value) &&
                  (!request.ProductVariantId.HasValue || balance.ProductVariantId == request.ProductVariantId.Value) &&
                  (!string.IsNullOrWhiteSpace(request.MovementType) ? movement.MovementType == request.MovementType : true) &&
                  (!string.IsNullOrWhiteSpace(request.BatchNumber) ? batch != null && batch.BatchNumber.Contains(request.BatchNumber) : true) &&
                  (!string.IsNullOrWhiteSpace(request.Search) ? movement.MovementNumber.Contains(request.Search) || product.ProductName.Contains(request.Search) : true)
            select new { movement, balance, location, outlet, product, variant, batch, user };
        if (request.From.HasValue)
        {
            var range = ReportBusinessDateCalculator.ToUtcRange(request.From.Value, request.From.Value, tenantInfo.Timezone);
            query = query.Where(x => x.movement.OccurredAt >= range.FromUtc);
        }
        if (request.To.HasValue)
        {
            var range = ReportBusinessDateCalculator.ToUtcRange(request.To.Value, request.To.Value, tenantInfo.Timezone);
            query = query.Where(x => x.movement.OccurredAt < range.ToUtcExclusive);
        }

        var total = await query.CountAsync(cancellationToken);
        var page = await query.OrderByDescending(x => x.movement.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var records = page.Select(x => Row(("stockMovementId", x.movement.Id), ("movementNumber", x.movement.MovementNumber),
            ("movementAt", x.movement.OccurredAt), ("productId", x.product.Id), ("productName", x.product.ProductName),
            ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("outletId", x.outlet.Id),
            ("outletName", x.outlet.OutletName), ("inventoryLocationId", x.location.Id), ("inventoryLocationName", x.location.LocationName),
            ("productBatchId", x.batch?.Id), ("batchNumber", x.batch?.BatchNumber), ("movementType", x.movement.MovementType),
            ("quantityBefore", x.movement.QuantityBefore), ("quantityChange", x.movement.QuantityChange), ("quantityAfter", x.movement.QuantityAfter),
            ("unitCost", canViewValue ? x.movement.UnitCost : null), ("totalCost", canViewValue ? x.movement.TotalCost : null),
            ("referenceType", null), ("referenceNumber", x.movement.ReferenceNumberSnapshot), ("reasonCode", x.movement.ReasonCode),
            ("reason", x.movement.MovementNote), ("notes", x.movement.MovementNote), ("performedByUserId", x.movement.CreatedByTenantUserId),
            ("performedByUserName", x.user == null ? null : x.user.DisplayName ?? x.user.FullName), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        return Result(tenantInfo, "movements", request, new Dictionary<string, object?> { ["movementCount"] = total }, records, total);
    }

    private async Task<ReportResultDto> BuildTillSummaryResultAsync(
        TenantInfo tenantInfo,
        TenantRequestContext context,
        ReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var query =
            from session in _dbContext.TillSessions.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on session.OutletId equals outlet.Id
            join till in _dbContext.Tills.AsNoTracking() on session.TillId equals till.Id
            join summary in _dbContext.TillSessionSummaries.AsNoTracking() on session.Id equals summary.TillSessionId into summaries
            from summary in summaries.DefaultIfEmpty()
            join cashier in _dbContext.TenantUsers.AsNoTracking() on session.OpenedByTenantUserId equals cashier.Id into cashiers
            from cashier in cashiers.DefaultIfEmpty()
            where session.TenantId == context.TenantId &&
                  outletIds.Contains(session.OutletId) &&
                  (!request.OutletId.HasValue || session.OutletId == request.OutletId.Value) &&
                  (!request.TillId.HasValue || session.TillId == request.TillId.Value) &&
                  (!request.CashierId.HasValue || session.OpenedByTenantUserId == request.CashierId.Value)
            select new { session, outlet, till, summary, cashier };
        if (request.From.HasValue) query = query.Where(x => x.session.BusinessDate >= request.From.Value);
        if (request.To.HasValue) query = query.Where(x => x.session.BusinessDate <= request.To.Value);

        var total = await query.CountAsync(cancellationToken);
        var page = await query.OrderByDescending(x => x.session.BusinessDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var sessionIds = page.Select(x => x.session.Id).ToList();
        var cashMovements = await _dbContext.TillCashMovements.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && sessionIds.Contains(x.TillSessionId))
            .GroupBy(x => new { x.TillSessionId, x.MovementType })
            .Select(g => new { g.Key.TillSessionId, g.Key.MovementType, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);
        decimal Cash(Guid sessionId, params string[] movementTypes) =>
            cashMovements.Where(x => x.TillSessionId == sessionId && movementTypes.Contains(x.MovementType)).Sum(x => x.Amount);

        var records = page.Select(x => Row(("tillSessionId", x.session.Id), ("sessionNumber", x.session.SessionNumber),
            ("businessDate", x.session.BusinessDate), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName),
            ("tillId", x.till.Id), ("tillCode", x.till.TillCode), ("tillName", x.till.TillName),
            ("cashierId", x.session.OpenedByTenantUserId), ("cashierName", x.cashier == null ? null : x.cashier.DisplayName ?? x.cashier.FullName),
            ("openedAt", x.session.OpenedAt), ("closedAt", x.session.ClosedAt),
            ("openingCashAmount", x.summary == null ? x.session.OpeningFloatAmount : x.summary.OpeningCashAmount),
            ("cashInAmount", Cash(x.session.Id, "CASH_IN")), ("cashDropAmount", Cash(x.session.Id, "CASH_DROP", "CASH_OUT")),
            ("expectedCashAmount", x.summary == null ? null : x.summary.ExpectedCashAmount),
            ("countedCashAmount", x.summary == null ? null : x.summary.CountedCashAmount),
            ("cashDifference", x.summary == null ? null : x.summary.CashDifferenceAmount),
            ("grossSalesAmount", x.summary == null ? 0 : x.summary.GrossSalesAmount),
            ("discountAmount", x.summary == null ? 0 : x.summary.DiscountAmount),
            ("taxAmount", x.summary == null ? 0 : x.summary.TaxAmount),
            ("netSalesAmount", x.summary == null ? 0 : x.summary.NetSalesAmount),
            ("refundAmount", x.summary == null ? 0 : x.summary.RefundAmount),
            ("voidCount", x.summary == null ? 0 : x.summary.VoidCount),
            ("orderCount", x.summary == null ? 0 : x.summary.OrderCount),
            ("sessionStatus", x.session.Status), ("approvalStatus", x.summary != null && x.summary.ApprovedAt != null ? "APPROVED" : "PENDING"),
            ("currencyCode", x.session.CurrencyCode))).ToList();
        return Result(tenantInfo, "tills", request, new Dictionary<string, object?> { ["sessionCount"] = total }, records, total);
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

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildPaymentBreakdownAsync(TenantRequestContext context, ReportQueryRequest request, List<Guid> orderIds, CancellationToken cancellationToken) =>
        (await (from payment in _dbContext.SalesPayments.AsNoTracking()
                join method in _dbContext.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
                where payment.TenantId == context.TenantId && orderIds.Contains(payment.SalesOrderId) && payment.PaymentStatus != "FAILED"
                group payment by method.MethodName into g
                select new { Method = g.Key, Amount = g.Sum(x => x.PaidAmount - x.RefundedAmount - x.ChangeAmount) })
            .ToListAsync(cancellationToken))
        .Select(x => Row(("paymentMethodName", x.Method), ("amount", x.Amount))).ToList();

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
        var assigned = await _dbContext.OutletUserRoles.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.TenantUserId == context.UserId && x.RevokedAt == null).Select(x => x.OutletId)
            .Union(_dbContext.OutletUserPermissions.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.TenantUserId == context.UserId && x.RevokedAt == null).Select(x => x.OutletId))
            .Distinct().ToListAsync(cancellationToken);
        if (assigned.Count > 0) return assigned;
        return await _dbContext.Outlets.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.Status != "DELETED").Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ReportFilterOptionDto>> GetCashierOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.TenantUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AccountStatus == ActiveStatus)
            .OrderBy(x => x.FullName)
            .Select(x => new ReportFilterOptionDto(x.Id.ToString(), null, x.DisplayName ?? x.FullName, x.AccountStatus, null, x.Email, x.AccountStatus == ActiveStatus))
            .ToListAsync(cancellationToken);

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
