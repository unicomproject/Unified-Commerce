using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformDashboardRepository : IPlatformDashboardRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformDashboardRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformDashboardResponse> GetDashboardAsync(
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Select(x => new TenantSnapshot(
                x.Id,
                x.TenantCode,
                x.Name,
                x.Status,
                x.BillingStatus,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var subscriptions = await _dbContext.TenantSubscriptions
            .AsNoTracking()
            .Select(x => new SubscriptionSnapshot(x.TenantId, x.SubscriptionStatus))
            .ToListAsync(cancellationToken);

        var trialTenantIds = subscriptions
            .Where(x => IsStatus(x.SubscriptionStatus, "TRIAL"))
            .Select(x => x.TenantId)
            .ToHashSet();

        var suspendedTenants = tenants.Count(x => IsStatus(x.Status, "suspended"));
        var setupPendingTenants = tenants.Count(x =>
            IsStatus(x.Status, "setup_pending") || IsStatus(x.Status, "pending_payment"));
        var pastDueSubscriptions = subscriptions.Count(x => IsStatus(x.SubscriptionStatus, "PAST_DUE"));

        var attentionItems = new List<PlatformDashboardAttentionItemDto>
        {
            Attention(
                "suspended_tenants",
                "Suspended Tenants",
                "Tenants currently suspended.",
                suspendedTenants,
                "critical"),
            Attention(
                "setup_pending",
                "Setup Pending",
                "Tenants awaiting payment or initial setup.",
                setupPendingTenants,
                "warning"),
            Attention(
                "past_due_subscriptions",
                "Past Due Subscriptions",
                "Tenant subscriptions with past due billing status.",
                pastDueSubscriptions,
                "critical"),
            Attention(
                "pending_billing",
                "Pending Billing",
                "Tenants with pending, failed, or overdue billing status.",
                tenants.Count(x =>
                    IsStatus(x.BillingStatus, "pending") ||
                    IsStatus(x.BillingStatus, "failed") ||
                    IsStatus(x.BillingStatus, "overdue")),
                "warning")
        };

        var recentTenants = tenants
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new PlatformDashboardRecentTenantDto(
                x.Id,
                x.TenantCode,
                x.Name,
                x.Status,
                x.CreatedAt))
            .ToList();

        return new PlatformDashboardResponse(
            TotalTenants: tenants.Count,
            ActiveTenants: tenants.Count(x => IsStatus(x.Status, "active")),
            SuspendedTenants: suspendedTenants,
            TrialTenants: tenants.Count(x => trialTenantIds.Contains(x.Id)),
            TotalSubscriptions: subscriptions.Count,
            ActiveSubscriptions: subscriptions.Count(x => IsStatus(x.SubscriptionStatus, "ACTIVE")),
            PendingBillingCount: tenants.Count(x =>
                IsStatus(x.BillingStatus, "pending") ||
                IsStatus(x.BillingStatus, "failed") ||
                IsStatus(x.BillingStatus, "overdue")),
            TotalOutlets: await CountNonDeletedAsync(_dbContext.Outlets.Select(x => x.Status), cancellationToken),
            TotalTills: await CountNonDeletedAsync(_dbContext.Tills.Select(x => x.Status), cancellationToken),
            TotalUsers: await CountNonDeletedAsync(_dbContext.TenantUsers.Select(x => x.Status), cancellationToken),
            RecentTenants: recentTenants,
            AttentionItems: attentionItems,
            GeneratedAt: generatedAt);
    }

    private static async Task<int> CountNonDeletedAsync(
        IQueryable<string> statuses,
        CancellationToken cancellationToken)
    {
        var values = await statuses.ToListAsync(cancellationToken);
        return values.Count(x => !IsStatus(x, "DELETED"));
    }

    private static bool IsStatus(string value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static PlatformDashboardAttentionItemDto Attention(
        string type,
        string title,
        string description,
        int count,
        string severity) =>
        new(type, title, description, count, severity);

    private sealed record TenantSnapshot(
        Guid Id,
        string TenantCode,
        string Name,
        string Status,
        string BillingStatus,
        DateTimeOffset CreatedAt);

    private sealed record SubscriptionSnapshot(Guid TenantId, string SubscriptionStatus);
}


