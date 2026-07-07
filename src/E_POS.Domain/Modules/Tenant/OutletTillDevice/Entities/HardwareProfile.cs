using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class HardwareProfile : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string ProfileCode { get; protected set; } = string.Empty;
}

