using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.HardwareCash.Entities;

public class HardwareDevice : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string HardwareDeviceCode { get; protected set; } = string.Empty;
    public string SerialNumber { get; protected set; } = string.Empty;
}
