using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class OutletBusinessHour : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public int DayOfWeek { get; protected set; }
    public TimeOnly OpenTime { get; protected set; }
    public TimeOnly CloseTime { get; protected set; }
}
