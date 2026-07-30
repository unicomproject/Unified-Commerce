using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformDashboardService : IPlatformDashboardService
{
    public static readonly string[] SetupPendingStatuses =
    [
        TenantStatusConstants.Draft,
        TenantStatusConstants.SetupPending,
        TenantStatusConstants.PendingActivation,
        TenantStatusConstants.PendingPayment
    ];

    private static readonly ApplicationError AccessDenied = new(
        "platform_dashboard.access_denied",
        "Platform dashboard access denied.");

    private readonly IPlatformDashboardRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IPlatformDashboardHealthProbe _healthProbe;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformDashboardService(
        IPlatformDashboardRepository repository,
        IPlatformPermissionChecker permissionChecker,
        IPlatformDashboardHealthProbe healthProbe,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
        _healthProbe = healthProbe;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PlatformDashboardResponse>> GetDashboardAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await _permissionChecker.HasPermissionAsync(
                platformUserId,
                PlatformPermissionCodes.DashboardView,
                cancellationToken))
        {
            return ApplicationResult<PlatformDashboardResponse>.Failure(AccessDenied);
        }

        var canTenantSubscriptionsView = await _permissionChecker.HasPermissionAsync(
            platformUserId, PlatformPermissionCodes.TenantSubscriptionsView, cancellationToken);
        var canBillingView = await _permissionChecker.HasPermissionAsync(
            platformUserId, PlatformPermissionCodes.BillingView, cancellationToken);
        var canUsersView = await _permissionChecker.HasPermissionAsync(
            platformUserId, PlatformPermissionCodes.UsersView, cancellationToken);

        var generatedAt = _dateTimeProvider.UtcNow;
        PlatformDashboardComputationSnapshot snapshot;
        try
        {
            snapshot = await _repository.GetComputationSnapshotAsync(generatedAt, cancellationToken);
        }
        catch (Exception)
        {
            return ApplicationResult<PlatformDashboardResponse>.Failure(new ApplicationError(
                PlatformDashboardErrorCodes.SectionCalculationFailed,
                "Platform dashboard could not be loaded."));
        }

        var tenantSummary = BuildTenantSummary(snapshot);
        var subscriptionSummary = canTenantSubscriptionsView
            ? BuildSubscriptionSummary(snapshot)
            : PermissionDeniedSection<PlatformDashboardSubscriptionSummaryDto>();
        var revenueSummary = canTenantSubscriptionsView && canBillingView
            ? BuildRevenueSummary(snapshot, generatedAt)
            : PermissionDeniedSection<PlatformDashboardRevenueSummaryDto>();
        var trends = BuildTrends(snapshot, canTenantSubscriptionsView, canBillingView, generatedAt);
        var attention = BuildAttention(snapshot, canTenantSubscriptionsView, canBillingView);
        var footprint = BuildFootprint(snapshot, canUsersView);
        var systemHealth = await BuildSystemHealthAsync(cancellationToken);
        var recentTenants = BuildRecentTenants(snapshot);

        var response = MapResponse(
            generatedAt,
            tenantSummary,
            subscriptionSummary,
            revenueSummary,
            trends,
            attention,
            footprint,
            systemHealth,
            recentTenants,
            snapshot,
            canTenantSubscriptionsView,
            canBillingView,
            canUsersView);

        return ApplicationResult<PlatformDashboardResponse>.Success(response);
    }

    private static PlatformDashboardSectionDto<PlatformDashboardTenantSummaryDto> BuildTenantSummary(
        PlatformDashboardComputationSnapshot snapshot)
    {
        try
        {
            var active = CountStatus(snapshot.Tenants, TenantStatusConstants.Active);
            var suspended = CountStatus(snapshot.Tenants, TenantStatusConstants.Suspended);
            var inactive = CountStatus(snapshot.Tenants, TenantStatusConstants.Inactive);
            var setupPending = snapshot.Tenants.Count(t => IsSetupPending(t.Status));
            var lifecycle = new List<PlatformDashboardLifecycleBucketDto>
            {
                new("Active", active),
                new("Setup Pending", setupPending),
                new("Suspended", suspended),
                new("Inactive", inactive)
            };

            return Success(new PlatformDashboardTenantSummaryDto(
                snapshot.Tenants.Count,
                active,
                setupPending,
                suspended,
                inactive,
                lifecycle));
        }
        catch
        {
            return Unavailable<PlatformDashboardTenantSummaryDto>(PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private static PlatformDashboardSectionDto<PlatformDashboardSubscriptionSummaryDto> BuildSubscriptionSummary(
        PlatformDashboardComputationSnapshot snapshot)
    {
        try
        {
            var data = new PlatformDashboardSubscriptionSummaryDto(
                snapshot.Subscriptions.Count,
                CountSubStatus(snapshot.Subscriptions, TenantSubscriptionStatusConstants.Trial),
                CountSubStatus(snapshot.Subscriptions, TenantSubscriptionStatusConstants.Active),
                CountSubStatus(snapshot.Subscriptions, TenantSubscriptionStatusConstants.PastDue),
                CountSubStatus(snapshot.Subscriptions, TenantSubscriptionStatusConstants.Cancelled),
                CountSubStatus(snapshot.Subscriptions, TenantSubscriptionStatusConstants.Expired));
            return Success(data);
        }
        catch
        {
            return Unavailable<PlatformDashboardSubscriptionSummaryDto>(PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private PlatformDashboardSectionDto<PlatformDashboardRevenueSummaryDto> BuildRevenueSummary(
        PlatformDashboardComputationSnapshot snapshot,
        DateTimeOffset generatedAt)
    {
        try
        {
            var inputs = snapshot.Subscriptions.Select(sub =>
            {
                var addons = snapshot.Addons
                    .Where(a => a.TenantSubscriptionId == sub.Id)
                    .Select(a => new PlatformDashboardMrrCalculator.AddonMrrInput(
                        a.Status, a.UnitPrice, a.Quantity, a.CurrencyCode, a.AutoRenew))
                    .ToList();
                return new PlatformDashboardMrrCalculator.SubscriptionMrrInput(
                    sub.Id,
                    sub.SubscriptionStatus,
                    sub.CurrencyCode,
                    sub.PlanPrice,
                    sub.BillingCycle,
                    sub.PlanBillingInterval,
                    sub.DiscountType,
                    sub.DiscountValue,
                    addons);
            }).ToList();

            var result = PlatformDashboardMrrCalculator.Calculate(inputs, snapshot.Currencies);
            if (!result.Success)
            {
                // Secure operational signal only — CurrencyCode is not returned to clients.
                System.Diagnostics.Trace.TraceWarning(
                    "Platform dashboard MRR unavailable due to currency metadata. CurrencyCode={0}",
                    result.FailedCurrencyCode);
                return Unavailable<PlatformDashboardRevenueSummaryDto>(
                    result.ErrorCode ?? PlatformDashboardErrorCodes.CurrencyMetadataUnavailable);
            }

            return Success(new PlatformDashboardRevenueSummaryDto(result.Groups, generatedAt));
        }
        catch (Exception)
        {
            return Unavailable<PlatformDashboardRevenueSummaryDto>(PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private static PlatformDashboardSectionDto<PlatformDashboardTrendsDto>? BuildTrends(
        PlatformDashboardComputationSnapshot snapshot,
        bool canTenantSubscriptionsView,
        bool canBillingView,
        DateTimeOffset generatedAt)
    {
        try
        {
            if (!PlatformDashboardTrendCalculator.TryGetTimeZone(snapshot.PlatformTimezone, out var timeZone, out var error))
            {
                return Unavailable<PlatformDashboardTrendsDto>(error ?? PlatformDashboardErrorCodes.TimezoneUnavailable);
            }

            var tenantGrowth = PlatformDashboardTrendCalculator.BuildCountSeries(
                "tenants",
                snapshot.TenantCreatedEvents,
                generatedAt,
                timeZone!);

            PlatformDashboardTrendSeriesDto? subscriptionTrend = null;
            if (canTenantSubscriptionsView)
            {
                var historyStates = BuildHistoryStates(snapshot);
                subscriptionTrend = PlatformDashboardHistoricalTrendBuilder.BuildActiveSubscriptionSeries(
                    historyStates,
                    generatedAt,
                    timeZone!);
            }

            var mrrTrends = new List<PlatformDashboardTrendSeriesDto>();
            if (canTenantSubscriptionsView && canBillingView)
            {
                var historyStates = BuildHistoryStates(snapshot);
                var mrrSeries = PlatformDashboardHistoricalTrendBuilder.BuildMrrSeries(
                    historyStates,
                    snapshot.Currencies,
                    generatedAt,
                    timeZone!);
                if (!mrrSeries.Success)
                {
                    // Keep tenant/subscription trends; surface incomplete MRR history as empty series with section still SUCCESS
                    // unless currency metadata is the failure (then fail whole trends? Prefer keep tenant trends).
                    if (string.Equals(mrrSeries.ErrorCode, PlatformDashboardErrorCodes.CurrencyMetadataUnavailable, StringComparison.Ordinal))
                    {
                        // MRR trend cannot be computed; leave empty — revenue section handles metadata separately.
                        mrrTrends = [];
                    }
                    else
                    {
                        mrrTrends = [];
                    }
                }
                else
                {
                    mrrTrends = mrrSeries.Series.ToList();
                }
            }

            return Success(new PlatformDashboardTrendsDto(
                snapshot.PlatformTimezone!.Trim(),
                tenantGrowth,
                subscriptionTrend,
                mrrTrends));
        }
        catch
        {
            return Unavailable<PlatformDashboardTrendsDto>(PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private static List<PlatformDashboardHistoricalTrendBuilder.SubscriptionHistoryState> BuildHistoryStates(
        PlatformDashboardComputationSnapshot snapshot)
    {
        return snapshot.Subscriptions.Select(sub =>
        {
            var addons = snapshot.Addons
                .Where(a => a.TenantSubscriptionId == sub.Id)
                .Select(a => new PlatformDashboardMrrCalculator.AddonMrrInput(
                    a.Status, a.UnitPrice, a.Quantity, a.CurrencyCode, a.AutoRenew))
                .ToList();
            var events = snapshot.SubscriptionHistory
                .Where(h => h.TenantSubscriptionId == sub.Id)
                .Select(h => new PlatformDashboardHistoricalTrendBuilder.HistoryEvent(
                    h.TenantSubscriptionId,
                    h.ChangeType,
                    h.ChangedAt,
                    h.OldStatus,
                    h.NewStatus,
                    h.ChangeData))
                .ToList();
            return new PlatformDashboardHistoricalTrendBuilder.SubscriptionHistoryState(
                sub.Id,
                sub.CurrencyCode,
                sub.PlanPrice,
                sub.BillingCycle,
                sub.PlanBillingInterval,
                sub.DiscountType,
                sub.DiscountValue,
                sub.SubscriptionStatus,
                sub.StartedAt,
                sub.CreatedAt,
                addons,
                events);
        }).ToList();
    }

    private static PlatformDashboardSectionDto<PlatformDashboardAttentionSummaryDto> BuildAttention(
        PlatformDashboardComputationSnapshot snapshot,
        bool canTenantSubscriptionsView,
        bool canBillingView)
    {
        try
        {
            var items = new List<PlatformDashboardAttentionItemDto>
            {
                Attention(
                    "suspended_tenants",
                    "Suspended Tenants",
                    "Tenants currently suspended.",
                    CountStatus(snapshot.Tenants, TenantStatusConstants.Suspended),
                    "critical"),
                Attention(
                    "setup_pending",
                    "Setup Pending",
                    "Tenants awaiting payment or initial setup.",
                    snapshot.Tenants.Count(t => IsSetupPending(t.Status)),
                    "warning")
            };

            if (canTenantSubscriptionsView)
            {
                items.Add(Attention(
                    "past_due_subscriptions",
                    "Past Due Subscriptions",
                    "Tenant subscriptions with PAST_DUE status.",
                    CountSubStatus(snapshot.Subscriptions, TenantSubscriptionStatusConstants.PastDue),
                    "critical"));
            }

            if (canBillingView)
            {
                items.Add(Attention(
                    "pending_billing",
                    "Pending Billing",
                    "Issued invoices that are PENDING with a balance due.",
                    snapshot.PendingBillingCount,
                    "warning"));
            }

            return Success(new PlatformDashboardAttentionSummaryDto(items, items.Sum(x => x.Count)));
        }
        catch
        {
            return Unavailable<PlatformDashboardAttentionSummaryDto>(PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private static PlatformDashboardSectionDto<PlatformDashboardFootprintDto> BuildFootprint(
        PlatformDashboardComputationSnapshot snapshot,
        bool canUsersView)
    {
        try
        {
            return Success(new PlatformDashboardFootprintDto(
                snapshot.TotalOutlets,
                snapshot.TotalTills,
                snapshot.TotalTenantUsers,
                canUsersView ? snapshot.TotalPlatformUsers : null));
        }
        catch
        {
            return Unavailable<PlatformDashboardFootprintDto>(PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private async Task<PlatformDashboardSectionDto<PlatformDashboardSystemHealthDto>> BuildSystemHealthAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var health = await _healthProbe.ProbeAsync(cancellationToken);
            return Success(health);
        }
        catch (Exception)
        {
            return Unavailable<PlatformDashboardSystemHealthDto>(PlatformDashboardErrorCodes.HealthProbeFailed);
        }
    }

    private static PlatformDashboardSectionDto<IReadOnlyList<PlatformDashboardRecentTenantDto>> BuildRecentTenants(
        PlatformDashboardComputationSnapshot snapshot)
    {
        try
        {
            var recent = snapshot.Tenants
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new PlatformDashboardRecentTenantDto(
                    x.Id, x.TenantCode, x.Name, x.Status, x.CreatedAt))
                .ToList();
            return Success<IReadOnlyList<PlatformDashboardRecentTenantDto>>(recent);
        }
        catch
        {
            return Unavailable<IReadOnlyList<PlatformDashboardRecentTenantDto>>(
                PlatformDashboardErrorCodes.SectionCalculationFailed);
        }
    }

    private static PlatformDashboardResponse MapResponse(
        DateTimeOffset generatedAt,
        PlatformDashboardSectionDto<PlatformDashboardTenantSummaryDto> tenantSummary,
        PlatformDashboardSectionDto<PlatformDashboardSubscriptionSummaryDto>? subscriptionSummary,
        PlatformDashboardSectionDto<PlatformDashboardRevenueSummaryDto>? revenueSummary,
        PlatformDashboardSectionDto<PlatformDashboardTrendsDto>? trends,
        PlatformDashboardSectionDto<PlatformDashboardAttentionSummaryDto> attention,
        PlatformDashboardSectionDto<PlatformDashboardFootprintDto> footprint,
        PlatformDashboardSectionDto<PlatformDashboardSystemHealthDto> systemHealth,
        PlatformDashboardSectionDto<IReadOnlyList<PlatformDashboardRecentTenantDto>> recentTenants,
        PlatformDashboardComputationSnapshot snapshot,
        bool canTenantSubscriptionsView,
        bool canBillingView,
        bool canUsersView)
    {
        var tenantData = tenantSummary.Data;
        var subData = subscriptionSummary?.Status == PlatformDashboardSectionStatuses.Success
            ? subscriptionSummary.Data
            : null;
        var footprintData = footprint.Data;

        return new PlatformDashboardResponse(
            GeneratedAt: generatedAt,
            TenantSummary: tenantSummary,
            SubscriptionSummary: subscriptionSummary,
            RevenueSummary: revenueSummary,
            Trends: trends,
            AttentionSummary: attention,
            PlatformFootprint: footprint,
            SystemHealth: systemHealth,
            RecentTenants: recentTenants,
            TotalTenants: tenantData?.TotalTenants,
            ActiveTenants: tenantData?.ActiveTenants,
            SuspendedTenants: tenantData?.SuspendedTenants,
            InactiveTenants: tenantData?.InactiveTenants,
            SetupPendingTenants: tenantData?.SetupPendingTenants,
            TrialTenants: canTenantSubscriptionsView ? subData?.TrialSubscriptions : null,
            TotalSubscriptions: canTenantSubscriptionsView ? subData?.TotalSubscriptions : null,
            ActiveSubscriptions: canTenantSubscriptionsView ? subData?.ActiveSubscriptions : null,
            PastDueSubscriptions: canTenantSubscriptionsView ? subData?.PastDueSubscriptions : null,
            CancelledSubscriptions: canTenantSubscriptionsView ? subData?.CancelledSubscriptions : null,
            ExpiredSubscriptions: canTenantSubscriptionsView ? subData?.ExpiredSubscriptions : null,
            PendingBillingCount: canBillingView ? snapshot.PendingBillingCount : null,
            TotalOutlets: footprintData?.TotalOutlets,
            TotalTills: footprintData?.TotalTills,
            TotalUsers: footprintData?.TotalTenantUsers,
            TotalTenantUsers: footprintData?.TotalTenantUsers,
            TotalPlatformUsers: canUsersView ? footprintData?.TotalPlatformUsers : null,
            RecentTenantsList: recentTenants.Data,
            AttentionItems: attention.Data?.Items);
    }

    public static bool IsSetupPending(string status) =>
        SetupPendingStatuses.Any(expected =>
            string.Equals(status, expected, StringComparison.OrdinalIgnoreCase));

    private static int CountStatus(IReadOnlyList<PlatformDashboardTenantRow> tenants, string status) =>
        tenants.Count(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

    private static int CountSubStatus(IReadOnlyList<PlatformDashboardSubscriptionRow> subscriptions, string status) =>
        subscriptions.Count(x => string.Equals(x.SubscriptionStatus, status, StringComparison.OrdinalIgnoreCase));

    private static PlatformDashboardAttentionItemDto Attention(
        string type, string title, string description, int count, string severity) =>
        new(type, title, description, count, severity);

    private static PlatformDashboardSectionDto<T> Success<T>(T data) =>
        new(PlatformDashboardSectionStatuses.Success, null, data);

    private static PlatformDashboardSectionDto<T> Unavailable<T>(string errorCode) =>
        new(PlatformDashboardSectionStatuses.Unavailable, errorCode, default);

    private static PlatformDashboardSectionDto<T> PermissionDeniedSection<T>() =>
        new(PlatformDashboardSectionStatuses.PermissionDenied, "platform_dashboard.permission_denied", default);
}
