namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantListItemDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    /// <summary>
    /// Temporary compatibility field (subscription status for list filters). Prefer <see cref="LifecycleStatus"/>.
    /// </summary>
    string BillingStatus,
    string OperatingMode,
    string BaseCurrency,
    string DefaultTimezone,
    string DefaultLocale,
    string? BusinessType,
    PlatformTenantSubscriptionSummaryDto? Subscription,
    int OutletCount,
    int TillCount,
    int UserCount,
    bool OnlineStoreEnabled,
    bool ClickCollectEnabled,
    bool OfflineEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    /// <summary>
    /// Authoritative tenant lifecycle status from <c>tenants.status</c>.
    /// </summary>
    string LifecycleStatus = "");

