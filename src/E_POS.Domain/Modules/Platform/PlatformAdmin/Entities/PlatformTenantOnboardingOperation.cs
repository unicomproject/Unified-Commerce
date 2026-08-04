using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public sealed class PlatformTenantOnboardingOperation : AuditableEntity
{
    public Guid DraftId { get; private set; }
    public Guid TenantId { get; private set; }
    public string OperationType { get; private set; } = "FINALIZATION";
    public string Status { get; private set; } = "SUCCEEDED";
    public string ProvisioningStatus { get; private set; } = "SUCCEEDED";
    public string PaymentStatus { get; private set; } = "NOT_REQUIRED";
    public string InvitationStatus { get; private set; } = "NOT_ELIGIBLE";
    public string IdempotencyKeyHash { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? SanitizedFailureDetails { get; private set; }
    public string? ResultReference { get; private set; }
    public long Version { get; private set; } = 1;

    public static PlatformTenantOnboardingOperation CreateCompleted(
        Guid id, Guid draftId, Guid tenantId, string keyHash, string requestHash,
        string paymentStatus, string invitationStatus, DateTimeOffset now)
    {
        return new PlatformTenantOnboardingOperation
        {
            Id = id,
            DraftId = draftId,
            TenantId = tenantId,
            IdempotencyKeyHash = keyHash,
            RequestHash = requestHash,
            PaymentStatus = paymentStatus,
            InvitationStatus = invitationStatus,
            ResultReference = tenantId.ToString("D"),
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkRetryable(string failureCode, string? safeDetails, DateTimeOffset nextRetryAt, DateTimeOffset now)
    {
        Status = "FAILED_RETRYABLE";
        FailureCode = failureCode;
        SanitizedFailureDetails = safeDetails;
        NextRetryAt = nextRetryAt;
        AttemptCount++;
        UpdatedAt = now;
        Version++;
    }

    public void MarkInvitationSent(DateTimeOffset now)
    {
        InvitationStatus = "SENT";
        UpdatedAt = now;
        Version++;
    }
}
