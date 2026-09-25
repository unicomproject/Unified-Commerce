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
    public int FailedVerificationAttempts { get; protected set; }

    public const int MaxVerificationAttempts = 3;

    // The pickup code is shown to, and must be re-displayable to, the order's own
    // customer for as long as it is valid — unlike a password, a peer reading it off
    // the customer's own screen is the intended, legitimate use. Storing it in
    // recoverable form (rather than one-way hashed) is a deliberate choice: what makes
    // it safe is that it is a large random value, single-use, and time-limited — not
    // secrecy from its rightful holder. Comparisons still use fixed-time equality.
    public void IssuePickupCode(string pickupCode, int version, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(pickupCode))
            throw new ArgumentException("Pickup code is required.", nameof(pickupCode));
        if (PickupStatus != "READY")
            throw new InvalidOperationException("PICKUP_NOT_READY_FOR_CODE");

        PickupQrTokenHash = pickupCode;
        PickupQrVersion = version;
        PickupQrExpiresAt = expiresAt;
        FailedVerificationAttempts = 0;
        UpdatedAt = now;
    }

    public void RecordFailedVerification(DateTimeOffset now)
    {
        if (PickupStatus != "READY")
            throw new InvalidOperationException("PICKUP_NOT_VERIFIABLE");

        FailedVerificationAttempts++;
        UpdatedAt = now;
    }

    public bool IsLockedOut => FailedVerificationAttempts >= MaxVerificationAttempts;

    public void Verify(Guid tenantUserId, string verificationMethod, DateTimeOffset now)
    {
        if (PickupStatus != "READY")
            throw new InvalidOperationException("PICKUP_NOT_VERIFIABLE");
        if (IsLockedOut)
            throw new InvalidOperationException("PICKUP_VERIFICATION_LOCKED");
        if (string.IsNullOrWhiteSpace(verificationMethod))
            throw new ArgumentException("Verification method is required.", nameof(verificationMethod));

        PickupStatus = "VERIFIED";
        VerificationMethod = verificationMethod.Trim();
        VerifiedByTenantUserId = tenantUserId;
        VerifiedAt = now;
        // Single-use: burn the code immediately so it can never be replayed.
        PickupQrTokenHash = null;
        UpdatedAt = now;
    }

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

    /// <summary>
    /// Issues (or re-issues) a collection QR for a READY pickup. The QR never expires on its
    /// own — it stays valid until the order is actually collected (or the pickup is cancelled),
    /// regardless of how long that takes. <paramref name="expiresAt"/> is accepted only for
    /// backward compatibility with historical rows; pass null for the current no-expiry policy.
    /// </summary>
    public void IssueCollectionQr(
        string tokenHash,
        int version,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
    {
        if (PickupStatus != "READY")
            throw new InvalidOperationException("PICKUP_NOT_READY_FOR_QR");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Collection QR token hash is required.", nameof(tokenHash));

        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Collection QR version must be positive.");

        if (expiresAt is { } value && value <= now)
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Collection QR expiry must be in the future.");

        PickupQrTokenHash = tokenHash.Trim();
        PickupQrVersion = version;
        PickupQrExpiresAt = expiresAt;
        UpdatedAt = now;
    }

    public void MarkCollected(DateTimeOffset now)
    {
        if (PickupStatus == "COLLECTED" && CollectedAt.HasValue)
            return;

        if (PickupStatus is "CANCELLED" or "EXPIRED" or "COMPLETED")
            throw new InvalidOperationException("PICKUP_NOT_COLLECTABLE");

        if (PickupStatus is not ("READY" or "VERIFIED"))
            throw new InvalidOperationException("PICKUP_NOT_COLLECTABLE");

        PickupStatus = "COLLECTED";
        CollectedAt = now;
        UpdatedAt = now;
    }
}

