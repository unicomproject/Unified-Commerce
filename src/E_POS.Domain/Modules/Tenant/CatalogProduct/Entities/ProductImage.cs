using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductImage : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public Guid? MediaAssetId { get; protected set; }
    public string? AltText { get; protected set; }
    public string ImagePurpose { get; protected set; } = string.Empty;
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
        Guid? mediaAssetId,
        string? altText,
        string imagePurpose,
        int sortOrder,
        bool isPrimaryImage,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductImage
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            SalesChannelId = salesChannelId,
            MediaAssetId = mediaAssetId,
            AltText = altText?.Trim(),
            ImagePurpose = imagePurpose.Trim().ToUpperInvariant(),
            SortOrder = sortOrder,
            IsPrimaryImage = isPrimaryImage,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetPrimary(bool isPrimary, Guid? userId, DateTimeOffset now)
    {
        IsPrimaryImage = isPrimary;
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
    }

    public void SetSortOrder(int sortOrder, Guid? userId, DateTimeOffset now)
    {
        SortOrder = Math.Max(0, sortOrder);
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? userId, DateTimeOffset now)
    {
        Status = "DELETED";
        IsPrimaryImage = false;
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
    }

    public void Reassign(Guid mediaAssetId, Guid? userId, DateTimeOffset now)
    {
        MediaAssetId = mediaAssetId;
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
    }

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
        return Create(
            id,
            tenantId,
            productId,
            productVariantId,
            salesChannelId,
            mediaAssetId,
            altText,
            imagePurpose,
            sortOrder,
            isPrimaryImage,
            status,
            createdByTenantUserId,
            now);
    }
}