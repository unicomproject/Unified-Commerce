using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Integration.Entities;

public sealed class IntegrationOutboxMessage : AuditableEntity
{
    public string MessageType { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public long AggregateSequence { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public int PayloadSchemaVersion { get; private set; } = 1;
    public string DeduplicationKey { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";
    public int AttemptCount { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseExpiresAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? SanitizedLastError { get; private set; }

    public static IntegrationOutboxMessage Create(Guid id, string messageType, string aggregateType,
        Guid aggregateId, long aggregateSequence, Guid? tenantId, Guid correlationId, Guid? causationId,
        string payloadJson, string deduplicationKey, DateTimeOffset now)
    {
        return new IntegrationOutboxMessage
        {
            Id = id,
            MessageType = messageType.Trim(),
            AggregateType = aggregateType.Trim(),
            AggregateId = aggregateId,
            AggregateSequence = aggregateSequence,
            TenantId = tenantId,
            CorrelationId = correlationId,
            CausationId = causationId,
            PayloadJson = payloadJson,
            DeduplicationKey = deduplicationKey.Trim(),
            AvailableAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool TryAcquire(string workerId, DateTimeOffset now, TimeSpan lease)
    {
        if (Status == "DELIVERED" || Status == "FAILED_FINAL") return false;
        if (Status == "PROCESSING" && LeaseExpiresAt > now) return false;
        Status = "PROCESSING";
        LeaseOwner = workerId;
        LeaseExpiresAt = now.Add(lease);
        AttemptCount++;
        UpdatedAt = now;
        return true;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        Status = "DELIVERED";
        ProcessedAt = now;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        LastErrorCode = null;
        SanitizedLastError = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string code, string safeError, bool terminal, DateTimeOffset nextAttempt, DateTimeOffset now)
    {
        Status = terminal ? "FAILED_FINAL" : "FAILED_RETRYABLE";
        LastErrorCode = code;
        SanitizedLastError = safeError;
        AvailableAt = nextAttempt;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        UpdatedAt = now;
    }

    public void RetryNow(DateTimeOffset now)
    {
        if (Status is not ("FAILED_RETRYABLE" or "FAILED_FINAL"))
            throw new InvalidOperationException("Only failed outbox messages can be retried.");
        Status = "PENDING";
        AvailableAt = now;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        LastErrorCode = null;
        SanitizedLastError = null;
        UpdatedAt = now;
    }
}
