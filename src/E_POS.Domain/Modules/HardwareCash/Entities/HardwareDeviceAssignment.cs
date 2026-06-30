using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.HardwareCash.Entities;

public class HardwareDeviceAssignment : AuditableEntity
{
    public Guid? PosDeviceId { get; protected set; }
    public string EffectiveFrom { get; protected set; } = string.Empty;
    public Guid HardwareDeviceId { get; protected set; }
}
