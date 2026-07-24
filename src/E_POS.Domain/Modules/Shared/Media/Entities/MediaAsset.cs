using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Media.Entities;

public sealed class MediaAsset : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string ContainerName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string? PublicUrl { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public string FileExtension { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public int? WidthPx { get; private set; }
    public int? HeightPx { get; private set; }
    public string ChecksumHash { get; private set; } = string.Empty;
    public string AssetType { get; private set; } = string.Empty;
    public string AssetPurpose { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; private set; }
    public Guid? UpdatedByTenantUserId { get; private set; }

    public static MediaAsset Create(
        Guid id,
        Guid tenantId,
        string containerName,
        string storageKey,
        string? publicUrl,
        string originalFileName,
        string mimeType,
        string fileExtension,
        long fileSizeBytes,
        int? widthPx,
        int? heightPx,
        string checksumHash,
        string assetType,
        string assetPurpose,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new MediaAsset
        {
            Id = id,
            TenantId = tenantId,
            ContainerName = containerName.Trim(),
            StorageKey = storageKey.Trim(),
            PublicUrl = publicUrl?.Trim(),
            OriginalFileName = originalFileName.Trim(),
            MimeType = mimeType.Trim().ToLowerInvariant(),
            FileExtension = fileExtension.Trim().ToLowerInvariant(),
            FileSizeBytes = fileSizeBytes,
            WidthPx = widthPx,
            HeightPx = heightPx,
            ChecksumHash = checksumHash.Trim().ToLowerInvariant(),
            AssetType = assetType.Trim().ToUpperInvariant(),
            AssetPurpose = assetPurpose.Trim().ToUpperInvariant(),
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkInactive(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        if (Status == "DELETED")
        {
            return;
        }

        Status = "INACTIVE";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
