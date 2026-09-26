using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed partial class TenantAdminReportsRepository
{
    /// <summary>
    /// REP-07B. Opening is the signed ledger before From; the period is From &lt;= OccurredAt &lt; ToExclusive.
    /// Reconciliation totals use the scope filters only (outlet/location/product/variant/batch) so that
    /// Closing = Opening + Net Movement always holds; movement-type and search filters narrow the rows.
    /// </summary>
    private async Task<ReportResultDto> BuildStockMovementsResultAsync(TenantInfo tenantInfo, TenantRequestContext context,
        ReportQueryRequest request, List<Guid> outletIds, bool canViewValue, CancellationToken cancellationToken)
    {
        var scope =
            from movement in _dbContext.StockMovements.AsNoTracking()
            join balance in _dbContext.InventoryBalances.AsNoTracking() on movement.InventoryBalanceId equals balance.Id
            join location in _dbContext.InventoryLocations.AsNoTracking() on balance.InventoryLocationId equals location.Id
            join batch in _dbContext.ProductBatches.AsNoTracking() on balance.ProductBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            where movement.TenantId == context.TenantId && balance.TenantId == context.TenantId &&
                  outletIds.Contains(location.OutletId) &&
                  (!request.OutletId.HasValue || location.OutletId == request.OutletId.Value) &&
                  (!request.InventoryLocationId.HasValue || location.Id == request.InventoryLocationId.Value) &&
                  (!request.ProductId.HasValue || balance.ProductId == request.ProductId.Value) &&
                  (!request.ProductVariantId.HasValue || balance.ProductVariantId == request.ProductVariantId.Value) &&
                  (string.IsNullOrWhiteSpace(request.BatchNumber) || (batch != null && batch.BatchNumber.Contains(request.BatchNumber)))
            select new { movement, balance, location, batch };
        var (fromUtc, toUtc) = ReportBusinessDateCalculator.ResolveBusinessDateRange(request.From, request.To, tenantInfo.Timezone);
        var period = scope.Where(x => (!fromUtc.HasValue || x.movement.OccurredAt >= fromUtc.Value)
            && (!toUtc.HasValue || x.movement.OccurredAt < toUtc.Value));

        // Server-side aggregation: no ledger rows are materialised to build the totals.
        var openingByVariant = fromUtc.HasValue
            ? await scope.Where(x => x.movement.OccurredAt < fromUtc.Value)
                .GroupBy(x => new { x.location.OutletId, x.balance.ProductId, x.balance.ProductVariantId })
                .Select(g => new { g.Key.OutletId, g.Key.ProductId, g.Key.ProductVariantId, Quantity = g.Sum(x => x.movement.QuantityChange) })
                .ToListAsync(cancellationToken)
            : [];
        var periodByVariant = await period
            .GroupBy(x => new { x.location.OutletId, x.balance.ProductId, x.balance.ProductVariantId, x.movement.MovementType, Inbound = x.movement.QuantityChange >= 0 })
            .Select(g => new { g.Key.OutletId, g.Key.ProductId, g.Key.ProductVariantId, g.Key.MovementType, Quantity = g.Sum(x => x.movement.QuantityChange) })
            .ToListAsync(cancellationToken);

        var totals = ReportRules.CalculateStockReconciliation(openingByVariant.Sum(x => x.Quantity),
            periodByVariant.Select(x => (x.MovementType, x.Quantity)));
        var variantKeys = openingByVariant.Select(x => (x.OutletId, x.ProductId, x.ProductVariantId))
            .Union(periodByVariant.Select(x => (x.OutletId, x.ProductId, x.ProductVariantId))).ToList();
        var byVariant = variantKeys.Select(key =>
        {
            var r = ReportRules.CalculateStockReconciliation(
                openingByVariant.Where(x => (x.OutletId, x.ProductId, x.ProductVariantId) == key).Sum(x => x.Quantity),
                periodByVariant.Where(x => (x.OutletId, x.ProductId, x.ProductVariantId) == key).Select(x => (x.MovementType, x.Quantity)));
            return (IReadOnlyDictionary<string, object?>)Row(("outletId", key.OutletId), ("productId", key.ProductId), ("productVariantId", key.ProductVariantId),
                ("openingQuantity", r.Opening), ("receipts", r.Receipts), ("transferIn", r.TransferIn), ("restockableReturns", r.RestockableReturns),
                ("stockIssues", r.StockIssues), ("transferOut", r.TransferOut), ("signedAdjustments", r.SignedAdjustments),
                ("otherSignedMovements", r.OtherSignedMovements), ("netMovement", r.NetMovement), ("closingQuantity", r.Closing));
        }).ToList();

        var rowsQuery =
            from x in period
            join outlet in _dbContext.Outlets.AsNoTracking() on x.location.OutletId equals outlet.Id
            join product in _dbContext.Products.AsNoTracking() on x.balance.ProductId equals product.Id
            join variant in _dbContext.ProductVariants.AsNoTracking() on x.balance.ProductVariantId equals variant.Id into variants
            from variant in variants.DefaultIfEmpty()
            join uom in _dbContext.UnitOfMeasures.AsNoTracking() on variant.StockUomId equals uom.Id into uoms
            from uom in uoms.DefaultIfEmpty()
            join user in _dbContext.TenantUsers.AsNoTracking() on x.movement.CreatedByTenantUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            where (string.IsNullOrWhiteSpace(request.MovementType) || x.movement.MovementType == request.MovementType) &&
                  (string.IsNullOrWhiteSpace(request.Search) || x.movement.MovementNumber.Contains(request.Search) ||
                   product.ProductName.Contains(request.Search) || (variant != null && (variant.Sku ?? string.Empty).Contains(request.Search)))
            select new { x.movement, x.balance, x.location, x.batch, outlet, product, variant, UomCode = uom == null ? null : uom.UomCode, user };

        var total = await rowsQuery.CountAsync(cancellationToken);
        var page = await rowsQuery.OrderByDescending(x => x.movement.OccurredAt).ThenBy(x => x.movement.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var movementIds = page.Select(x => x.movement.Id).ToList();
        var references = await _dbContext.StockMovementReferences.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && movementIds.Contains(x.StockMovementId))
            .ToListAsync(cancellationToken);
        var records = page.Select(x =>
        {
            var bucket = ReportRules.MapStockMovement(x.movement.MovementType, x.movement.QuantityChange);
            var reference = references.FirstOrDefault(r => r.StockMovementId == x.movement.Id);
            return Row(("stockMovementId", x.movement.Id), ("movementNumber", x.movement.MovementNumber),
                ("movementAt", x.movement.OccurredAt), ("occurredAt", x.movement.OccurredAt), ("productId", x.product.Id), ("productName", x.product.ProductName),
                ("productVariantId", x.variant?.Id), ("variantName", x.variant?.VariantName), ("sku", x.variant?.Sku), ("unit", x.UomCode),
                ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName), ("inventoryLocationId", x.location.Id), ("inventoryLocationName", x.location.LocationName),
                ("productBatchId", x.batch?.Id), ("batchNumber", x.batch?.BatchNumber), ("movementType", x.movement.MovementType),
                ("reconciliationBucket", bucket.ToString().ToUpperInvariant()),
                ("quantityBefore", x.movement.QuantityBefore), ("quantityChange", x.movement.QuantityChange), ("quantityAfter", x.movement.QuantityAfter),
                ("unitCost", canViewValue ? x.movement.UnitCost : null), ("totalCost", canViewValue ? x.movement.TotalCost : null),
                ("referenceType", reference?.ReferenceType), ("referenceId", reference?.ReferenceId), ("referenceNumber", x.movement.ReferenceNumberSnapshot),
                // The ledger records one side of a transfer per location; the counterparty outlet is not stored.
                ("sourceOutletId", bucket == StockBucket.TransferOut ? x.outlet.Id : null),
                ("destinationOutletId", bucket == StockBucket.TransferIn ? x.outlet.Id : null),
                ("reasonCode", x.movement.ReasonCode), ("reason", x.movement.MovementNote), ("notes", x.movement.MovementNote),
                ("persistentEventId", x.movement.IdempotencyKey),
                ("performedByUserId", x.movement.CreatedByTenantUserId),
                ("performedByUserName", x.user == null ? null : x.user.DisplayName ?? x.user.FullName), ("currencyCode", tenantInfo.CurrencyCode));
        }).ToList();

        var result = Result(tenantInfo, "movements", request, Row(
            ("movementCount", total), ("openingQuantity", totals.Opening), ("receipts", totals.Receipts),
            ("transferIn", totals.TransferIn), ("restockableReturns", totals.RestockableReturns), ("stockIssues", totals.StockIssues),
            ("transferOut", totals.TransferOut), ("signedAdjustments", totals.SignedAdjustments),
            ("otherSignedMovements", totals.OtherSignedMovements), ("netMovement", totals.NetMovement), ("closingQuantity", totals.Closing),
            ("dateBasis", "STOCK LEDGER OCCURRED AT"),
            ("closingFormula", "opening + receipts + transferIn + restockableReturns - stockIssues - transferOut + signedAdjustments")), records, total);
        return result with { Sections = new Dictionary<string, object?> { ["reconciliationByVariant"] = byVariant } };
    }

    /// <summary>
    /// REP-03. Closed sessions report the original submitted CashReconciliation snapshot; activity recorded
    /// after that snapshot is surfaced as a separate revised figure and never overwrites the original count.
    /// </summary>
    private async Task<ReportResultDto> BuildTillSummaryResultAsync(TenantInfo tenantInfo, TenantRequestContext context,
        ReportQueryRequest request, CancellationToken cancellationToken)
    {
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var (tillIds, _) = await GetAccessibleTillIdsAsync(context, outletIds, cancellationToken);
        var query =
            from session in _dbContext.TillSessions.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on session.OutletId equals outlet.Id
            join till in _dbContext.Tills.AsNoTracking() on session.TillId equals till.Id
            join summary in _dbContext.TillSessionSummaries.AsNoTracking() on session.Id equals summary.TillSessionId into summaries
            from summary in summaries.DefaultIfEmpty()
            join cashier in _dbContext.TenantUsers.AsNoTracking() on session.OpenedByTenantUserId equals cashier.Id into cashiers
            from cashier in cashiers.DefaultIfEmpty()
            join closer in _dbContext.TenantUsers.AsNoTracking() on session.ClosedByTenantUserId equals closer.Id into closers
            from closer in closers.DefaultIfEmpty()
            where session.TenantId == context.TenantId &&
                  outletIds.Contains(session.OutletId) &&
                  tillIds.Contains(session.TillId) &&
                  (!request.OutletId.HasValue || session.OutletId == request.OutletId.Value) &&
                  (!request.TillId.HasValue || session.TillId == request.TillId.Value) &&
                  (!request.CashierId.HasValue || session.OpenedByTenantUserId == request.CashierId.Value)
            select new { session, outlet, till, summary, cashier, closer };
        // Sessions are selected by their business date but always report their full open-to-close activity.
        if (request.From.HasValue) query = query.Where(x => x.session.BusinessDate >= request.From.Value);
        if (request.To.HasValue) query = query.Where(x => x.session.BusinessDate <= request.To.Value);

        var total = await query.CountAsync(cancellationToken);
        var page = await query.OrderByDescending(x => x.session.BusinessDate).ThenByDescending(x => x.session.OpenedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var sessionIds = page.Select(x => x.session.Id).ToList();
        var reconciliations = (await _dbContext.CashReconciliations.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && sessionIds.Contains(x.TillSessionId))
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken))
            .GroupBy(x => x.TillSessionId).ToDictionary(g => g.Key, g => g.First());
        var live = await CalculateLiveCashAsync(context.TenantId, page.Select(x => (x.session.Id, x.session.OpeningFloatAmount, x.session.CurrencyCode)).ToList(), cancellationToken);
        var reviewerIds = reconciliations.Values.Where(x => x.ApprovedByTenantUserId.HasValue).Select(x => x.ApprovedByTenantUserId!.Value).Distinct().ToList();
        var reviewers = await _dbContext.TenantUsers.AsNoTracking().Where(x => x.TenantId == context.TenantId && reviewerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName ?? x.FullName, cancellationToken);

        var records = new List<Dictionary<string, object?>>();
        foreach (var x in page)
        {
            var current = live[x.session.Id];
            var row = Row(("tillSessionId", x.session.Id), ("sessionNumber", x.session.SessionNumber),
                ("businessDate", x.session.BusinessDate), ("outletId", x.outlet.Id), ("outletName", x.outlet.OutletName),
                ("tillId", x.till.Id), ("tillCode", x.till.TillCode), ("tillName", x.till.TillName),
                ("cashierId", x.session.OpenedByTenantUserId), ("cashierName", x.cashier == null ? null : x.cashier.DisplayName ?? x.cashier.FullName),
                ("responsibleStaffName", x.cashier == null ? null : x.cashier.DisplayName ?? x.cashier.FullName),
                ("closedByUserId", x.session.ClosedByTenantUserId), ("closedByName", x.closer == null ? null : x.closer.DisplayName ?? x.closer.FullName),
                ("openedAt", x.session.OpenedAt), ("closedAt", x.session.ClosedAt),
                ("grossSalesAmount", x.summary == null ? 0 : x.summary.GrossSalesAmount),
                ("discountAmount", x.summary == null ? 0 : x.summary.DiscountAmount),
                ("taxAmount", x.summary == null ? 0 : x.summary.TaxAmount),
                ("netSalesAmount", x.summary == null ? 0 : x.summary.NetSalesAmount),
                ("refundAmount", x.summary == null ? 0 : x.summary.RefundAmount),
                ("voidCount", x.summary == null ? 0 : x.summary.VoidCount),
                ("orderCount", x.summary == null ? 0 : x.summary.OrderCount),
                ("sessionStatus", x.session.Status), ("currencyCode", x.session.CurrencyCode));
            if (reconciliations.TryGetValue(x.session.Id, out var original))
            {
                var snapshot = ReadCloseSnapshot(original.CalculationDetailsJson, x.session.OpeningFloatAmount, original.ExpectedCashAmount);
                SetCashFigures(row, snapshot);
                row["expectedCashAmount"] = original.ExpectedCashAmount;
                row["countedCashAmount"] = original.CountedCashAmount;
                row["cashDifference"] = original.DifferenceAmount;
                row["varianceReason"] = original.DifferenceReason;
                row["closeReason"] = original.DifferenceReason;
                row["reconciliationId"] = original.Id;
                row["closeSnapshotAt"] = original.CreatedAt;
                row["reviewStatus"] = original.ReconciliationStatus;
                row["approvalStatus"] = original.ReconciliationStatus;
                row["reviewedBy"] = original.ApprovedByTenantUserId;
                row["reviewedByName"] = original.ApprovedByTenantUserId.HasValue ? reviewers.GetValueOrDefault(original.ApprovedByTenantUserId.Value) : null;
                var late = current.Activity.Where(a => a.CreatedAt > original.CreatedAt).ToList();
                row["lateActivityPaymentIds"] = late.Where(a => a.Kind == "PAYMENT").Select(a => a.Id).ToList();
                row["lateActivityMovementIds"] = late.Where(a => a.Kind == "MOVEMENT").Select(a => a.Id).ToList();
                row["requiresCorrectionReview"] = late.Count > 0;
                // Traceable correction: recomputed from current ledger, shown next to the untouched original.
                row["revisedExpectedCashAmount"] = late.Count > 0 ? current.Figures.ExpectedCash : null;
                row["revisedCashDifference"] = late.Count > 0 ? ReportRules.CalculateCashDifference(original.CountedCashAmount, current.Figures.ExpectedCash) : null;
                row["correctionDelta"] = late.Count > 0 ? current.Figures.ExpectedCash - original.ExpectedCashAmount : null;
            }
            else
            {
                // Open (or legacy unreconciled) session: live expected cash, no count yet.
                SetCashFigures(row, current.Figures);
                row["expectedCashAmount"] = current.Figures.ExpectedCash;
                row["countedCashAmount"] = null;
                row["cashDifference"] = null;
                row["varianceReason"] = null;
                row["reviewStatus"] = x.session.ClosedAt.HasValue ? "NOT_RECONCILED" : "OPEN";
                row["approvalStatus"] = row["reviewStatus"];
                row["requiresCorrectionReview"] = false;
            }
            records.Add(row);
        }
        var closed = records.Where(r => r["countedCashAmount"] is decimal).ToList();
        return Result(tenantInfo, "tills", request, Row(("sessionCount", total),
            ("closedSessionCount", closed.Count), ("totalCashDifference", closed.Sum(r => (decimal)r["cashDifference"]!)),
            ("sessionsWithDifference", closed.Count(r => (decimal)r["cashDifference"]! != 0m)),
            ("sessionsRequiringCorrectionReview", records.Count(r => r["requiresCorrectionReview"] is true)),
            ("dateBasis", "SESSION BUSINESS DATE (FULL SESSION ACTIVITY)"),
            ("expectedCashFormula", "openingFloat + cashReceipts - cashRefunds + otherCashIn - otherCashOut")), records, total);
    }

    private static void SetCashFigures(Dictionary<string, object?> row, CashFigures figures)
    {
        row["openingFloat"] = figures.OpeningFloat;
        row["openingCashAmount"] = figures.OpeningFloat;
        row["cashReceipts"] = figures.CashReceipts;
        row["cashRefunds"] = figures.CashRefunds;
        row["otherCashIn"] = figures.OtherCashIn;
        row["otherCashOut"] = figures.OtherCashOut;
        row["cashInAmount"] = figures.OtherCashIn;
        row["cashDropAmount"] = figures.OtherCashOut;
        row["calculatedExpectedCash"] = figures.ExpectedCash;
    }

    /// <summary>
    /// Reads the close-time calculation persisted by the till close (PosTillSessionRepository).
    /// Cash refunds are netted into its ExpectedCash, so they are recovered from the stored components.
    /// </summary>
    private static CashFigures ReadCloseSnapshot(string? json, decimal sessionOpeningFloat, decimal expectedCash)
    {
        decimal Value(JsonElement root, string name) => root.TryGetProperty(name, out var v) && v.TryGetDecimal(out var d) ? d : 0m;
        if (string.IsNullOrWhiteSpace(json)) return new(sessionOpeningFloat, 0m, 0m, 0m, 0m, expectedCash);
        using var details = JsonDocument.Parse(json);
        var root = details.RootElement;
        var opening = root.TryGetProperty("OpeningFloat", out _) ? Value(root, "OpeningFloat") : sessionOpeningFloat;
        var receipts = Value(root, "CashPayments");
        var otherIn = Value(root, "CashIn") + Value(root, "OpeningAdjustments");
        var otherOut = Value(root, "CashOut") + Value(root, "ClosingRemovals");
        var stored = root.TryGetProperty("ExpectedCash", out _) ? Value(root, "ExpectedCash") : expectedCash;
        var refunds = opening + receipts + otherIn - otherOut - stored;
        return new(opening, receipts, refunds, otherIn, otherOut, stored);
    }

    /// <summary>
    /// Current expected cash per session using the same sources as the till close: successful CASH payments
    /// (and their recorded refunds) plus manual/canonical cash movements. Sale cash that was also logged as a
    /// CASH_IN movement referencing the payment number is not counted twice.
    /// </summary>
    private async Task<Dictionary<Guid, (CashFigures Figures, List<(Guid Id, string Kind, DateTimeOffset CreatedAt)> Activity)>> CalculateLiveCashAsync(
        Guid tenantId, List<(Guid Id, decimal OpeningFloat, string CurrencyCode)> sessions, CancellationToken ct)
    {
        var ids = sessions.Select(x => x.Id).ToList();
        var successStatuses = ReportRules.SuccessfulPaymentStatuses;
        var payments = await (from p in _dbContext.SalesPayments.AsNoTracking()
                              join m in _dbContext.PaymentMethods.AsNoTracking() on p.PaymentMethodId equals m.Id
                              where p.TenantId == tenantId && p.TillSessionId.HasValue && ids.Contains(p.TillSessionId.Value)
                              select new { p.Id, SessionId = p.TillSessionId!.Value, p.PaymentNumber, p.PaidAmount, p.RefundedAmount, p.PaymentStatus,
                                  p.CurrencyCode, m.MethodCode, p.CreatedAt, p.UpdatedAt }).ToListAsync(ct);
        var movements = await _dbContext.TillCashMovements.AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.TillSessionId)).ToListAsync(ct);
        var canonical = await (from movement in _dbContext.CashMovements.AsNoTracking()
                               join type in _dbContext.CashMovementTypes.AsNoTracking() on movement.MovementTypeId equals type.Id
                               where movement.TenantId == tenantId && ids.Contains(movement.TillSessionId) && type.AffectsExpectedCash
                               select new { movement.Id, SessionId = movement.TillSessionId, movement.Amount, movement.CurrencyCode, type.Direction, movement.CreatedAt })
            .ToListAsync(ct);
        var result = new Dictionary<Guid, (CashFigures, List<(Guid, string, DateTimeOffset)>)>();
        foreach (var (id, opening, currency) in sessions)
        {
            var cash = payments.Where(p => p.SessionId == id && p.CurrencyCode == currency && p.MethodCode == "CASH" && successStatuses.Contains(p.PaymentStatus)).ToList();
            var paymentNumbers = cash.Select(p => p.PaymentNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var manual = movements.Where(m => m.TillSessionId == id && m.CurrencyCode == currency
                && !(m.MovementType == "CASH_IN" && m.ReferenceNumber != null && paymentNumbers.Contains(m.ReferenceNumber))).ToList();
            var canon = canonical.Where(c => c.SessionId == id && c.CurrencyCode == currency).ToList();
            var otherIn = manual.Where(m => m.MovementType is "CASH_IN" or "OPENING_FLOAT").Sum(m => m.Amount) + canon.Where(c => c.Direction == "IN").Sum(c => c.Amount);
            var otherOut = manual.Where(m => m.MovementType is "CASH_OUT" or "CASH_DROP" or "CLOSING_REMOVE").Sum(m => m.Amount) + canon.Where(c => c.Direction == "OUT").Sum(c => c.Amount);
            var receipts = cash.Sum(p => p.PaidAmount);
            var refunds = cash.Sum(p => p.RefundedAmount);
            var figures = new CashFigures(opening, receipts, refunds, otherIn, otherOut,
                ReportRules.CalculateExpectedCash(opening, receipts, refunds, otherIn, otherOut));
            var activity = payments.Where(p => p.SessionId == id).Select(p => (p.Id, "PAYMENT", p.UpdatedAt.HasValue && p.UpdatedAt.Value > p.CreatedAt ? p.UpdatedAt.Value : p.CreatedAt))
                .Concat(movements.Where(m => m.TillSessionId == id).Select(m => (m.Id, "MOVEMENT", m.CreatedAt)))
                .Concat(canon.Select(c => (c.Id, "MOVEMENT", c.CreatedAt))).ToList();
            result[id] = (figures, activity);
        }
        return result;
    }

    private sealed record CashFigures(decimal OpeningFloat, decimal CashReceipts, decimal CashRefunds,
        decimal OtherCashIn, decimal OtherCashOut, decimal ExpectedCash);
}
