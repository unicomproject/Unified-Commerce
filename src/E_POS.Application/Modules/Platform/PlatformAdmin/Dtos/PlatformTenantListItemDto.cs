namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantListItemDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
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
    DateTimeOffset? UpdatedAt);

