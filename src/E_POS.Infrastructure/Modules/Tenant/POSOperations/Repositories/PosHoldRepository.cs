using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Infrastructure.Persistence;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;

public sealed class PosHoldRepository : IPosHoldRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly IPosCheckoutRepository _checkoutRepository;
    private readonly IPosTillSessionRepository _tillSessionRepository;

    public PosHoldRepository(
        EPosDbContext dbContext,
        IPosCheckoutRepository checkoutRepository,
        IPosTillSessionRepository tillSessionRepository)
    {
        _dbContext = dbContext;
        _checkoutRepository = checkoutRepository;
        _tillSessionRepository = tillSessionRepository;
    }

    public async Task<PosCancelHoldRepositoryResult> CancelHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid holdId,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ExpireDueHoldsAsync(tenantId, now, cancellationToken);

        var hold = await _dbContext.PosOrderHolds.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == holdId &&
                        x.HeldByTenantUserId == tenantUserId)
            .Select(x => new
            {
                x.SalesOrderId,
                x.HoldStatus,
                x.ReleasedAt,
                x.CancelledAt,
                x.HoldNumber
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (hold is null)
            return new("pos_holds.not_found");
        if (string.Equals(hold.HoldStatus, "EXPIRED", StringComparison.Ordinal))
            return new("pos_holds.expired");
        if (hold.HoldStatus != "HELD" || hold.ReleasedAt.HasValue || hold.CancelledAt.HasValue)
            return new("pos_holds.not_cancellable");

        // The service layer enforces a mandatory, trimmed, <=250 char reason. This
        // fallback only protects direct repository callers (e.g. legacy tests).
        var cancellationReason = string.IsNullOrWhiteSpace(reason)
            ? "Cancelled by cashier."
            : reason.Trim();
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var holdAffected = await _dbContext.PosOrderHolds
            .Where(x => x.TenantId == tenantId && x.Id == holdId &&
                        x.HeldByTenantUserId == tenantUserId &&
                        x.HoldStatus == "HELD" && x.ReleasedAt == null &&
                        x.CancelledAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.HoldStatus, "CANCELLED")
                .SetProperty(x => x.CancelledAt, now)
                .SetProperty(x => x.CancellationReason, cancellationReason)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (holdAffected != 1)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return new("pos_holds.not_cancellable");
        }

        var orderAffected = await _dbContext.SalesOrders
            .Where(x => x.TenantId == tenantId && x.Id == hold.SalesOrderId &&
                        x.Status == "DRAFT")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "CANCELLED")
                .SetProperty(x => x.PaymentStatus, "UNPAID")
                .SetProperty(x => x.FulfillmentStatus, "CANCELLED")
                .SetProperty(x => x.CancelledAt, now)
                .SetProperty(x => x.CancellationReason, cancellationReason)
                .SetProperty(x => x.UpdatedByTenantUserId, tenantUserId)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (orderAffected != 1)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return new("pos_holds.not_cancellable");
        }

        _dbContext.PosOrderHoldEvents.Add(PosOrderHoldEvent.Create(
            Guid.NewGuid(), tenantId, holdId, "PARK_CANCELLED", now, tenantUserId,
            null, null, null, null, hold.HoldNumber, hold.SalesOrderId,
            "HELD", "CANCELLED", null, cancellationReason));
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(null);
    }

    public async Task<PosRecallHoldRepositoryResult> RecallHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        Guid holdId,
        PosRecallHoldRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ExpireDueHoldsAsync(tenantId, now, cancellationToken);

        var parked = await (from hold in _dbContext.PosOrderHolds.AsNoTracking()
                            join order in _dbContext.SalesOrders.AsNoTracking()
                                on new { hold.TenantId, Id = hold.SalesOrderId }
                                equals new { order.TenantId, order.Id }
                            where hold.TenantId == tenantId && hold.Id == holdId &&
                                  hold.HeldByTenantUserId == tenantUserId
                            select new { Hold = hold, Order = order })
            .FirstOrDefaultAsync(cancellationToken);
        if (parked is null)
            return new("pos_holds.not_found", null);
        if (string.Equals(parked.Hold.HoldStatus, "EXPIRED", StringComparison.Ordinal))
            return new("pos_holds.expired", null);
        if (parked.Hold.HoldStatus != "HELD" || parked.Hold.ReleasedAt.HasValue ||
            parked.Hold.CancelledAt.HasValue)
            return new("pos_holds.not_recallable", null);

        var sessionResult = await _tillSessionRepository.ResolveCurrentSessionAsync(
            tenantId, request.DeviceId, cancellationToken);
        if (!sessionResult.IsSuccess || sessionResult.Snapshot is null)
            return new(sessionResult.ErrorCode ?? "pos_checkout.till_session_not_open", null);
        var session = sessionResult.Snapshot;
        if (parked.Order.TillId != session.TillId)
            return new("pos_holds.till_mismatch", null);

        var storedLines = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == parked.Order.Id &&
                        x.ProductVariantId.HasValue && x.Quantity > 0)
            .OrderBy(x => x.LineNumber)
            .Select(x => new PosCheckoutLineRequestDto(
                x.ProductVariantId!.Value, (int)x.Quantity, x.UomId, x.LineNote))
            .ToListAsync(cancellationToken);
        if (storedLines.Count == 0)
            return new("pos_checkout.invalid_lines", null);

        // Soft stock re-validation only: Park never reserved inventory, so recall must
        // recheck current stock/pricing and surface any shortfalls as warnings rather
        // than blocking the recall outright.
        var summaryResult = await _checkoutRepository.CalculateSummaryAsync(
            tenantId, tenantUserId, permissions,
            new PosCheckoutSummaryRequestDto(
                request.DeviceId, "NewSale", parked.Order.CustomerId, storedLines),
            now, cancellationToken);
        if (!summaryResult.IsSuccess || summaryResult.Summary is null)
            return new(summaryResult.ErrorCode ?? "pos_holds.recall_failed", null);

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var affected = await _dbContext.PosOrderHolds
            .Where(x => x.TenantId == tenantId && x.Id == holdId &&
                        x.HeldByTenantUserId == tenantUserId &&
                        x.HoldStatus == "HELD" && x.ReleasedAt == null &&
                        x.CancelledAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.HoldStatus, "RELEASED")
                .SetProperty(x => x.ReleasedByTenantUserId, tenantUserId)
                .SetProperty(x => x.ReleasedAt, now)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (affected != 1)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return new("pos_holds.not_recallable", null);
        }

        // SalesOrder intentionally stays DRAFT: recall only releases the Park hold and
        // hands the in-memory cart back to checkout, it does not advance order status.
        _dbContext.PosOrderHoldEvents.Add(PosOrderHoldEvent.Create(
            Guid.NewGuid(), tenantId, holdId, "PARK_RECALLED", now, tenantUserId,
            session.OutletId, session.TillId, session.SessionId, request.DeviceId,
            parked.Hold.HoldNumber, parked.Order.Id, "HELD", "RELEASED", null));
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null) await transaction.CommitAsync(cancellationToken);

        return new(null, new PosRecallHoldResponseDto(
            holdId, parked.Order.Id, parked.Hold.HoldNumber, request.DeviceId,
            parked.Order.CustomerId, parked.Order.CustomerNameSnapshot, "NewSale",
            parked.Hold.HoldReason, now, storedLines, summaryResult.Summary,
            summaryResult.Summary.ValidationMessages));
    }

    public async Task<PosCreateHoldRepositoryResult> CreateHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCreateHoldRequestDto request,
        DateTimeOffset heldAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var now = heldAt;
        var normalizedLines = request.Lines
            .Select(x => x with { LineNote = string.IsNullOrWhiteSpace(x.LineNote) ? null : x.LineNote.Trim() })
            .GroupBy(x => new { x.VariantId, x.UomId, x.LineNote })
            .OrderBy(x => x.Key.VariantId)
            .Select(x => new PosCheckoutLineRequestDto(x.Key.VariantId, checked(x.Sum(y => y.Qty)),
                x.Key.UomId, x.Key.LineNote, x.First().ClientLineId, x.First().Source,
                x.First().RecommendationParentProductId, x.First().RecommendationRelationshipId))
            .ToList();

        var idempotencyKey = request.IdempotencyKey!.Trim();
        var requestFingerprint = ComputeRequestFingerprint(request, normalizedLines);

        // Legacy compatibility only: ExternalOrderReference keeps its historical
        // "POS_HOLD:<hash>:<hash>" shape so older reporting/exports keep working. The
        // authoritative idempotency guard is PosOrderHold.IdempotencyKey plus the
        // tenant-scoped filtered unique index (uq_pos_order_holds_tenant_id_idempotency_key).
        var reference = $"POS_HOLD:{Hash(idempotencyKey)[..32]}:{requestFingerprint[..32]}";

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await AcquireParkReferenceLockAsync(tenantId, heldAt, cancellationToken);

        var existingByKey = await _dbContext.PosOrderHolds.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey)
            .Select(x => new { x.Id, x.RequestFingerprint })
            .FirstOrDefaultAsync(cancellationToken);
        if (existingByKey is not null)
        {
            if (!string.Equals(existingByKey.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            {
                if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                return new("pos_holds.idempotency_conflict", null);
            }

            var replayed = await LoadHoldItemAsync(tenantId, existingByKey.Id, cancellationToken);
            if (replayed is null)
            {
                if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                return new("pos_holds.idempotency_conflict", null);
            }

            _dbContext.PosOrderHoldEvents.Add(PosOrderHoldEvent.Create(
                Guid.NewGuid(), tenantId, existingByKey.Id, "PARK_IDEMPOTENT_REPLAY", now,
                tenantUserId, null, null, null, request.DeviceId, replayed.HoldNumber,
                replayed.SaleId, "HELD", "HELD", null, "Idempotent replay of an existing park request."));
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(null, replayed);
        }

        // Partial-payment guard: a sale that already has any recorded payment cannot be
        // parked (it must be voided/refunded through the payment flow instead).
        if (request.SourceSaleId is { } sourceSaleId && sourceSaleId != Guid.Empty)
        {
            var sourceOrder = await _dbContext.SalesOrders.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == sourceSaleId)
                .Select(x => new { x.PaidAmount, x.PaymentStatus })
                .FirstOrDefaultAsync(cancellationToken);
            var hasRecordedPayment = sourceOrder is not null &&
                (sourceOrder.PaidAmount > 0m ||
                 (!string.IsNullOrWhiteSpace(sourceOrder.PaymentStatus) &&
                  !string.Equals(sourceOrder.PaymentStatus, "UNPAID", StringComparison.OrdinalIgnoreCase)));
            if (hasRecordedPayment)
            {
                if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                return new("pos_holds.sale_partially_paid_cannot_be_parked", null);
            }
        }

        // Soft stock validation only (CalculateSummaryAsync). Park does not reserve or
        // deduct inventory; availability is re-checked again at Recall/checkout time.
        var summaryResult = await _checkoutRepository.CalculateSummaryAsync(
            tenantId, tenantUserId, permissions,
            new PosCheckoutSummaryRequestDto(
                request.DeviceId, request.SaleType, request.CustomerId,
                normalizedLines, request.DiscountApplicationId),
            now, cancellationToken);
        if (!summaryResult.IsSuccess || summaryResult.Summary is null)
            return new(summaryResult.ErrorCode ?? "pos_holds.create_failed", null);

        var sessionResult = await _tillSessionRepository.ResolveCurrentSessionAsync(
            tenantId, request.DeviceId, cancellationToken);
        if (!sessionResult.IsSuccess || sessionResult.Snapshot is null)
            return new(sessionResult.ErrorCode ?? "pos_checkout.till_session_not_open", null);
        var session = sessionResult.Snapshot;
        var businessDate = await _dbContext.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.SessionId &&
                        x.TillId == session.TillId && x.Status == "OPEN")
            .Select(x => (DateOnly?)x.BusinessDate)
            .SingleOrDefaultAsync(cancellationToken);
        if (!businessDate.HasValue)
            return new("till_session.not_found", null);
        var summary = summaryResult.Summary;

        var variantIds = normalizedLines.Select(x => x.VariantId).ToList();
        var variants = await (from variant in _dbContext.ProductVariants.AsNoTracking()
                              join product in _dbContext.Products.AsNoTracking()
                                  on new { variant.TenantId, Id = variant.ProductId }
                                  equals new { product.TenantId, product.Id }
                              join uom in _dbContext.UnitOfMeasures.AsNoTracking()
                                  on variant.SalesUomId equals uom.Id
                              where variant.TenantId == tenantId &&
                                    (uom.TenantId == null || uom.TenantId == tenantId) &&
                                    variantIds.Contains(variant.Id)
                              select new
                              {
                                  Variant = variant,
                                  Product = product,
                                  Uom = uom
                              }).ToListAsync(cancellationToken);
        if (variants.Count != variantIds.Count)
            return new("pos_checkout.variant_not_found", null);

        var priceList = await _dbContext.PriceLists.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" &&
                        x.CurrencyCode == summary.BillingSummary.Currency &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now) &&
                        (x.IsDefaultPriceList || _dbContext.PriceListOutlets.Any(m =>
                            m.TenantId == tenantId && m.PriceListId == x.Id &&
                            m.OutletId == session.OutletId && m.Status == "ACTIVE")))
            .OrderByDescending(x => _dbContext.PriceListOutlets.Any(m =>
                m.TenantId == tenantId && m.PriceListId == x.Id &&
                m.OutletId == session.OutletId && m.Status == "ACTIVE"))
            .ThenByDescending(x => x.IsDefaultPriceList)
            .ThenByDescending(x => x.Priority)
            .FirstOrDefaultAsync(cancellationToken);
        if (priceList is null)
            return new("pos_checkout.price_not_configured", null);

        var productIds = variants.Select(x => x.Product.Id).ToList();
        var imageUrlsByProduct = await LoadPrimaryImageUrlsAsync(
            tenantId, productIds, cancellationToken);
        var priceRows = await _dbContext.PriceListItems.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PriceListId == priceList.Id &&
                        x.Status == "ACTIVE" && productIds.Contains(x.ProductId) &&
                        (!x.ProductVariantId.HasValue || variantIds.Contains(x.ProductVariantId.Value)) &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= now) &&
                        (!x.ValidUntil.HasValue || x.ValidUntil >= now))
            .ToListAsync(cancellationToken);

        var salesChannelId = await EnsurePosSalesChannelAsync(tenantId, now, cancellationToken);
        var customerName = request.CustomerId.HasValue
            ? await _dbContext.Customers.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == request.CustomerId.Value)
                .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var orderNumber = await NextNumberAsync(
            _dbContext.SalesOrders.Where(x => x.TenantId == tenantId).Select(x => x.OrderNumber),
            "SO-", cancellationToken);
        var holdNumber = await NextParkReferenceAsync(
            tenantId, heldAt, cancellationToken);
        var saleId = Guid.NewGuid();
        var holdId = Guid.NewGuid();
        var order = SalesOrder.CreateHeldPosSale(
            saleId, tenantId, orderNumber, reference, salesChannelId,
            request.CustomerId, customerName, session.TillId, session.SessionId,
            priceList.Id, summary.BillingSummary.Currency, priceList.PriceIncludesTax,
            summary.BillingSummary.Subtotal, summary.BillingSummary.Discount,
            summary.BillingSummary.Tax, summary.BillingSummary.TotalPayable,
            request.Reason, tenantUserId, businessDate.Value, now);
        _dbContext.SalesOrders.Add(order);

        var responseLines = new List<PosHoldLineDto>();
        var selectedPrices = normalizedLines.ToDictionary(
            requestedLine => requestedLine.VariantId,
            requestedLine =>
            {
                var detail = variants.Single(x => x.Variant.Id == requestedLine.VariantId);
                return priceRows
                    .Where(x => x.ProductId == detail.Product.Id &&
                                (!x.ProductVariantId.HasValue || x.ProductVariantId == requestedLine.VariantId) &&
                                (!x.UomId.HasValue || x.UomId == detail.Variant.SalesUomId) &&
                                x.MinQuantity <= requestedLine.Qty)
                    .OrderByDescending(x => x.ProductVariantId.HasValue)
                    .ThenByDescending(x => x.UomId.HasValue)
                    .ThenByDescending(x => x.MinQuantity)
                    .FirstOrDefault();
            });
        if (selectedPrices.Values.Any(x => x is null))
            return new("pos_checkout.price_not_configured", null);
        var rawSubtotal = normalizedLines.Sum(x =>
            selectedPrices[x.VariantId]!.SellingPrice * x.Qty);
        if (rawSubtotal <= 0m)
            return new("pos_checkout.price_not_configured", null);

        var lineNumber = 1;
        foreach (var requestedLine in normalizedLines)
        {
            var detail = variants.Single(x => x.Variant.Id == requestedLine.VariantId);
            var price = selectedPrices[requestedLine.VariantId]!;
            var lineWeight = price.SellingPrice * requestedLine.Qty / rawSubtotal;
            var lineSubtotal = summary.BillingSummary.Subtotal * lineWeight;
            var unitPrice = lineSubtotal / requestedLine.Qty;
            var ratio = summary.BillingSummary.Subtotal == 0
                ? 0m : lineSubtotal / summary.BillingSummary.Subtotal;
            var lineDiscount = summary.BillingSummary.Discount * ratio;
            var lineTax = summary.BillingSummary.Tax * ratio;
            var line = SalesOrderLine.CreateForHeldPosSale(
                Guid.NewGuid(), tenantId, saleId, lineNumber++, detail.Product.Id,
                detail.Variant.Id, detail.Variant.SalesUomId, price.Id,
                detail.Variant.Sku, detail.Product.ProductName, detail.Variant.VariantName,
                detail.Uom.UomCode, detail.Uom.UomName, detail.Product.ProductType,
                detail.Product.ProductStructure, requestedLine.Qty, unitPrice,
                lineSubtotal, lineDiscount, lineTax, priceList.PriceIncludesTax, now);
            _dbContext.SalesOrderLines.Add(line);
            line.SetLineNote(requestedLine.LineNote, now);
            var lineTotal = priceList.PriceIncludesTax
                ? lineSubtotal - lineDiscount
                : lineSubtotal - lineDiscount + lineTax;
            responseLines.Add(new PosHoldLineDto(
                line.Id, detail.Variant.Id, detail.Product.ProductName,
                detail.Variant.VariantName, detail.Variant.Sku, requestedLine.Qty,
                ToMoney(unitPrice), ToMoney(lineTotal), requestedLine.LineNote,
                imageUrlsByProduct.GetValueOrDefault(detail.Product.Id)));
        }

        var hold = PosOrderHold.Create(
            holdId, tenantId, holdNumber, saleId, request.Reason,
            tenantUserId, heldAt, expiresAt, idempotencyKey, requestFingerprint);
        _dbContext.PosOrderHolds.Add(hold);
        _dbContext.PosOrderHoldEvents.Add(PosOrderHoldEvent.Create(
            Guid.NewGuid(), tenantId, holdId, "PARK_CREATED", now, tenantUserId,
            null, session.TillId, session.SessionId, request.DeviceId, holdNumber, saleId,
            null, "HELD", null));
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            // Never leak the raw Postgres unique-violation error: re-read by key and
            // either replay the winning concurrent create or report a clean conflict.
            if (IsUniqueViolation(exception))
            {
                var reread = await _dbContext.PosOrderHolds.AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey)
                    .Select(x => new { x.Id, x.RequestFingerprint })
                    .FirstOrDefaultAsync(cancellationToken);
                if (reread is not null &&
                    string.Equals(reread.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                {
                    var replay = await LoadHoldItemAsync(tenantId, reread.Id, cancellationToken);
                    if (replay is not null)
                        return new(null, replay);
                }
            }

            return new("pos_holds.idempotency_conflict", null);
        }

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new(null, new PosHoldListItemDto(
            holdId, holdNumber, saleId, orderNumber, session.TillId,
            session.SessionId, request.CustomerId, customerName, request.Reason,
            "held", responseLines.Sum(x => x.Qty), summary.BillingSummary.Subtotal,
            summary.BillingSummary.Discount, summary.BillingSummary.Tax,
            summary.BillingSummary.TotalPayable, summary.BillingSummary.Currency,
            heldAt, expiresAt, responseLines));
    }

    public async Task<PosGetActiveHoldsRepositoryResult> GetActiveHoldsAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosHoldListQueryDto query,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ExpireDueHoldsAsync(tenantId, now, cancellationToken);

        // The active-holds till scope is always resolved server-side from the caller's
        // trusted device + its currently open till session. A client-supplied tillId is
        // never accepted, and OpenedByTenantUserId is never used as a till lookup: a
        // cashier can be signed in without owning the till session that opened it.
        var sessionResult = await _tillSessionRepository.ResolveCurrentSessionAsync(
            tenantId, query.DeviceId, cancellationToken);
        if (!sessionResult.IsSuccess || sessionResult.Snapshot is null)
        {
            return new PosGetActiveHoldsRepositoryResult(
                sessionResult.ErrorCode ?? "pos_checkout.till_session_not_open", null);
        }
        var session = sessionResult.Snapshot;

        var sessionAuthority = await _dbContext.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == session.SessionId &&
                        x.TillId == session.TillId && x.Status == "OPEN")
            .Select(x => new { x.BusinessDate, x.CurrencyCode })
            .SingleOrDefaultAsync(cancellationToken);
        if (sessionAuthority is null)
        {
            return new PosGetActiveHoldsRepositoryResult("till_session.not_found", null);
        }

        var filtered =
            from hold in _dbContext.PosOrderHolds.AsNoTracking()
            join order in _dbContext.SalesOrders.AsNoTracking()
                on new { hold.TenantId, Id = hold.SalesOrderId }
                equals new { order.TenantId, order.Id }
            where hold.TenantId == tenantId &&
                  hold.HeldByTenantUserId == tenantUserId &&
                  hold.HoldStatus == "HELD" &&
                  hold.ReleasedAt == null &&
                  hold.CancelledAt == null &&
                  (!hold.ExpiresAt.HasValue || hold.ExpiresAt > now) &&
                  order.TillId == session.TillId
            select new
            {
                Hold = hold,
                Order = order
            };

        filtered = query.Scope switch
        {
            PosHoldListScopes.Today => filtered.Where(
                x => x.Order.BusinessDate == sessionAuthority.BusinessDate),
            PosHoldListScopes.CurrentShift => filtered.Where(
                x => x.Order.TillSessionId == session.SessionId),
            _ => filtered
        };

        var aggregate = await filtered
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                TotalValue = group.Sum(x => x.Order.TotalAmount)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var totalCount = aggregate?.TotalCount ?? 0;
        var totalValue = aggregate is null ? 0 : ToMoney(aggregate.TotalValue);

        var holds = await filtered
            .OrderByDescending(x => x.Hold.HeldAt)
            .ThenBy(x => x.Hold.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        if (holds.Count == 0)
        {
            return new PosGetActiveHoldsRepositoryResult(
                null,
                Array.Empty<PosHoldListItemDto>(),
                totalCount,
                totalValue,
                sessionAuthority.CurrencyCode);
        }

        var saleIds = holds.Select(x => x.Order.Id).ToList();
        var lines = await _dbContext.SalesOrderLines
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId.HasValue &&
                        saleIds.Contains(x.SalesOrderId.Value))
            .OrderBy(x => x.LineNumber)
            .Select(x => new
            {
                SaleId = x.SalesOrderId!.Value,
                x.ProductId,
                Line = new PosHoldLineDto(
                    x.Id,
                    x.ProductVariantId,
                    x.ProductNameSnapshot,
                    x.VariantNameSnapshot,
                    x.SkuSnapshot,
                    (int)x.Quantity,
                    ToMoney(x.UnitPrice),
                    ToMoney(x.LineTotalAmount),
                    x.LineNote)
            })
            .ToListAsync(cancellationToken);
        var lineImageUrls = await LoadPrimaryImageUrlsAsync(
            tenantId, lines.Select(x => x.ProductId).Distinct().ToList(), cancellationToken);
        var linesBySale = lines
            .GroupBy(x => x.SaleId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<PosHoldLineDto>)x
                    .Select(y => y.Line with
                    {
                        ImageUrl = lineImageUrls.GetValueOrDefault(y.ProductId)
                    })
                    .ToList());

        var results = holds.Select(x =>
        {
            var orderLines = linesBySale.GetValueOrDefault(
                x.Order.Id,
                Array.Empty<PosHoldLineDto>());
            return new PosHoldListItemDto(
                x.Hold.Id,
                x.Hold.HoldNumber,
                x.Order.Id,
                x.Order.OrderNumber,
                x.Order.TillId,
                x.Order.TillSessionId,
                x.Order.CustomerId,
                x.Order.CustomerNameSnapshot,
                x.Hold.HoldReason,
                x.Hold.HoldStatus.ToLowerInvariant(),
                orderLines.Sum(line => line.Qty),
                ToMoney(x.Order.SubtotalAmount),
                ToMoney(x.Order.DiscountAmount),
                ToMoney(x.Order.TaxAmount),
                ToMoney(x.Order.TotalAmount),
                x.Order.CurrencyCode,
                x.Hold.HeldAt,
                x.Hold.ExpiresAt,
                orderLines);
        }).ToList();

        return new PosGetActiveHoldsRepositoryResult(
            null,
            results,
            totalCount,
            totalValue,
            sessionAuthority.CurrencyCode);
    }

    /// <summary>
    /// Flips any due (HELD, ExpiresAt &lt;= now) holds to EXPIRED and writes a
    /// PARK_EXPIRED audit event for each. Called defensively at the start of every
    /// hold-lifecycle read/mutation so status is always current for this tenant.
    /// </summary>
    private async Task ExpireDueHoldsAsync(
        Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await _dbContext.PosOrderHolds.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.HoldStatus == "HELD" &&
                        x.ReleasedAt == null && x.CancelledAt == null &&
                        x.ExpiresAt.HasValue && x.ExpiresAt <= now)
            .Select(x => new { x.Id, x.HoldNumber, x.SalesOrderId })
            .ToListAsync(cancellationToken);
        if (due.Count == 0) return;

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var affected = await _dbContext.PosOrderHolds
            .Where(x => x.TenantId == tenantId && x.HoldStatus == "HELD" &&
                        x.ReleasedAt == null && x.CancelledAt == null &&
                        x.ExpiresAt.HasValue && x.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.HoldStatus, "EXPIRED")
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (affected == 0)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return;
        }

        foreach (var item in due)
        {
            _dbContext.PosOrderHoldEvents.Add(PosOrderHoldEvent.Create(
                Guid.NewGuid(), tenantId, item.Id, "PARK_EXPIRED", now, null,
                null, null, null, null, item.HoldNumber, item.SalesOrderId,
                "HELD", "EXPIRED", null, $"Auto-expired {due.Count} due park hold(s)."));
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a hold (by id, regardless of current status/till) as a
    /// <see cref="PosHoldListItemDto"/>. Used for idempotent-replay responses, where the
    /// caller must see the hold exactly as it exists now, independent of the active-list
    /// filters applied by <see cref="GetActiveHoldsAsync"/>.
    /// </summary>
    private async Task<PosHoldListItemDto?> LoadHoldItemAsync(
        Guid tenantId, Guid holdId, CancellationToken cancellationToken)
    {
        var row = await (from hold in _dbContext.PosOrderHolds.AsNoTracking()
                         join order in _dbContext.SalesOrders.AsNoTracking()
                             on new { hold.TenantId, Id = hold.SalesOrderId }
                             equals new { order.TenantId, order.Id }
                         where hold.TenantId == tenantId && hold.Id == holdId
                         select new { Hold = hold, Order = order })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var lineRows = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == row.Order.Id)
            .OrderBy(x => x.LineNumber)
            .Select(x => new
            {
                x.ProductId,
                Line = new PosHoldLineDto(
                    x.Id,
                    x.ProductVariantId,
                    x.ProductNameSnapshot,
                    x.VariantNameSnapshot,
                    x.SkuSnapshot,
                    (int)x.Quantity,
                    ToMoney(x.UnitPrice),
                    ToMoney(x.LineTotalAmount),
                    x.LineNote)
            })
            .ToListAsync(cancellationToken);
        var imageUrls = await LoadPrimaryImageUrlsAsync(
            tenantId, lineRows.Select(x => x.ProductId).Distinct().ToList(), cancellationToken);
        var lines = lineRows
            .Select(x => x.Line with
            {
                ImageUrl = imageUrls.GetValueOrDefault(x.ProductId)
            })
            .ToList();

        return new PosHoldListItemDto(
            row.Hold.Id,
            row.Hold.HoldNumber,
            row.Order.Id,
            row.Order.OrderNumber,
            row.Order.TillId,
            row.Order.TillSessionId,
            row.Order.CustomerId,
            row.Order.CustomerNameSnapshot,
            row.Hold.HoldReason,
            row.Hold.HoldStatus.ToLowerInvariant(),
            lines.Sum(x => x.Qty),
            ToMoney(row.Order.SubtotalAmount),
            ToMoney(row.Order.DiscountAmount),
            ToMoney(row.Order.TaxAmount),
            ToMoney(row.Order.TotalAmount),
            row.Order.CurrencyCode,
            row.Hold.HeldAt,
            row.Hold.ExpiresAt,
            lines);
    }

    private static string ComputeRequestFingerprint(
        PosCreateHoldRequestDto request,
        IReadOnlyList<PosCheckoutLineRequestDto> normalizedLines) =>
        Hash(JsonSerializer.Serialize(new
        {
            request.DeviceId,
            request.SaleType,
            request.CustomerId,
            Lines = normalizedLines,
            request.Reason,
            request.DiscountApplicationId,
            request.SourceSaleId
        }));

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
        }
        return false;
    }

    private static int ToMoney(decimal value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private async Task<IReadOnlyDictionary<Guid, string>> LoadPrimaryImageUrlsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, string>();

        var rows = await (
            from image in _dbContext.ProductImages.AsNoTracking()
            join mediaAsset in _dbContext.MediaAssets.AsNoTracking()
                on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id }
            where image.TenantId == tenantId &&
                  productIds.Contains(image.ProductId) &&
                  image.Status == "ACTIVE" &&
                  mediaAsset.Status == "ACTIVE" &&
                  mediaAsset.PublicUrl != null &&
                  mediaAsset.PublicUrl != ""
            select new
            {
                image.Id,
                image.ProductId,
                image.ProductVariantId,
                image.IsPrimaryImage,
                image.SortOrder,
                mediaAsset.PublicUrl
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(x => x.ProductVariantId.HasValue ? 1 : 0)
                    .ThenByDescending(x => x.IsPrimaryImage)
                    .ThenBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .First()
                    .PublicUrl!);
    }

    internal async Task<Guid> EnsurePosSalesChannelAsync(
        Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var posChannelId = await (from sc in _dbContext.SalesChannels.AsNoTracking()
                                  join psc in _dbContext.PlatformSalesChannels.AsNoTracking() on sc.PlatformSalesChannelId equals psc.Id
                                  where sc.TenantId == tenantId && psc.ChannelType == "POS" && sc.Status == "ACTIVE"
                                  select (Guid?)sc.Id)
                                  .FirstOrDefaultAsync(cancellationToken);
        if (posChannelId.HasValue) return posChannelId.Value;

        var platformPosId = await _dbContext.PlatformSalesChannels.AsNoTracking()
            .Where(x => x.ChannelType == "POS")
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (platformPosId == Guid.Empty)
            throw new MissingSystemPosSalesChannelException(tenantId);

        var channel = SalesChannel.Create(
            Guid.NewGuid(), tenantId, platformPosId, "POS", "ACTIVE", 0, now);
        _dbContext.SalesChannels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return channel.Id;
    }

    private async Task<string> NextParkReferenceAsync(
        Guid tenantId,
        DateTimeOffset heldAt,
        CancellationToken cancellationToken)
    {
        var prefix = ParkSaleReference.Prefix(heldAt);
        var values = await _dbContext.PosOrderHolds.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.HoldNumber.StartsWith(prefix))
            .Select(x => x.HoldNumber)
            .ToListAsync(cancellationToken);
        var maximum = values
            .Select(value => ParkSaleReference.TryReadSequence(value, heldAt, out var sequence)
                ? sequence
                : 0)
            .DefaultIfEmpty()
            .Max();

        return ParkSaleReference.Format(heldAt, checked(maximum + 1));
    }

    private async Task AcquireParkReferenceLockAsync(
        Guid tenantId,
        DateTimeOffset heldAt,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
            return;

        var resource = ParkSaleReference.LockResource(tenantId, heldAt);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({resource}, 0))",
            cancellationToken);
    }

    private static async Task<string> NextNumberAsync(
        IQueryable<string> query, string prefix, CancellationToken cancellationToken)
    {
        var values = await query.Where(x => x.StartsWith(prefix)).ToListAsync(cancellationToken);
        var max = values.Select(x => int.TryParse(x[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty().Max();
        return $"{prefix}{max + 1:D6}";
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
