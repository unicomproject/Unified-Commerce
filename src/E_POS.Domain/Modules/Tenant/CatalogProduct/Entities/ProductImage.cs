using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductImage : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public Guid? MediaAssetId { get; protected set; }
    public string ImageStorageKey { get; protected set; } = string.Empty;
    public string? ImageUrl { get; protected set; }
    public string? AltText { get; protected set; }
    public string ImagePurpose { get; protected set; } = string.Empty;
    public string? MimeType { get; protected set; }
    public long? FileSizeBytes { get; protected set; }
    public int? WidthPx { get; protected set; }
    public int? HeightPx { get; protected set; }
    public string? ChecksumHash { get; protected set; }
    public int SortOrder { get; protected set; }
    public bool IsPrimaryImage { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductImage Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        Guid? salesChannelId,
        string imageStorageKey,
        string? imageUrl,
        string? altText,
        string imagePurpose,
        string? mimeType,
        long? fileSizeBytes,
        int? widthPx,
        int? heightPx,
        string? checksumHash,
        int sortOrder,
        bool isPrimaryImage,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        Guid? mediaAssetId = null)
    {
        return new ProductImage
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            SalesChannelId = salesChannelId,
            MediaAssetId = mediaAssetId,
            ImageStorageKey = imageStorageKey.Trim(),
            ImageUrl = imageUrl?.Trim(),
            AltText = altText?.Trim(),
            ImagePurpose = imagePurpose.Trim().ToUpperInvariant(),
            MimeType = mimeType?.Trim(),
            FileSizeBytes = fileSizeBytes,
            WidthPx = widthPx,
            HeightPx = heightPx,
            ChecksumHash = checksumHash?.Trim(),
            SortOrder = sortOrder,
            IsPrimaryImage = isPrimaryImage,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

