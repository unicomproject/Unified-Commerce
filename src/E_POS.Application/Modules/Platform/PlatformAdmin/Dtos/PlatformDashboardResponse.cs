namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

/// <summary>
/// Sectioned Platform Dashboard response. Legacy flat fields remain for compatibility;
/// permission-restricted values are omitted (null), never presented as authentic zeros.
/// </summary>
public sealed record PlatformDashboardResponse(
    DateTimeOffset GeneratedAt,
    PlatformDashboardSectionDto<PlatformDashboardTenantSummaryDto> TenantSummary,
    PlatformDashboardSectionDto<PlatformDashboardSubscriptionSummaryDto>? SubscriptionSummary,
    PlatformDashboardSectionDto<PlatformDashboardRevenueSummaryDto>? RevenueSummary,
    PlatformDashboardSectionDto<PlatformDashboardTrendsDto>? Trends,
    PlatformDashboardSectionDto<PlatformDashboardAttentionSummaryDto> AttentionSummary,
    PlatformDashboardSectionDto<PlatformDashboardFootprintDto> PlatformFootprint,
    PlatformDashboardSectionDto<PlatformDashboardSystemHealthDto> SystemHealth,
    PlatformDashboardSectionDto<IReadOnlyList<PlatformDashboardRecentTenantDto>> RecentTenants,
    // Legacy flat fields (nullable when omitted by permission)
    int? TotalTenants,
    int? ActiveTenants,
    int? SuspendedTenants,
    int? InactiveTenants,
    int? SetupPendingTenants,
    int? TrialTenants,
    int? TotalSubscriptions,
    int? ActiveSubscriptions,
    int? PastDueSubscriptions,
    int? CancelledSubscriptions,
    int? ExpiredSubscriptions,
    int? PendingBillingCount,
    int? TotalOutlets,
    int? TotalTills,
    int? TotalUsers,
    int? TotalTenantUsers,
    int? TotalPlatformUsers,
    IReadOnlyList<PlatformDashboardRecentTenantDto>? RecentTenantsList,
    IReadOnlyList<PlatformDashboardAttentionItemDto>? AttentionItems);
