using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public sealed class PlatformTenantOnboardingDraft : AuditableEntity
{
    public Guid OwnerPlatformUserId { get; private set; }
    public string Status { get; private set; } = "in_progress";
    public short CurrentStep { get; private set; } = 1;
    public short CompletedStepsMask { get; private set; }
    public short ProgressPercent { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public int SchemaVersion { get; private set; } = 1;
    public string? TenantCodeNormalized { get; private set; }
    public string? TenantSlugNormalized { get; private set; }
    public string? RequestedDomainNormalized { get; private set; }
    public string? AdminEmailNormalized { get; private set; }
    public long Version { get; private set; } = 1;
    public string? FinalizeIdempotencyKeyHash { get; private set; }
    public string? FinalizeRequestHash { get; private set; }
    public Guid? CreatedTenantId { get; private set; }
    public string? LastErrorCode { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? DiscardedAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid CreatedByPlatformUserId { get; private set; }
    public Guid UpdatedByPlatformUserId { get; private set; }

    public static PlatformTenantOnboardingDraft Create(
        Guid id,
        Guid ownerPlatformUserId,
        string payloadJson,
        short currentStep,
        short completedStepsMask,
        short progressPercent,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        return new PlatformTenantOnboardingDraft
        {
            Id = id,
            OwnerPlatformUserId = ownerPlatformUserId,
            CreatedByPlatformUserId = ownerPlatformUserId,
            UpdatedByPlatformUserId = ownerPlatformUserId,
            PayloadJson = payloadJson,
            CurrentStep = currentStep,
            CompletedStepsMask = completedStepsMask,
            ProgressPercent = progressPercent,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = expiresAt
        };
    }

    public void Update(
        string payloadJson,
        short currentStep,
        short completedStepsMask,
        short progressPercent,
        string? tenantCode,
        string? tenantSlug,
        string? requestedDomain,
        string? adminEmail,
        Guid actorId,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        EnsureEditable();
        PayloadJson = payloadJson;
        CurrentStep = currentStep;
        CompletedStepsMask = completedStepsMask;
        ProgressPercent = progressPercent;
        TenantCodeNormalized = Normalize(tenantCode, upper: true);
        TenantSlugNormalized = Normalize(tenantSlug);
        RequestedDomainNormalized = Normalize(requestedDomain);
        AdminEmailNormalized = Normalize(adminEmail, upper: true);
        UpdatedByPlatformUserId = actorId;
        UpdatedAt = now;
        ExpiresAt = expiresAt;
        LastErrorCode = null;
        Version++;
    }

    public void BeginFinalization(string keyHash, string requestHash, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        Status = "finalizing";
        FinalizeIdempotencyKeyHash = keyHash;
        FinalizeRequestHash = requestHash;
        UpdatedByPlatformUserId = actorId;
        UpdatedAt = now;
        Version++;
    }

    public void Complete(Guid tenantId, Guid actorId, DateTimeOffset now)
    {
        Status = "completed";
        CreatedTenantId = tenantId;
        FinalizedAt = now;
        ProgressPercent = 100;
        CompletedStepsMask = 127;
        UpdatedByPlatformUserId = actorId;
        UpdatedAt = now;
        Version++;
    }

    public void RestoreAfterFailedFinalization(string safeErrorCode, Guid actorId, DateTimeOffset now)
    {
        Status = "in_progress";
        LastErrorCode = safeErrorCode;
        UpdatedByPlatformUserId = actorId;
        UpdatedAt = now;
        Version++;
    }

    public void Discard(Guid actorId, DateTimeOffset now)
    {
        if (Status == "discarded") return;
        EnsureEditable();
        Status = "discarded";
        DiscardedAt = now;
        UpdatedByPlatformUserId = actorId;
        UpdatedAt = now;
        Version++;
    }

    private void EnsureEditable()
    {
        if (Status != "in_progress")
            throw new InvalidOperationException($"Draft in status '{Status}' is not editable.");
    }

    private static string? Normalize(string? value, bool upper = false)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        return upper ? normalized.ToUpperInvariant() : normalized.ToLowerInvariant();
    }
}
