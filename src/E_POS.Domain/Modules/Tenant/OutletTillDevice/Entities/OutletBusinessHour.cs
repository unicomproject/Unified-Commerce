using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class OutletBusinessHour : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public short DayOfWeek { get; protected set; }
    public TimeOnly? OpeningTime { get; protected set; }
    public TimeOnly? ClosingTime { get; protected set; }
    public bool IsClosed { get; protected set; }
    public DateOnly? ValidFrom { get; protected set; }
    public DateOnly? ValidUntil { get; protected set; }

    public static OutletBusinessHour Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        short dayOfWeek,
        TimeOnly? openingTime,
        TimeOnly? closingTime,
        bool isClosed,
        DateOnly? validFrom,
        DateOnly? validUntil,
        DateTimeOffset now)
    {
        return new OutletBusinessHour
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            DayOfWeek = dayOfWeek,
            OpeningTime = isClosed ? null : openingTime,
            ClosingTime = isClosed ? null : closingTime,
            IsClosed = isClosed,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
