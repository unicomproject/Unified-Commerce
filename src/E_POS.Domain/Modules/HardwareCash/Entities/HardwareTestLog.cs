using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.HardwareCash.Entities;

public class HardwareTestLog : AuditableEntity
{
    public Guid HardwareDeviceId { get; protected set; }
    public string TestResult { get; protected set; } = string.Empty;
}
