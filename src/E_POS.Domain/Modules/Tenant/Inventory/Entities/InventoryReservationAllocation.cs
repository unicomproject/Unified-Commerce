using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryReservationAllocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryReservationLineId { get; protected set; }
    public Guid InventoryBalanceId { get; protected set; }
    public Guid? SerialNumberId { get; protected set; }
    public decimal AllocatedQuantity { get; protected set; }
    public decimal ReleasedQuantity { get; protected set; }
    public decimal FulfilledQuantity { get; protected set; }
    public string AllocationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset AllocatedAt { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }

    protected InventoryReservationAllocation() { }

    public static InventoryReservationAllocation Create(
        Guid id,
        Guid tenantId,
        Guid inventoryReservationLineId,
        Guid inventoryBalanceId,
        Guid? serialNumberId,
        decimal allocatedQuantity,
        string allocationStatus,
        DateTimeOffset allocatedAt,
        DateTimeOffset now)
    {
        return new InventoryReservationAllocation
        {
            Id = id,
            TenantId = tenantId,
            InventoryReservationLineId = inventoryReservationLineId,
            InventoryBalanceId = inventoryBalanceId,
            SerialNumberId = serialNumberId,
            AllocatedQuantity = allocatedQuantity,
            ReleasedQuantity = 0,
            FulfilledQuantity = 0,
            AllocationStatus = allocationStatus.Trim(),
            AllocatedAt = allocatedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateQuantities(
        decimal releasedDelta,
        decimal fulfilledDelta,
        DateTimeOffset now)
    {
        ReleasedQuantity += releasedDelta;
        FulfilledQuantity += fulfilledDelta;
        UpdatedAt = now;
    }

    public void Release(DateTimeOffset releasedAt, DateTimeOffset now)
    {
        ReleasedAt = releasedAt;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, DateTimeOffset now)
    {
        AllocationStatus = status.Trim();
        UpdatedAt = now;
    }
}
