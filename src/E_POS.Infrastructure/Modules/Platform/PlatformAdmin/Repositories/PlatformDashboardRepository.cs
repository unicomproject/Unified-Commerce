using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
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

    public async Task<PlatformDashboardComputationSnapshot> GetComputationSnapshotAsync(
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Select(x => new PlatformDashboardTenantRow(
                x.Id,
                x.TenantCode,
                x.DisplayName,
                x.Status,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var subscriptions = await (
            from sub in _dbContext.TenantSubscriptions.AsNoTracking()
            join plan in _dbContext.SubscriptionPlans.AsNoTracking()
                on sub.SubscriptionPlanId equals plan.Id into plans
            from plan in plans.DefaultIfEmpty()
            select new PlatformDashboardSubscriptionRow(
                sub.Id,
                sub.TenantId,
                sub.SubscriptionStatus,
                sub.CurrencyCode,
                sub.PlanPrice,
                sub.BillingCycle,
                plan != null ? plan.BillingInterval : null,
                sub.DiscountType,
                sub.DiscountValue,
                sub.CreatedAt,
                sub.StartedAt))
            .ToListAsync(cancellationToken);

        var addons = await _dbContext.TenantSubscriptionAddons
            .AsNoTracking()
            .Select(x => new PlatformDashboardAddonRow(
                x.TenantSubscriptionId,
                x.Status,
                x.UnitPrice,
                x.Quantity,
                x.CurrencyCode,
                x.AutoRenew,
                x.StartsAt,
                x.EndsAt))
            .ToListAsync(cancellationToken);

        var history = await _dbContext.TenantSubscriptionHistory
            .AsNoTracking()
            .Select(x => new PlatformDashboardSubscriptionHistoryRow(
                x.TenantSubscriptionId,
                x.ChangeType,
                x.ChangedAt,
                x.OldStatus,
                x.NewStatus,
                x.ChangeData))
            .ToListAsync(cancellationToken);

        var currencyRows = await _dbContext.Currencies
            .AsNoTracking()
            .Select(x => new { x.CurrencyCode, x.DecimalPlaces, x.IsActive })
            .ToListAsync(cancellationToken);

        var currencies = new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>(StringComparer.OrdinalIgnoreCase);
        var conflicting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in currencyRows.GroupBy(x => x.CurrencyCode.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var active = group.Where(x => x.IsActive).ToList();
            var candidates = active.Count > 0 ? active : group.ToList();
            if (candidates.Count != 1)
            {
                conflicting.Add(group.Key);
                continue;
            }

            var row = candidates[0];
            currencies[group.Key] = new PlatformDashboardMrrCalculator.CurrencyMetadata(
                row.CurrencyCode.Trim().ToUpperInvariant(),
                row.DecimalPlaces);
        }

        // Conflicting currencies are intentionally absent so MRR resolution fails safely.
        foreach (var code in conflicting)
        {
            currencies.Remove(code);
        }

        var pendingBilling = await _dbContext.SubscriptionInvoices.AsNoTracking()
            .CountAsync(x => x.InvoiceStatus == "PENDING" && x.BalanceDue > 0m, cancellationToken);

        var totalOutlets = await CountNonDeletedAsync(_dbContext.Outlets.Select(x => x.Status), cancellationToken);
        var totalTills = await CountNonDeletedAsync(_dbContext.Tills.Select(x => x.Status), cancellationToken);
        var totalTenantUsers = await CountNonDeletedAsync(_dbContext.TenantUsers.Select(x => x.AccountStatus), cancellationToken);
        var totalPlatformUsers = await CountNonDeletedAsync(_dbContext.PlatformUsers.Select(x => x.Status), cancellationToken);

        var timezoneSetting = await _dbContext.PlatformSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettingKey == PlatformSettingKeys.DefaultTimezone, cancellationToken);
        var timezone = timezoneSetting?.GetStringValue();

        return new PlatformDashboardComputationSnapshot(
            generatedAt,
            timezone,
            tenants,
            subscriptions,
            addons,
            history,
            currencies,
            pendingBilling,
            totalOutlets,
            totalTills,
            totalTenantUsers,
            totalPlatformUsers,
            tenants.Select(x => (x.CreatedAt, x.Id)).ToList(),
            subscriptions.Select(x => (x.CreatedAt, x.Id)).ToList());
    }

    private static async Task<int> CountNonDeletedAsync(
        IQueryable<string> statuses,
        CancellationToken cancellationToken)
    {
        var values = await statuses.ToListAsync(cancellationToken);
        return values.Count(x => !string.Equals(x, "DELETED", StringComparison.OrdinalIgnoreCase));
    }
}
