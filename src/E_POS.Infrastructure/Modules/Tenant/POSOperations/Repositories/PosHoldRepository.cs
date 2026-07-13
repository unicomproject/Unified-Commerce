using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Infrastructure.Persistence;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
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
        var hold = await _dbContext.PosOrderHolds.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == holdId &&
                        x.HeldByTenantUserId == tenantUserId)
            .Select(x => new
            {
                x.SalesOrderId,
                x.HoldStatus,
                x.ReleasedAt,
                x.CancelledAt,
                x.ExpiresAt
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (hold is null)
            return new("pos_holds.not_found");
        if (hold.ExpiresAt.HasValue && hold.ExpiresAt <= now)
            return new("pos_holds.expired");
        if (hold.HoldStatus != "HELD" || hold.ReleasedAt.HasValue || hold.CancelledAt.HasValue)
            return new("pos_holds.not_cancellable");

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
                        x.CancelledAt == null &&
                        (!x.ExpiresAt.HasValue || x.ExpiresAt > now))
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
        if (parked.Hold.ExpiresAt.HasValue && parked.Hold.ExpiresAt <= now)
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
            .Select(x => new PosCheckoutLineRequestDto(x.ProductVariantId!.Value, (int)x.Quantity))
            .ToListAsync(cancellationToken);
        if (storedLines.Count == 0)
            return new("pos_checkout.invalid_lines", null);

        var summaryResult = await _checkoutRepository.CalculateSummaryAsync(
            tenantId, tenantUserId, permissions,
            new PosCheckoutSummaryRequestDto(
                request.DeviceId, "NewSale", parked.Order.CustomerId, storedLines),
            now, cancellationToken);
        if (!summaryResult.IsSuccess || summaryResult.Summary is null)
            return new(summaryResult.ErrorCode ?? "pos_holds.recall_failed", null);

        var affected = await _dbContext.PosOrderHolds
            .Where(x => x.TenantId == tenantId && x.Id == holdId &&
                        x.HeldByTenantUserId == tenantUserId &&
                        x.HoldStatus == "HELD" && x.ReleasedAt == null &&
                        x.CancelledAt == null &&
                        (!x.ExpiresAt.HasValue || x.ExpiresAt > now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.HoldStatus, "RELEASED")
                .SetProperty(x => x.ReleasedByTenantUserId, tenantUserId)
                .SetProperty(x => x.ReleasedAt, now)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (affected != 1)
            return new("pos_holds.not_recallable", null);

        return new(null, new PosRecallHoldResponseDto(
            holdId, parked.Order.Id, parked.Hold.HoldNumber, request.DeviceId,
            parked.Order.CustomerId, parked.Order.CustomerNameSnapshot, "NewSale",
            parked.Hold.HoldReason, now, storedLines, summaryResult.Summary));
    }

    public async Task<PosCreateHoldRepositoryResult> CreateHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCreateHoldRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedLines = request.Lines
            .GroupBy(x => x.VariantId)
            .OrderBy(x => x.Key)
            .Select(x => new PosCheckoutLineRequestDto(x.Key, checked(x.Sum(y => y.Qty))))
            .ToList();
        var keyHash = Hash(request.IdempotencyKey!.Trim())[..32];
        var requestHash = Hash(JsonSerializer.Serialize(new
        {
            request.DeviceId,
            request.SaleType,
            request.CustomerId,
            Lines = normalizedLines,
            request.Reason,
            request.DiscountApplicationId,
            request.ExpiresAt
        }))[..32];
        var referencePrefix = $"POS_HOLD:{keyHash}:";
        var reference = referencePrefix + requestHash;
        var existing = await _dbContext.SalesOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.ExternalOrderReference != null &&
                        x.ExternalOrderReference.StartsWith(referencePrefix))
            .Select(x => new { x.Id, x.ExternalOrderReference })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.ExternalOrderReference, reference, StringComparison.Ordinal))
                return new("pos_holds.idempotency_conflict", null);
            var replay = await GetHoldBySaleIdAsync(tenantId, tenantUserId, existing.Id, now, cancellationToken);
            return replay is null
                ? new("pos_holds.idempotency_conflict", null)
                : new(null, replay);
        }

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
        var holdNumber = await NextNumberAsync(
            _dbContext.PosOrderHolds.Where(x => x.TenantId == tenantId).Select(x => x.HoldNumber),
            "HOLD-", cancellationToken);
        var saleId = Guid.NewGuid();
        var holdId = Guid.NewGuid();
        var order = SalesOrder.CreateHeldPosSale(
            saleId, tenantId, orderNumber, reference, salesChannelId,
            request.CustomerId, customerName, session.TillId, session.SessionId,
            priceList.Id, summary.BillingSummary.Currency,
            summary.BillingSummary.Subtotal, summary.BillingSummary.Discount,
            summary.BillingSummary.Tax, summary.BillingSummary.TotalPayable,
            request.Reason, tenantUserId, now);
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
                lineSubtotal, lineDiscount, lineTax, now);
            _dbContext.SalesOrderLines.Add(line);
            responseLines.Add(new PosHoldLineDto(
                line.Id, detail.Variant.Id, detail.Product.ProductName,
                detail.Variant.VariantName, detail.Variant.Sku, requestedLine.Qty,
                ToMoney(unitPrice), ToMoney(lineSubtotal - lineDiscount + lineTax)));
        }

        var hold = PosOrderHold.Create(
            holdId, tenantId, holdNumber, saleId, request.Reason,
            tenantUserId, now, request.ExpiresAt);
        _dbContext.PosOrderHolds.Add(hold);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return new("pos_holds.idempotency_conflict", null);
        }

        return new(null, new PosHoldListItemDto(
            holdId, holdNumber, saleId, orderNumber, session.TillId,
            session.SessionId, request.CustomerId, customerName, request.Reason,
            "held", responseLines.Sum(x => x.Qty), summary.BillingSummary.Subtotal,
            summary.BillingSummary.Discount, summary.BillingSummary.Tax,
            summary.BillingSummary.TotalPayable, summary.BillingSummary.Currency,
            now, request.ExpiresAt, responseLines));
    }

    public async Task<IReadOnlyList<PosHoldListItemDto>> GetActiveHoldsAsync(
        Guid tenantId,
        Guid tenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var holds = await (
                from hold in _dbContext.PosOrderHolds.AsNoTracking()
                join order in _dbContext.SalesOrders.AsNoTracking()
                    on new { hold.TenantId, Id = hold.SalesOrderId }
                    equals new { order.TenantId, order.Id }
                where hold.TenantId == tenantId &&
                      hold.HeldByTenantUserId == tenantUserId &&
                      hold.HoldStatus == "HELD" &&
                      hold.ReleasedAt == null &&
                      hold.CancelledAt == null &&
                      (!hold.ExpiresAt.HasValue || hold.ExpiresAt > now)
                orderby hold.HeldAt descending, hold.Id
                select new
                {
                    Hold = hold,
                    Order = order
                })
            .ToListAsync(cancellationToken);

        if (holds.Count == 0)
        {
            return Array.Empty<PosHoldListItemDto>();
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
                Line = new PosHoldLineDto(
                    x.Id,
                    x.ProductVariantId,
                    x.ProductNameSnapshot,
                    x.VariantNameSnapshot,
                    x.SkuSnapshot,
                    (int)x.Quantity,
                    ToMoney(x.UnitPrice),
                    ToMoney(x.LineTotalAmount))
            })
            .ToListAsync(cancellationToken);
        var linesBySale = lines
            .GroupBy(x => x.SaleId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<PosHoldLineDto>)x.Select(y => y.Line).ToList());

        return holds.Select(x =>
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
    }

    private static int ToMoney(decimal value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private async Task<PosHoldListItemDto?> GetHoldBySaleIdAsync(
        Guid tenantId, Guid tenantUserId, Guid saleId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var holds = await GetActiveHoldsAsync(tenantId, tenantUserId, now, cancellationToken);
        return holds.FirstOrDefault(x => x.SaleId == saleId);
    }

    private async Task<Guid> EnsurePosSalesChannelAsync(
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
            throw new ApplicationException("System POS platform sales channel not found.");

        var channel = SalesChannel.Create(
            Guid.NewGuid(), tenantId, platformPosId, "POS", "ACTIVE", 0, now);
        _dbContext.SalesChannels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return channel.Id;
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
