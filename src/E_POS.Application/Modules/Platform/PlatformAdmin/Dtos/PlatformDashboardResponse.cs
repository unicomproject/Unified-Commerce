namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformDashboardResponse(
    int TotalTenants,
    int ActiveTenants,
    int SuspendedTenants,
    int TrialTenants,
    int TotalSubscriptions,
    int ActiveSubscriptions,
    int PendingBillingCount,
    int TotalOutlets,
    int TotalTills,
    int TotalUsers,
    IReadOnlyList<PlatformDashboardRecentTenantDto> RecentTenants,
    IReadOnlyList<PlatformDashboardAttentionItemDto> AttentionItems,
    DateTimeOffset GeneratedAt);

