using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class HardwareProfile : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ProfileName { get; protected set; } = string.Empty;
    public string ProfileType { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string? ConfigurationJson { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
