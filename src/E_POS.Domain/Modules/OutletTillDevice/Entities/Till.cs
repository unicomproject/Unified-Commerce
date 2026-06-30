using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class Till : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string TillCode { get; protected set; } = string.Empty;
}
