using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

public sealed partial class PlatformTenantRepository : IPlatformTenantRepository
{
    private static readonly string[] CurrentSubscriptionStatuses = ["TRIAL", "ACTIVE", "PAST_DUE"];

    private readonly EPosDbContext _dbContext;

    public PlatformTenantRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformTenantListResponse> GetTenantsAsync(
        PlatformTenantListQuery query,
        CancellationToken cancellationToken)
    {
        var rows = await BuildTenantRowsAsync(cancellationToken);
        var filtered = ApplyFilters(rows, query).ToList();
        var sorted = ApplySort(filtered, query).ToList();
        var totalCount = sorted.Count;
        var pageItems = sorted
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(MapListItem)
            .ToList();

        return new PlatformTenantListResponse(
            pageItems,
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    public async Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var rows = await BuildTenantRowsAsync(cancellationToken);

        return new PlatformTenantSummaryResponse(
            TotalTenants: rows.Count,
            ActiveTenants: rows.Count(x => IsStatus(x.Status, "active")),
            SuspendedTenants: rows.Count(x => IsStatus(x.Status, "suspended")),
            TrialTenants: rows.Count(x => x.HasTrialSubscription),
            PendingActivationTenants: rows.Count(x =>
                IsStatus(x.Status, "setup_pending") || IsStatus(x.Status, "pending_payment")),
            PendingBillingCount: rows.Count(x =>
                IsStatus(x.BillingStatus, "pending") ||
                IsStatus(x.BillingStatus, "failed") ||
                IsStatus(x.BillingStatus, "overdue")),
            TotalOutlets: rows.Sum(x => x.OutletCount),
            TotalTills: rows.Sum(x => x.TillCount));
    }

    public async Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var statuses = await _dbContext.Tenants
            .AsNoTracking()
            .Select(x => x.Status)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var billingStatuses = await _dbContext.Tenants
            .AsNoTracking()
            .Select(x => x.BillingStatus)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var operatingModes = await _dbContext.Tenants
            .AsNoTracking()
            .Select(x => x.OperatingMode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var plans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new PlatformTenantFilterOptionPlanDto(x.Id, x.Name, x.PlanCode))
            .ToListAsync(cancellationToken);

        return new PlatformTenantFilterOptionsResponse(
            statuses,
            billingStatuses,
            operatingModes,
            plans);
    }

    public async Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var subscription = await (
            from tenantSubscription in _dbContext.TenantSubscriptions.AsNoTracking()
            join plan in _dbContext.SubscriptionPlans.AsNoTracking()
                on tenantSubscription.SubscriptionPlanId equals plan.Id
            where tenantSubscription.TenantId == tenantId
            orderby tenantSubscription.CreatedAt descending
            select new
            {
                tenantSubscription.SubscriptionPlanId,
                plan.Name,
                tenantSubscription.SubscriptionStatus,
                tenantSubscription.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var currentSubscription = subscription
            .FirstOrDefault(x => IsAnyStatus(x.SubscriptionStatus, CurrentSubscriptionStatuses))
            ?? subscription.FirstOrDefault();

        var userStatuses = await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var userCount = userStatuses.Count(x => !IsStatus(x, "DELETED"));

        var outletStatuses = await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var outletCount = outletStatuses.Count(x => !IsStatus(x, "DELETED"));

        var tillStatuses = await _dbContext.Tills
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var tillCount = tillStatuses.Count(x => !IsStatus(x, "DELETED"));

        var enabledFeatureCodes = await (
            from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on entitlement.PlatformFeatureId equals feature.Id
            where entitlement.TenantId == tenantId
            select new { entitlement.EntitlementStatus, feature.FeatureCode })
            .ToListAsync(cancellationToken);

        var featureCodes = enabledFeatureCodes
            .Where(x => IsStatus(x.EntitlementStatus, "ENABLED"))
            .Select(x => x.FeatureCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var lastActivityAt = await GetLastActivityAtAsync(tenantId, tenant.UpdatedAt, cancellationToken);

        var profile = await _dbContext.TenantProfiles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new PlatformTenantProfileDetailDto(
                x.LegalName,
                x.PrimaryContactName,
                x.PrimaryEmail,
                x.PrimaryPhone,
                x.WebsiteUrl))
            .FirstOrDefaultAsync(cancellationToken);

        var address = await _dbContext.TenantAddresses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.AddressType == "REGISTERED")
            .ThenByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PlatformTenantAddressDetailDto(
                x.AddressType,
                x.Line1,
                x.Line2,
                x.City,
                x.State,
                x.PostalCode,
                x.CountryCode))
            .FirstOrDefaultAsync(cancellationToken);

        PlatformTenantDetailSubscriptionDto? subscriptionDto = currentSubscription is null
            ? null
            : new PlatformTenantDetailSubscriptionDto(
                currentSubscription.SubscriptionPlanId,
                currentSubscription.Name,
                currentSubscription.SubscriptionStatus,
                TrialEndsAt: null,
                StartsAt: null,
                EndsAt: null);

        return new PlatformTenantDetailResponse(
            tenant.Id,
            tenant.TenantCode,
            tenant.Name,
            tenant.Status,
            tenant.BillingStatus,
            tenant.OperatingMode,
            tenant.BaseCurrency,
            tenant.DefaultTimezone,
            tenant.DefaultLocale,
            tenant.BusinessType,
            Profile: profile,
            PrimaryAddress: address,
            Subscription: subscriptionDto,
            userCount,
            outletCount,
            tillCount,
            HasFeature(featureCodes, PlatformTenantFeatureCodes.OnlineStore),
            HasFeature(featureCodes, PlatformTenantFeatureCodes.ClickCollect),
            HasFeature(featureCodes, PlatformTenantFeatureCodes.OfflineOperationSync),
            tenant.CreatedAt,
            tenant.UpdatedAt,
            lastActivityAt,
            CanUpdate: false,
            CanActivate: false,
            CanSuspend: false,
            CanManageEntitlements: false);
    }

    public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken)
    {
        var normalizedCode = tenantCode.Trim();
        return _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.TenantCode == normalizedCode,
                cancellationToken);
    }

    public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _dbContext.Tenants
            .FirstOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }

    public async Task AddTenantWithSubscriptionAndEntitlementsAsync(
        Tenant tenant,
        TenantSubscription subscription,
        IReadOnlyList<Guid> enabledFeatureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _dbContext.Tenants.Add(tenant);
        _dbContext.TenantSubscriptions.Add(subscription);

        foreach (var featureId in enabledFeatureIds.Distinct())
        {
            _dbContext.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenant.Id,
                featureId,
                TenantEntitlementStatusConstants.Enabled,
                now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TenantSubscriptions
            .Where(subscription =>
                subscription.TenantId == tenantId &&
                CurrentSubscriptionStatuses.Contains(subscription.SubscriptionStatus))
            .OrderByDescending(subscription => subscription.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task UpdateTenantSubscriptionAsync(
        TenantSubscription subscription,
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceTenantEntitlementsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> enabledFeatureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.TenantFeatureEntitlements
            .Where(entitlement => entitlement.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        _dbContext.TenantFeatureEntitlements.RemoveRange(existing);

        foreach (var featureId in enabledFeatureIds.Distinct())
        {
            _dbContext.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenantId,
                featureId,
                TenantEntitlementStatusConstants.Enabled,
                now));
        }

        var tenant = await _dbContext.Tenants
            .FirstAsync(item => item.Id == tenantId, cancellationToken);

        tenant.TouchUpdatedAt(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        var featureIds = await _dbContext.SubscriptionPlanFeatures
            .AsNoTracking()
            .Where(feature =>
                feature.SubscriptionPlanId == planId &&
                feature.Status == SubscriptionPlanConstants.PlanFeatureStatus.Included)
            .Select(feature => feature.PlatformFeatureId)
            .ToListAsync(cancellationToken);

        return featureIds.ToHashSet();
    }

    public async Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
        IReadOnlyList<Guid>? featureIds,
        IReadOnlyList<string>? featureCodes,
        CancellationToken cancellationToken)
    {
        var requestedFeatureIds = featureIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        var requestedFeatureCodes = featureCodes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        if (requestedFeatureIds.Count == 0 && requestedFeatureCodes.Count == 0)
        {
            return [];
        }

        var query = _dbContext.PlatformFeatures
            .AsNoTracking()
            .Where(feature => feature.Status == "ACTIVE");

        if (requestedFeatureIds.Count > 0 && requestedFeatureCodes.Count > 0)
        {
            query = query.Where(feature =>
                requestedFeatureIds.Contains(feature.Id) ||
                requestedFeatureCodes.Contains(feature.FeatureCode));
        }
        else if (requestedFeatureIds.Count > 0)
        {
            query = query.Where(feature => requestedFeatureIds.Contains(feature.Id));
        }
        else
        {
            query = query.Where(feature => requestedFeatureCodes.Contains(feature.FeatureCode));
        }

        return await query
            .Select(feature => new ResolvedTenantFeature(feature.Id, feature.FeatureCode))
            .ToListAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> GetLastActivityAtAsync(
        Guid tenantId,
        DateTimeOffset? tenantUpdatedAt,
        CancellationToken cancellationToken)
    {
        var loginAudits = await (
            from audit in _dbContext.TenantLoginAudits.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on audit.TenantUserId equals user.Id
            where user.TenantId == tenantId
            select audit.CreatedAt)
            .ToListAsync(cancellationToken);

        DateTimeOffset? lastActivity = tenantUpdatedAt;

        if (loginAudits.Count > 0)
        {
            var latestLogin = loginAudits.Max();
            if (!lastActivity.HasValue || latestLogin > lastActivity.Value)
            {
                lastActivity = latestLogin;
            }
        }

        return lastActivity;
    }

    private async Task<List<TenantRow>> BuildTenantRowsAsync(CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants.AsNoTracking().ToListAsync(cancellationToken);

        var subscriptions = await (
            from subscription in _dbContext.TenantSubscriptions.AsNoTracking()
            join plan in _dbContext.SubscriptionPlans.AsNoTracking()
                on subscription.SubscriptionPlanId equals plan.Id
            select new SubscriptionRow(
                subscription.TenantId,
                subscription.SubscriptionPlanId,
                plan.Name,
                plan.PlanCode,
                subscription.SubscriptionStatus,
                subscription.CreatedAt))
            .ToListAsync(cancellationToken);

        var currentSubscriptions = subscriptions
            .Where(x => IsAnyStatus(x.SubscriptionStatus, CurrentSubscriptionStatuses))
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.CreatedAt).First());

        var trialTenantIds = subscriptions
            .Where(x => IsStatus(x.SubscriptionStatus, "TRIAL"))
            .Select(x => x.TenantId)
            .ToHashSet();

        var userCounts = await CountNonDeletedByTenantAsync(
            _dbContext.TenantUsers.AsNoTracking().Select(x => new TenantCountRow(x.TenantId, x.Status)),
            cancellationToken);

        var outletCounts = await CountNonDeletedByTenantAsync(
            _dbContext.Outlets.AsNoTracking().Select(x => new TenantCountRow(x.TenantId, x.Status)),
            cancellationToken);

        var tillCounts = await CountNonDeletedByTenantAsync(
            _dbContext.Tills.AsNoTracking().Select(x => new TenantCountRow(x.TenantId, x.Status)),
            cancellationToken);

        var entitlementRows = await (
            from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
            join feature in _dbContext.PlatformFeatures.AsNoTracking()
                on entitlement.PlatformFeatureId equals feature.Id
            select new { entitlement.TenantId, entitlement.EntitlementStatus, feature.FeatureCode })
            .ToListAsync(cancellationToken);

        var enabledFeatures = entitlementRows
            .Where(x => IsStatus(x.EntitlementStatus, "ENABLED"))
            .Select(x => new { x.TenantId, x.FeatureCode })
            .ToList();

        var featuresByTenant = enabledFeatures
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.FeatureCode).ToHashSet(StringComparer.OrdinalIgnoreCase));

        return tenants
            .Select(tenant =>
            {
                currentSubscriptions.TryGetValue(tenant.Id, out var subscription);
                featuresByTenant.TryGetValue(tenant.Id, out var featureCodes);
                featureCodes ??= [];

                return new TenantRow
                {
                    Id = tenant.Id,
                    Code = tenant.TenantCode,
                    Name = tenant.Name,
                    Status = tenant.Status,
                    BillingStatus = tenant.BillingStatus,
                    OperatingMode = tenant.OperatingMode,
                    BaseCurrency = tenant.BaseCurrency,
                    DefaultTimezone = tenant.DefaultTimezone,
                    DefaultLocale = tenant.DefaultLocale,
                    BusinessType = tenant.BusinessType,
                    CreatedAt = tenant.CreatedAt,
                    UpdatedAt = tenant.UpdatedAt,
                    OutletCount = outletCounts.GetValueOrDefault(tenant.Id),
                    TillCount = tillCounts.GetValueOrDefault(tenant.Id),
                    UserCount = userCounts.GetValueOrDefault(tenant.Id),
                    HasTrialSubscription = trialTenantIds.Contains(tenant.Id),
                    Subscription = subscription,
                    OnlineStoreEnabled = HasFeature(featureCodes, PlatformTenantFeatureCodes.OnlineStore),
                    ClickCollectEnabled = HasFeature(featureCodes, PlatformTenantFeatureCodes.ClickCollect),
                    OfflineEnabled = HasFeature(featureCodes, PlatformTenantFeatureCodes.OfflineOperationSync)
                };
            })
            .ToList();
    }

    private static async Task<Dictionary<Guid, int>> CountNonDeletedByTenantAsync(
        IQueryable<TenantCountRow> source,
        CancellationToken cancellationToken)
    {
        var rows = await source.ToListAsync(cancellationToken);

        return rows
            .Where(x => !IsStatus(x.Status, "DELETED"))
            .GroupBy(x => x.TenantId)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static IEnumerable<TenantRow> ApplyFilters(IEnumerable<TenantRow> rows, PlatformTenantListQuery query)
    {
        var filtered = rows;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            filtered = filtered.Where(row =>
                row.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                row.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (row.BusinessType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            filtered = filtered.Where(row => IsStatus(row.Status, status));
        }

        if (!string.IsNullOrWhiteSpace(query.BillingStatus))
        {
            var billingStatus = query.BillingStatus.Trim();
            filtered = filtered.Where(row => IsStatus(row.BillingStatus, billingStatus));
        }

        if (query.PlanId.HasValue && query.PlanId.Value != Guid.Empty)
        {
            filtered = filtered.Where(row =>
                row.Subscription is not null &&
                row.Subscription.PlanId == query.PlanId.Value);
        }

        return filtered;
    }

    private static IEnumerable<TenantRow> ApplySort(IEnumerable<TenantRow> rows, PlatformTenantListQuery query)
    {
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "code" => OrderBy(rows, row => row.Code, descending),
            "name" => OrderBy(rows, row => row.Name, descending),
            "status" => OrderBy(rows, row => row.Status, descending),
            "billingstatus" => OrderBy(rows, row => row.BillingStatus, descending),
            "operatingmode" => OrderBy(rows, row => row.OperatingMode, descending),
            "outletcount" => OrderBy(rows, row => row.OutletCount, descending),
            "tillcount" => OrderBy(rows, row => row.TillCount, descending),
            "usercount" => OrderBy(rows, row => row.UserCount, descending),
            "updatedat" => OrderBy(rows, row => row.UpdatedAt ?? DateTimeOffset.MinValue, descending),
            "createdat" or _ => OrderBy(rows, row => row.CreatedAt, descending)
        };
    }

    private static IEnumerable<TenantRow> OrderBy<TKey>(
        IEnumerable<TenantRow> rows,
        Func<TenantRow, TKey> keySelector,
        bool descending) =>
        descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);

    private static PlatformTenantListItemDto MapListItem(TenantRow row)
    {
        PlatformTenantSubscriptionSummaryDto? subscription = row.Subscription is null
            ? null
            : new PlatformTenantSubscriptionSummaryDto(
                row.Subscription.PlanId,
                row.Subscription.PlanName,
                row.Subscription.PlanCode,
                row.Subscription.SubscriptionStatus);

        return new PlatformTenantListItemDto(
            row.Id,
            row.Code,
            row.Name,
            row.Status,
            row.BillingStatus,
            row.OperatingMode,
            row.BaseCurrency,
            row.DefaultTimezone,
            row.DefaultLocale,
            row.BusinessType,
            subscription,
            row.OutletCount,
            row.TillCount,
            row.UserCount,
            row.OnlineStoreEnabled,
            row.ClickCollectEnabled,
            row.OfflineEnabled,
            row.CreatedAt,
            row.UpdatedAt);
    }

    private static bool HasFeature(IReadOnlySet<string> featureCodes, string featureCode) =>
        featureCodes.Contains(featureCode);

    private static bool IsStatus(string value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsAnyStatus(string value, IReadOnlyList<string> expectedValues) =>
        expectedValues.Any(expected => IsStatus(value, expected));

    private sealed record TenantCountRow(Guid TenantId, string Status);

    private sealed class TenantRow
    {
        public Guid Id { get; init; }

        public string Code { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public string BillingStatus { get; init; } = string.Empty;

        public string OperatingMode { get; init; } = string.Empty;

        public string BaseCurrency { get; init; } = string.Empty;

        public string DefaultTimezone { get; init; } = string.Empty;

        public string DefaultLocale { get; init; } = string.Empty;

        public string? BusinessType { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public int OutletCount { get; init; }

        public int TillCount { get; init; }

        public int UserCount { get; init; }

        public bool HasTrialSubscription { get; init; }

        public SubscriptionRow? Subscription { get; init; }

        public bool OnlineStoreEnabled { get; init; }

        public bool ClickCollectEnabled { get; init; }

        public bool OfflineEnabled { get; init; }
    }

    private sealed record SubscriptionRow(
        Guid TenantId,
        Guid PlanId,
        string PlanName,
        string PlanCode,
        string SubscriptionStatus,
        DateTimeOffset CreatedAt);
}
