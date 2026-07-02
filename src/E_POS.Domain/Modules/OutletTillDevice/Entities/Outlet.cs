using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class Outlet : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string OutletCode { get; protected set; } = string.Empty;

    public static Outlet Create(
        Guid id,
        Guid tenantId,
        string outletCode,
        string name,
        string status,
        DateTimeOffset createdAt)
    {
        return new Outlet
        {
            Id = id,
            TenantId = tenantId,
            OutletCode = outletCode,
            Name = name,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
