using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class BusinessType : AuditableEntity
{
    public string BusinessCode { get; protected set; } = string.Empty;
    public string BusinessName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static BusinessType Create(
        Guid id,
        string businessCode,
        string businessName,
        string? description,
        string status,
        DateTimeOffset now)
    {
        return new BusinessType
        {
            Id = id,
            BusinessCode = businessCode.Trim(),
            BusinessName = businessName.Trim(),
            Description = description?.Trim(),
            Status = status.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
