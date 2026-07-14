using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformBillingRepository : IPlatformBillingRepository
{
    private const string Overdue = "OVERDUE";
    private readonly EPosDbContext _db;

    public PlatformBillingRepository(EPosDbContext db) => _db = db;

    public async Task<PlatformBillingSummaryResponse> GetSummaryAsync(PlatformBillingQuery query, DateTimeOffset now, CancellationToken ct)
    {
        var filtered = ApplyFilters(BaseQuery(), query, now);
        var aggregates = await filtered.GroupBy(x => x.CurrencyCode)
            .Select(g => new
            {
                CurrencyCode = g.Key,
                PaidRevenue = g.Where(x => x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusPaid).Sum(x => x.TotalAmount),
                OutstandingAmount = g.Where(x => x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusPending).Sum(x => x.BalanceDue),
                OverdueAmount = g.Where(x => x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusPending && x.DueAt < now).Sum(x => x.BalanceDue),
                InvoiceCount = g.Count()
            })
            .OrderBy(x => x.CurrencyCode)
            .ToListAsync(ct);
        var rows = aggregates.Select(x => new PlatformBillingCurrencySummaryDto(
            x.CurrencyCode, x.PaidRevenue, x.OutstandingAmount, x.OverdueAmount, x.InvoiceCount)).ToList();
        return new(rows, await filtered.CountAsync(ct), now);
    }

    public async Task<PlatformBillingInvoiceListResponse> GetInvoicesAsync(PlatformBillingQuery query, DateTimeOffset now, CancellationToken ct)
    {
        var filtered = ApplyFilters(BaseQuery(), query, now);
        var total = await filtered.CountAsync(ct);
        var sorted = ApplySort(filtered, query);
        var rows = await sorted.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new(rows.Select(x => ToDto(x, now)).ToList(), query.PageNumber, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<PlatformBillingInvoiceDetailResponse?> GetInvoiceAsync(Guid id, DateTimeOffset now, CancellationToken ct)
    {
        var row = await BaseQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return null;
        var lines = await _db.SubscriptionInvoiceLines.AsNoTracking().Where(x => x.InvoiceId == id)
            .OrderBy(x => x.LineNumberInt).ThenBy(x => x.LineNumber)
            .Select(x => new PlatformBillingInvoiceLineDto(x.Id, x.LineNumber, x.Description, x.Quantity, x.UnitPrice, x.DiscountAmount, x.TaxAmount, x.LineTotal)).ToListAsync(ct);
        var payments = await PaymentsQuery(id).ToListAsync(ct);
        return new(ToDto(row, now), row.InvoiceType, row.BillingCycle, row.BillingPeriodStart, row.BillingPeriodEnd,
            row.SubtotalAmount, row.DiscountAmount, row.TaxAmount, lines, payments);
    }

    public async Task<IReadOnlyList<PlatformBillingPaymentDto>?> GetPaymentsAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.SubscriptionInvoices.AsNoTracking().AnyAsync(x => x.Id == id, ct)) return null;
        return await PaymentsQuery(id).ToListAsync(ct);
    }

    public async Task<PlatformBillingFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken ct)
    {
        var tenantRows = await (from tenant in _db.Tenants.AsNoTracking()
                                join invoice in _db.SubscriptionInvoices.AsNoTracking() on tenant.Id equals invoice.TenantId
                                select new { tenant.Id, tenant.TenantCode, tenant.DisplayName })
            .Distinct().OrderBy(x => x.DisplayName).ToListAsync(ct);
        var tenants = tenantRows.Select(x => new PlatformBillingTenantOptionDto(x.Id, x.TenantCode, x.DisplayName)).ToList();
        return new(tenants, new[] { "DRAFT", "PENDING", "OVERDUE", "PAID" });
    }

    public Task<PlatformBillingMutationResult> IssueAsync(Guid id, DateTimeOffset expected, DateTimeOffset now, CancellationToken ct)
        => MutateAsync(id, expected, invoice => invoice.Issue(now), now, ct);

    public Task<PlatformBillingMutationResult> MarkPaidAsync(Guid id, DateTimeOffset expected, DateTimeOffset paidAt, DateTimeOffset now, CancellationToken ct)
        => MutateAsync(id, expected, invoice => invoice.MarkPaid(paidAt, now), now, ct);

    private async Task<PlatformBillingMutationResult> MutateAsync(Guid id, DateTimeOffset expected, Action<E_POS.Domain.Modules.Platform.Subscription.Entities.SubscriptionInvoice> action, DateTimeOffset now, CancellationToken ct)
    {
        var invoice = await _db.SubscriptionInvoices.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (invoice is null) return new(PlatformBillingMutationOutcome.NotFound);
        if (invoice.UpdatedAt != expected) return new(PlatformBillingMutationOutcome.ConcurrencyConflict);
        try { action(invoice); }
        catch (InvalidOperationException) { return new(PlatformBillingMutationOutcome.InvalidTransition); }
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return new(PlatformBillingMutationOutcome.ConcurrencyConflict); }
        var row = await BaseQuery().SingleAsync(x => x.Id == id, ct);
        return new(PlatformBillingMutationOutcome.Success, ToDto(row, now));
    }

    private IQueryable<InvoiceRow> BaseQuery() =>
        from invoice in _db.SubscriptionInvoices.AsNoTracking()
        join tenant in _db.Tenants.AsNoTracking() on invoice.TenantId equals tenant.Id
        join subscription in _db.TenantSubscriptions.AsNoTracking() on invoice.SubscriptionId equals subscription.Id
        join plan in _db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
        select new InvoiceRow
        {
            Id = invoice.Id, InvoiceNumber = invoice.InvoiceNumber, TenantId = tenant.Id, TenantCode = tenant.TenantCode,
            TenantName = tenant.DisplayName, SubscriptionId = subscription.Id, SubscriptionStatus = subscription.SubscriptionStatus,
            PlanId = plan.Id, PlanCode = plan.PlanCode, PlanName = plan.Name, CurrencyCode = invoice.CurrencyCode,
            TotalAmount = invoice.TotalAmount, PaidAmount = invoice.PaidAmount, BalanceDue = invoice.BalanceDue,
            StoredStatus = invoice.InvoiceStatus, InvoiceType = invoice.InvoiceType, BillingCycle = invoice.BillingCycle,
            BillingPeriodStart = invoice.BillingPeriodStart, BillingPeriodEnd = invoice.BillingPeriodEnd,
            SubtotalAmount = invoice.SubtotalAmount, DiscountAmount = invoice.DiscountAmount, TaxAmount = invoice.TaxAmount,
            IssuedAt = invoice.IssuedAt, DueAt = invoice.DueAt, PaidAt = invoice.PaidAt, CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt ?? invoice.CreatedAt
        };

    private static IQueryable<InvoiceRow> ApplyFilters(IQueryable<InvoiceRow> q, PlatformBillingQuery f, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim().ToLower();
            q = q.Where(x => x.InvoiceNumber.ToLower().Contains(term) || x.TenantName.ToLower().Contains(term) || x.TenantCode.ToLower().Contains(term));
        }
        if (f.TenantId.HasValue) q = q.Where(x => x.TenantId == f.TenantId.Value);
        if (!string.IsNullOrWhiteSpace(f.Status))
        {
            var status = f.Status.Trim().ToUpperInvariant();
            q = status == Overdue
                ? q.Where(x => x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusPending && x.DueAt < now)
                : q.Where(x => x.StoredStatus == status && !(status == TenantSubscriptionBillingConstants.InvoiceStatusPending && x.DueAt < now));
        }
        if (f.DateField.Equals("dueAt", StringComparison.OrdinalIgnoreCase))
        {
            if (f.DateFrom.HasValue) q = q.Where(x => x.DueAt >= f.DateFrom);
            if (f.DateTo.HasValue) q = q.Where(x => x.DueAt <= f.DateTo);
        }
        else
        {
            if (f.DateFrom.HasValue) q = q.Where(x => x.IssuedAt >= f.DateFrom);
            if (f.DateTo.HasValue) q = q.Where(x => x.IssuedAt <= f.DateTo);
        }
        return q;
    }

    private static IQueryable<InvoiceRow> ApplySort(IQueryable<InvoiceRow> q, PlatformBillingQuery f)
    {
        var asc = f.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        return f.SortBy.ToLowerInvariant() switch
        {
            "invoicenumber" => asc ? q.OrderBy(x => x.InvoiceNumber) : q.OrderByDescending(x => x.InvoiceNumber),
            "tenant" => asc ? q.OrderBy(x => x.TenantName) : q.OrderByDescending(x => x.TenantName),
            "amount" => asc ? q.OrderBy(x => x.TotalAmount) : q.OrderByDescending(x => x.TotalAmount),
            "status" => asc ? q.OrderBy(x => x.StoredStatus) : q.OrderByDescending(x => x.StoredStatus),
            "issuedat" => asc ? q.OrderBy(x => x.IssuedAt) : q.OrderByDescending(x => x.IssuedAt),
            "dueat" => asc ? q.OrderBy(x => x.DueAt) : q.OrderByDescending(x => x.DueAt),
            _ => asc ? q.OrderBy(x => x.CreatedAt) : q.OrderByDescending(x => x.CreatedAt)
        };
    }

    private IQueryable<PlatformBillingPaymentDto> PaymentsQuery(Guid id) => _db.SubscriptionPaymentTransactions.AsNoTracking()
        .Where(x => x.InvoiceId == id).OrderByDescending(x => x.CreatedAt)
        .Select(x => new PlatformBillingPaymentDto(x.Id, x.ProviderName, x.ProviderTransactionId, x.TransactionStatus,
            x.CurrencyCode, x.Amount, x.ProviderFee, x.NetAmount, x.PaidAt, x.CreatedAt));

    private static PlatformBillingInvoiceDto ToDto(InvoiceRow x, DateTimeOffset now)
    {
        var overdue = x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusPending && x.DueAt < now;
        return new(x.Id, x.InvoiceNumber, x.TenantId, x.TenantCode, x.TenantName, x.SubscriptionId,
            x.SubscriptionStatus, x.PlanId, x.PlanCode, x.PlanName, x.CurrencyCode, x.TotalAmount, x.PaidAmount,
            x.BalanceDue, x.StoredStatus, overdue ? Overdue : x.StoredStatus, x.IssuedAt, x.DueAt, x.PaidAt,
            x.CreatedAt, x.UpdatedAt, x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusDraft,
            x.StoredStatus == TenantSubscriptionBillingConstants.InvoiceStatusPending);
    }

    private sealed class InvoiceRow
    {
        public Guid Id { get; init; }
        public string InvoiceNumber { get; init; } = string.Empty;
        public Guid TenantId { get; init; }
        public string TenantCode { get; init; } = string.Empty;
        public string TenantName { get; init; } = string.Empty;
        public Guid SubscriptionId { get; init; }
        public string SubscriptionStatus { get; init; } = string.Empty;
        public Guid PlanId { get; init; }
        public string PlanCode { get; init; } = string.Empty;
        public string PlanName { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal BalanceDue { get; init; }
        public string StoredStatus { get; init; } = string.Empty;
        public string InvoiceType { get; init; } = string.Empty;
        public string? BillingCycle { get; init; }
        public DateTimeOffset? BillingPeriodStart { get; init; }
        public DateTimeOffset? BillingPeriodEnd { get; init; }
        public decimal SubtotalAmount { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal TaxAmount { get; init; }
        public DateTimeOffset? IssuedAt { get; init; }
        public DateTimeOffset? DueAt { get; init; }
        public DateTimeOffset? PaidAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
