using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class OutletBusinessHour : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public int DayOfWeek { get; protected set; }
    public TimeOnly OpenTime { get; protected set; }
    public TimeOnly CloseTime { get; protected set; }

    public static OutletBusinessHour Create(Guid id, Guid outletId, int dayOfWeek, TimeOnly openTime, TimeOnly closeTime, DateTimeOffset now)
    {
        return new OutletBusinessHour
        {
            Id = id,
            OutletId = outletId,
            DayOfWeek = dayOfWeek,
            OpenTime = openTime,
            CloseTime = closeTime,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
