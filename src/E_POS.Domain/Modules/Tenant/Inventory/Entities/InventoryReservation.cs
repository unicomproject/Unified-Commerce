using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryReservation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReservationNumber { get; protected set; } = string.Empty;
    public string ReservationSource { get; protected set; } = string.Empty;
    public Guid? SourceReferenceId { get; protected set; }
    public string? SourceReferenceNumber { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public Guid? FulfillmentOutletId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string ReservationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset ReservedAt { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public string? ReleaseReason { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected InventoryReservation() { }

    public static InventoryReservation Create(
        Guid id,
        Guid tenantId,
        string reservationNumber,
        string reservationSource,
        Guid? sourceReferenceId,
        string? sourceReferenceNumber,
        Guid? salesChannelId,
        Guid? fulfillmentOutletId,
        Guid? customerId,
        string reservationStatus,
        DateTimeOffset reservedAt,
        DateTimeOffset? expiresAt,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new InventoryReservation
        {
            Id = id,
            TenantId = tenantId,
            ReservationNumber = reservationNumber.Trim(),
            ReservationSource = reservationSource.Trim(),
            SourceReferenceId = sourceReferenceId,
            SourceReferenceNumber = sourceReferenceNumber?.Trim(),
            SalesChannelId = salesChannelId,
            FulfillmentOutletId = fulfillmentOutletId,
            CustomerId = customerId,
            ReservationStatus = reservationStatus.Trim(),
            ReservedAt = reservedAt,
            ExpiresAt = expiresAt,
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Release(string releaseReason, DateTimeOffset releasedAt, Guid? updatedByTenantUserId)
    {
        ReleasedAt = releasedAt;
        ReleaseReason = releaseReason.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = releasedAt;
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        ReservationStatus = status.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    // A CHECKOUT-sourced reservation is created before the SalesOrder exists, so it
    // initially points at the checkout session. Once checkout confirmation creates the
    // order, the reservation must be re-pointed here or downstream consumers that key off
    // SourceReferenceId == salesOrderId (e.g. POS "start fulfillment") can never find it.
    // ExpiresAt is also cleared: it only guards an in-flight checkout hold, and a
    // reservation permanently attached to a confirmed order must never lapse.
    public void AttachOrder(Guid salesOrderId, string? salesOrderNumber, DateTimeOffset now)
    {
        SourceReferenceId = salesOrderId;
        SourceReferenceNumber = string.IsNullOrWhiteSpace(salesOrderNumber)
            ? SourceReferenceNumber
            : salesOrderNumber.Trim();
        ExpiresAt = null;
        UpdatedAt = now;
    }
}
