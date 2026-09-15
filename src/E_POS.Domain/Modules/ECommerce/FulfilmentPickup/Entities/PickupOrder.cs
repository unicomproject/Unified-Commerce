using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class PickupOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid FulfillmentOrderId { get; protected set; }
    public Guid? PickupSlotReservationId { get; protected set; }
    public string PickupNumber { get; protected set; } = string.Empty;
    public string PickupContactName { get; protected set; } = string.Empty;
    public string? PickupContactPhone { get; protected set; }
    public string? PickupContactEmail { get; protected set; }
    public string? PickupContactChannel { get; protected set; }
    public string PickupStatus { get; protected set; } = string.Empty;
    public string? PickupNote { get; protected set; }
    public string? PickupQrTokenHash { get; protected set; }
    public int? PickupQrVersion { get; protected set; }
    public DateTimeOffset? PickupQrExpiresAt { get; protected set; }
    public string? VerificationMethod { get; protected set; }
    public Guid? VerifiedByTenantUserId { get; protected set; }
    public DateTimeOffset? VerifiedAt { get; protected set; }
    public DateTimeOffset? CollectedAt { get; protected set; }

    protected PickupOrder() { }

    public static PickupOrder Create(
        Guid id,
        Guid tenantId,
        Guid fulfillmentOrderId,
        Guid? pickupSlotReservationId,
        string pickupNumber,
        string pickupContactName,
        string? pickupContactPhone,
        string? pickupContactEmail,
        string? pickupContactChannel,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(pickupNumber))
            throw new ArgumentException("Pickup number is required.", nameof(pickupNumber));
        if (string.IsNullOrWhiteSpace(pickupContactName))
            throw new ArgumentException("Pickup contact name is required.", nameof(pickupContactName));

        return new PickupOrder
        {
            Id = id,
            TenantId = tenantId,
            FulfillmentOrderId = fulfillmentOrderId,
            PickupSlotReservationId = pickupSlotReservationId,
            PickupNumber = pickupNumber.Trim(),
            PickupContactName = pickupContactName.Trim(),
            PickupContactPhone = string.IsNullOrWhiteSpace(pickupContactPhone) ? null : pickupContactPhone.Trim(),
            PickupContactEmail = string.IsNullOrWhiteSpace(pickupContactEmail) ? null : pickupContactEmail.Trim(),
            PickupContactChannel = string.IsNullOrWhiteSpace(pickupContactChannel) ? null : pickupContactChannel.Trim(),
            PickupStatus = "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkReady(DateTimeOffset now)
    {
        if (PickupStatus is "COLLECTED" or "CANCELLED" or "COMPLETED")
            throw new InvalidOperationException("PICKUP_NOT_READYABLE");

        if (PickupStatus == "READY")
            throw new InvalidOperationException("PICKUP_ALREADY_READY");

        PickupStatus = "READY";
        UpdatedAt = now;
    }
}

