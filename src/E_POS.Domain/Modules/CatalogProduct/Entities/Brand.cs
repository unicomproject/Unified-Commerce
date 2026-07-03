using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Brand : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string BrandCode { get; protected set; } = string.Empty;

    public static Brand Create(Guid id, Guid tenantId, string brandCode, string name, string status, DateTimeOffset now)
    {
        return new Brand
        {
            Id = id,
            TenantId = tenantId,
            BrandCode = BrandConstants.NormalizeCode(brandCode),
            Name = name.Trim(),
            Status = BrandConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string brandCode, string name, string status, DateTimeOffset now)
    {
        BrandCode = BrandConstants.NormalizeCode(brandCode);
        Name = name.Trim();
        Status = BrandConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = BrandConstants.DeletedStatus;
        UpdatedAt = now;
    }
}