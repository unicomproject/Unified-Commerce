using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class PickupSlotReservation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PickupSlotId { get; protected set; }
    public Guid? CheckoutSessionId { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public int ReservedCapacity { get; protected set; }
    public string ReservationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? ConfirmedAt { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public string? ReleaseReason { get; protected set; }

    protected PickupSlotReservation() { }

    public static PickupSlotReservation CreatePending(
        Guid id,
        Guid tenantId,
        Guid pickupSlotId,
        Guid checkoutSessionId,
        int reservedCapacity,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (reservedCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(reservedCapacity));
        return new PickupSlotReservation
        {
            Id = id,
            TenantId = tenantId,
            PickupSlotId = pickupSlotId,
            CheckoutSessionId = checkoutSessionId,
            ReservedCapacity = reservedCapacity,
            ReservationStatus = "PENDING",
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Confirm(Guid salesOrderId, DateTimeOffset now)
    {
        if (ReservationStatus == "CONFIRMED" && SalesOrderId == salesOrderId) return;
        if (ReservationStatus != "PENDING")
            throw new InvalidOperationException("Only a pending pickup slot reservation can be confirmed.");

        SalesOrderId = salesOrderId;
        ReservationStatus = "CONFIRMED";
        ConfirmedAt = now;
        UpdatedAt = now;
    }

    public void Release(string reason, DateTimeOffset now)
    {
        if (ReservationStatus is "RELEASED" or "CANCELLED" or "EXPIRED") return;
        ReservationStatus = "RELEASED";
        ReleasedAt = now;
        ReleaseReason = reason.Trim();
        UpdatedAt = now;
    }
}

