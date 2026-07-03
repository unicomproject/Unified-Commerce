using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Department : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string DepartmentCode { get; protected set; } = string.Empty;

    public static Department Create(Guid id, Guid tenantId, string departmentCode, string name, string status, DateTimeOffset now)
    {
        return new Department
        {
            Id = id,
            TenantId = tenantId,
            DepartmentCode = DepartmentConstants.NormalizeCode(departmentCode),
            Name = name.Trim(),
            Status = DepartmentConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string departmentCode, string name, string status, DateTimeOffset now)
    {
        DepartmentCode = DepartmentConstants.NormalizeCode(departmentCode);
        Name = name.Trim();
        Status = DepartmentConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = DepartmentConstants.DeletedStatus;
        UpdatedAt = now;
    }
}