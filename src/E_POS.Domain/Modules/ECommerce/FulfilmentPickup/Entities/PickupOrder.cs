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
}

