using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class PickupSlot : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid FulfillmentMethodOutletId { get; protected set; }
    public string SlotCode { get; protected set; } = string.Empty;
    public DateOnly SlotDate { get; protected set; }
    public TimeOnly WindowStart { get; protected set; }
    public TimeOnly WindowEnd { get; protected set; }
    public int Capacity { get; protected set; }
    public int ReservedCount { get; protected set; }
    public string SlotStatus { get; protected set; } = string.Empty;
    public long RowVersion { get; protected set; }

    protected PickupSlot() { }

    public static PickupSlot CreateOpen(
        Guid id,
        Guid tenantId,
        Guid fulfillmentMethodOutletId,
        string slotCode,
        DateOnly slotDate,
        TimeOnly windowStart,
        TimeOnly windowEnd,
        int capacity,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(slotCode))
            throw new ArgumentException("Slot code is required.", nameof(slotCode));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        if (windowEnd <= windowStart)
            throw new ArgumentOutOfRangeException(nameof(windowEnd), "Window end must be after window start.");

        return new PickupSlot
        {
            Id = id,
            TenantId = tenantId,
            FulfillmentMethodOutletId = fulfillmentMethodOutletId,
            SlotCode = slotCode.Trim(),
            SlotDate = slotDate,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            Capacity = capacity,
            ReservedCount = 0,
            SlotStatus = "OPEN",
            RowVersion = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Reserve(int capacity, DateTimeOffset now)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (SlotStatus != "OPEN" || ReservedCount + capacity > Capacity)
            throw new InvalidOperationException("Pickup slot is not available.");

        ReservedCount += capacity;
        RowVersion++;
        if (ReservedCount == Capacity) SlotStatus = "FULL";
        UpdatedAt = now;
    }

    public void Release(int capacity, DateTimeOffset now)
    {
        if (capacity <= 0 || capacity > ReservedCount)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        ReservedCount -= capacity;
        RowVersion++;
        if (SlotStatus == "FULL" && ReservedCount < Capacity) SlotStatus = "OPEN";
        UpdatedAt = now;
    }
}

