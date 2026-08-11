namespace E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

public sealed record PublicPosLoginBrandingResponse(
    string TenantSlug,
    string BrandDisplayName,
    string SystemName,
    string Description,
    string LoginSubtitle,
    string BackgroundMode,
    string BackgroundColor,
    string? LogoUrl,
    string? BackgroundImageUrl,
    string? HeroImageUrl,
    DateTimeOffset UpdatedAt);

public sealed record PosLoginBrandingConfiguredDto(
    string? SystemName,
    string? Description,
    string? SubtitleTemplate,
    string? BackgroundMode,
    string? BackgroundColor,
    Guid? BackgroundMediaAssetId,
    Guid? HeroMediaAssetId);

public sealed record TenantAdminPosLoginBrandingResponse(
    PosLoginBrandingConfiguredDto Configured,
    PublicPosLoginBrandingResponse Effective);

public sealed record UpdatePosLoginBrandingRequest(
    string? SystemName,
    string? Description,
    string? SubtitleTemplate,
    string? BackgroundMode,
    string? BackgroundColor,
    Guid? BackgroundMediaAssetId,
    Guid? HeroMediaAssetId);

public sealed record PosLoginBrandingMediaUploadResponse(
    Guid MediaAssetId,
    string Purpose,
    string? PublicUrl,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    int WidthPx,
    int HeightPx);
