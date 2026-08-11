using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface IPosLoginBrandingRepository
{
    Task<PosLoginBrandingTenantSnapshot?> FindActiveTenantBySlugAsync(string tenantSlug, CancellationToken cancellationToken);
    Task<PosLoginBrandingTenantSnapshot?> FindTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> GetSettingValuesAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<PosLoginBrandingMediaSnapshot?> FindMediaAsync(Guid mediaAssetId, CancellationToken cancellationToken);
    Task SaveSettingsAsync(Guid tenantId, IReadOnlyDictionary<string, string?> values, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed record PosLoginBrandingTenantSnapshot(
    Guid TenantId,
    string TenantSlug,
    string DisplayName,
    string? TradingName,
    Guid? LogoMediaAssetId,
    DateTimeOffset UpdatedAt);

public sealed record PosLoginBrandingMediaSnapshot(
    Guid Id,
    Guid TenantId,
    string? PublicUrl,
    string MimeType,
    string FileExtension,
    long FileSizeBytes,
    string AssetType,
    string AssetPurpose,
    string Status,
    DateTimeOffset UpdatedAt);
