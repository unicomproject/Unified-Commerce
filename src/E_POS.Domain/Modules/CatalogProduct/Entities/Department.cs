using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Department : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string DepartmentCode { get; protected set; } = string.Empty;
    public string DepartmentName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Department Create(
        Guid id, 
        Guid tenantId, 
        string departmentCode, 
        string departmentName, 
        string? description,
        int sortOrder,
        string status, 
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new Department
        {
            Id = id,
            TenantId = tenantId,
            DepartmentCode = departmentCode.Trim().ToUpperInvariant(),
            DepartmentName = departmentName.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string departmentCode, 
        string departmentName, 
        string? description,
        int sortOrder,
        string status, 
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        DepartmentCode = departmentCode.Trim().ToUpperInvariant();
        DepartmentName = departmentName.Trim();
        Description = description?.Trim();
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