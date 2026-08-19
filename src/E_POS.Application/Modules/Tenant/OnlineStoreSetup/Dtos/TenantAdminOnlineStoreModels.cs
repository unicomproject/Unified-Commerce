namespace E_POS.Application.Modules.Tenant.OnlineStoreSetup.Dtos;

public sealed record OnlineStoreSetupStepDto(int StepNumber, string Code, string Label, string Status, IReadOnlyList<string> BlockingReasons);

public sealed record OnlineStoreOverviewResponse(
    Guid SalesChannelId,
    string StoreStatus,
    string ChannelStatus,
    bool SetupEnabled,
    string Visibility,
    string? StoreSlug,
    string? HostedUrl,
    int CompletedSteps,
    int TotalSteps,
    int SetupProgressPercent,
    IReadOnlyList<OnlineStoreSetupStepDto> Steps,
    OnlineStoreReadinessResponse Readiness);

public sealed record OnlineStoreReadinessResponse(
    bool CanPublish,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<OnlineStoreSetupStepDto> Steps);

public sealed record OnlineStoreActivationResponse(
    bool SetupEnabled,
    string StoreStatus,
    string ChannelStatus,
    string Visibility,
    IReadOnlyList<OnlineStoreEntitlementDto> Entitlements);

public sealed record OnlineStoreEntitlementDto(string FeatureCode, string Status);

public sealed record UpdateOnlineStoreActivationRequest(bool SetupEnabled);

public sealed record OnlineStoreIdentityResponse(
    Guid SalesChannelId,
    string StoreName,
    string BusinessDisplayName,
    string? StoreDescription,
    string? StoreEmail,
    string? StorePhone,
    string? SupportTagline,
    string CurrencyCode,
    string Timezone);

public sealed record UpdateOnlineStoreIdentityRequest(
    string StoreName,
    string BusinessDisplayName,
    string? StoreDescription,
    string? StoreEmail,
    string? StorePhone,
    string? SupportTagline);

public sealed record OnlineStoreUrlDomainResponse(string? StoreSlug, string? HostedUrl, IReadOnlyList<OnlineStoreDomainDto> Domains);

public sealed record UpdateOnlineStoreUrlRequest(string StoreSlug);

public sealed record OnlineStoreDomainDto(
    Guid Id,
    string DomainType,
    string DomainName,
    bool IsPrimary,
    string VerificationStatus,
    DateTimeOffset? VerifiedAt,
    string SslStatus,
    DateTimeOffset? SslIssuedAt,
    DateTimeOffset? SslExpiresAt,
    string Status);

public sealed record CreateOnlineStoreDomainRequest(string DomainName, string DomainType, bool IsPrimary);

public sealed record OnlineStoreDomainTokenResponse(Guid DomainId, string DomainName, string VerificationToken);

public sealed record VerifyOnlineStoreDomainRequest(string VerificationToken);

public sealed record OnlineStoreBrandingResponse(
    Guid? LogoMediaAssetId,
    Guid? FaviconMediaAssetId,
    string PrimaryColor,
    string SecondaryColor,
    IReadOnlyList<OnlineStoreBannerDto> Banners);

public sealed record UpdateOnlineStoreBrandingRequest(
    Guid? LogoMediaAssetId,
    Guid? FaviconMediaAssetId,
    string PrimaryColor,
    string SecondaryColor);

public sealed record OnlineStoreMediaResponse(
    Guid MediaAssetId,
    string Purpose,
    string? PublicUrl,
    string FileName,
    string MimeType,
    long FileSizeBytes,
    int? WidthPx,
    int? HeightPx);

public sealed record OnlineStoreBannerDto(
    Guid Id,
    string BannerType,
    string Title,
    string? Subtitle,
    Guid? ImageMediaAssetId,
    string? ImageUrl,
    string? ActionText,
    string? ActionUrl,
    int SortOrder,
    string Status);

public sealed record UpsertOnlineStoreBannerRequest(
    string BannerType,
    string Title,
    string? Subtitle,
    Guid? ImageMediaAssetId,
    string? ActionText,
    string? ActionUrl,
    int SortOrder,
    string Status);

public sealed record UpdateOnlineStoreBannerStatusRequest(string Status);

public sealed record ReorderOnlineStoreBannersRequest(IReadOnlyList<ReorderOnlineStoreBannerItem> Items);

public sealed record ReorderOnlineStoreBannerItem(Guid BannerId, int SortOrder);

public sealed record OnlineStoreSupportResponse(
    string? Email,
    string? Phone,
    string? Whatsapp,
    string? HelpUrl,
    bool ContactUsEnabled,
    string? SupportHours,
    string? BusinessAddress);

public sealed record UpdateOnlineStoreSupportRequest(
    string? Email,
    string? Phone,
    string? Whatsapp,
    string? HelpUrl,
    bool ContactUsEnabled,
    string? SupportHours,
    string? BusinessAddress);

public sealed record OnlineStoreClickCollectResponse(
    bool Enabled,
    int OutletCount,
    IReadOnlyList<OnlineStoreCollectionOutletDto> Outlets);

public sealed record UpdateOnlineStoreClickCollectRequest(bool Enabled);

public sealed record OnlineStoreCollectionOutletDto(
    Guid OutletId,
    string OutletName,
    string OutletStatus,
    bool BusinessHoursConfigured,
    int? PreparationLeadMinutes,
    int? PickupWindowMinutes,
    string? CutoffTime,
    string Status);

public sealed record UpsertCollectionOutletRequest(int? PreparationLeadMinutes, int? PickupWindowMinutes, string? CutoffTime, string Status);

public sealed record BulkApplyCollectionOutletRequest(
    IReadOnlyList<Guid> OutletIds,
    int? PreparationLeadMinutes,
    int? PickupWindowMinutes,
    string? CutoffTime,
    string Status);

public sealed record OnlineStoreCatalogSummaryResponse(
    int TotalProducts,
    int VisibleOnline,
    int NotVisible,
    int Orderable,
    int LowStockProducts,
    int OutOfStockProducts);

public sealed record OnlineStoreCatalogProductListResponse(int PageNumber, int PageSize, int TotalCount, IReadOnlyList<OnlineStoreCatalogProductDto> Items);

public sealed record OnlineStoreCatalogProductDto(
    Guid ProductId,
    Guid? ProductVariantId,
    string ProductName,
    string? VariantName,
    bool IsVisible,
    bool IsOrderable,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableUntil,
    string Status);

public sealed record UpdateProductChannelVisibilityRequest(
    bool IsVisible,
    bool IsOrderable,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableUntil,
    string Status);

public sealed record BulkProductChannelVisibilityRequest(
    IReadOnlyList<Guid> ProductIds,
    bool IsVisible,
    bool IsOrderable,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableUntil,
    string Status);

public sealed record OnlineStorePolicyDto(
    Guid Id,
    string PolicyType,
    string Title,
    string Content,
    string Version,
    string Status,
    DateTimeOffset? PublishedAt);

public sealed record UpsertOnlineStorePolicyRequest(string Title, string Content, string Version);

public sealed record OnlineStorePublishResponse(
    string StoreStatus,
    string ChannelStatus,
    DateTimeOffset PublishedAt,
    OnlineStoreReadinessResponse Readiness);
