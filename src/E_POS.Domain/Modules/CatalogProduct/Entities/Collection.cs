using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Collection : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string CollectionCode { get; protected set; } = string.Empty;

    public static Collection Create(Guid id, Guid tenantId, string collectionCode, string name, string status, DateTimeOffset now)
    {
        return new Collection
        {
            Id = id,
            TenantId = tenantId,
            CollectionCode = CollectionConstants.NormalizeCode(collectionCode),
            Name = name.Trim(),
            Status = CollectionConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string collectionCode, string name, string status, DateTimeOffset now)
    {
        CollectionCode = CollectionConstants.NormalizeCode(collectionCode);
        Name = name.Trim();
        Status = CollectionConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = CollectionConstants.DeletedStatus;
        UpdatedAt = now;
    }
}