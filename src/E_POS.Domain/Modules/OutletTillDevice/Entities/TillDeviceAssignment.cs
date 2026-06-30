using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class TillDeviceAssignment : AuditableEntity
{
    public Guid? TillId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public string EffectiveFrom { get; protected set; } = string.Empty;
}
