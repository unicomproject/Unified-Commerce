using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Collection : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string CollectionCode { get; protected set; } = string.Empty;
    public string CollectionName { get; protected set; } = string.Empty;
    public string CollectionSlug { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string CollectionType { get; protected set; } = string.Empty;
    public DateTimeOffset? StartsAt { get; protected set; }
    public DateTimeOffset? EndsAt { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Collection Create(
        Guid id, 
        Guid tenantId, 
        string collectionCode, 
        string collectionName, 
        string collectionSlug,
        string? description,
        string collectionType,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        int sortOrder,
        string status, 
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new Collection
        {
            Id = id,
            TenantId = tenantId,
            CollectionCode = collectionCode.Trim().ToUpperInvariant(),
            CollectionName = collectionName.Trim(),
            CollectionSlug = collectionSlug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            CollectionType = collectionType.Trim().ToUpperInvariant(),
            StartsAt = startsAt,
            EndsAt = endsAt,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string collectionCode, 
        string collectionName, 
        string collectionSlug,
        string? description,
        string collectionType,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        int sortOrder,
        string status, 
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        CollectionCode = collectionCode.Trim().ToUpperInvariant();
        CollectionName = collectionName.Trim();
        CollectionSlug = collectionSlug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        CollectionType = collectionType.Trim().ToUpperInvariant();
        StartsAt = startsAt;
        EndsAt = endsAt;
        SortOrder = sortOrder;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}