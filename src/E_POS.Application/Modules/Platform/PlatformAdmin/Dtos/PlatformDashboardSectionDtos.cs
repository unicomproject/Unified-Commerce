namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public static class PlatformDashboardSectionStatuses
{
    public const string Success = "SUCCESS";
    public const string Unavailable = "UNAVAILABLE";
    public const string PermissionDenied = "PERMISSION_DENIED";
}

public static class PlatformDashboardErrorCodes
{
    public const string CurrencyMetadataUnavailable = "platform_dashboard.currency_metadata_unavailable";
    public const string TimezoneUnavailable = "platform_dashboard.timezone_unavailable";
    public const string SectionCalculationFailed = "platform_dashboard.section_calculation_failed";
    public const string HealthProbeFailed = "platform_dashboard.health_probe_failed";
    public const string MrrHistoryIncomplete = "platform_dashboard.mrr_history_incomplete";
}

public sealed record PlatformDashboardSectionDto<T>(
    string Status,
    string? ErrorCode,
    T? Data);

public sealed record PlatformDashboardTenantSummaryDto(
    int TotalTenants,
    int ActiveTenants,
    int SetupPendingTenants,
    int SuspendedTenants,
    int InactiveTenants,
    IReadOnlyList<PlatformDashboardLifecycleBucketDto> Lifecycle);

public sealed record PlatformDashboardLifecycleBucketDto(
    string Bucket,
    int Count);

public sealed record PlatformDashboardSubscriptionSummaryDto(
    int TotalSubscriptions,
    int TrialSubscriptions,
    int ActiveSubscriptions,
    int PastDueSubscriptions,
    int CancelledSubscriptions,
    int ExpiredSubscriptions);

public sealed record PlatformDashboardMrrGroupDto(
    string CurrencyCode,
    int DecimalPlaces,
    decimal Amount);

public sealed record PlatformDashboardRevenueSummaryDto(
    IReadOnlyList<PlatformDashboardMrrGroupDto> MrrByCurrency,
    DateTimeOffset CalculatedAt);

public sealed record PlatformDashboardTrendPointDto(
    string Date,
    decimal Value);

public sealed record PlatformDashboardTrendSeriesDto(
    string Metric,
    string? CurrencyCode,
    decimal? ChangePercent,
    string ChangeStatus,
    IReadOnlyList<PlatformDashboardTrendPointDto> Points);

public sealed record PlatformDashboardTrendsDto(
    string Timezone,
    PlatformDashboardTrendSeriesDto TenantGrowth,
    PlatformDashboardTrendSeriesDto? SubscriptionTrend,
    IReadOnlyList<PlatformDashboardTrendSeriesDto> MrrTrends);

public sealed record PlatformDashboardAttentionSummaryDto(
    IReadOnlyList<PlatformDashboardAttentionItemDto> Items,
    int TotalCount);

public sealed record PlatformDashboardFootprintDto(
    int TotalOutlets,
    int TotalTills,
    int TotalTenantUsers,
    int? TotalPlatformUsers);

public sealed record PlatformDashboardHealthDependencyDto(
    string Name,
    string Status,
    bool IsCritical,
    string? Message);

public sealed record PlatformDashboardSystemHealthDto(
    string OverallStatus,
    DateTimeOffset CheckedAt,
    IReadOnlyList<PlatformDashboardHealthDependencyDto> Dependencies);
