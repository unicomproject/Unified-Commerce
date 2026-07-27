namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string Status,
    /// <summary>
    /// Temporary compatibility field. Historically sourced from subscription status for filters.
    /// Not the canonical tenant lifecycle; prefer <see cref="LifecycleStatus"/>.
    /// </summary>
    string BillingStatus,
    string OperatingMode,
    string BaseCurrency,
    string DefaultTimezone,
    string DefaultLocale,
    string? BusinessType,
    PlatformTenantProfileDetailDto? Profile,
    PlatformTenantAddressDetailDto? PrimaryAddress,
    PlatformTenantDetailSubscriptionDto? Subscription,
    int UserCount,
    int OutletCount,
    int TillCount,
    bool OnlineStoreEnabled,
    bool ClickCollectEnabled,
    bool OfflineEnabled,
    IReadOnlyList<Guid> EnabledFeatureIds,
    IReadOnlyList<string> EnabledFeatureCodes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? LastActivityAt,
    bool CanUpdate,
    bool CanActivate,
    bool CanSuspend,
    bool CanManageEntitlements,
    /// <summary>
    /// Authoritative tenant lifecycle status from <c>tenants.status</c>.
    /// </summary>
    string LifecycleStatus = "");

