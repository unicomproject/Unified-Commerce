namespace E_POS.Domain.Modules.Shared.Idempotency.Entities;

public class IdempotencyRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string Status { get; set; } = string.Empty; // "IN_PROGRESS", "COMPLETED", "FAILED"
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTimeOffset? ProcessingLeasedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static IdempotencyRequest Create(
        Guid id,
        Guid tenantId,
        Guid actorUserId,
        string endpoint,
        string idempotencyKey,
        string requestHash,
        DateTimeOffset now)
    {
        return new IdempotencyRequest
        {
            Id = id,
            TenantId = tenantId,
            ActorUserId = actorUserId,
            Endpoint = endpoint.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            RequestHash = requestHash,
            Status = "IN_PROGRESS",
            AttemptCount = 1,
            ProcessingLeasedUntil = now.AddMinutes(5), // 5 minute processing lease initial
            ExpiresAt = now.AddDays(1), // 24 hours expiry
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RenewLease(DateTimeOffset now, TimeSpan duration)
    {
        ProcessingLeasedUntil = now.Add(duration);
        AttemptCount++;
        UpdatedAt = now;
    }

    public void Complete(int responseStatusCode, string? responseBody, DateTimeOffset now)
    {
        Status = "COMPLETED";
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        CompletedAt = now;
        ProcessingLeasedUntil = null;
        UpdatedAt = now;
    }

    public void Fail(string errorCode, DateTimeOffset now)
    {
        Status = "FAILED";
        LastErrorCode = errorCode;
        ProcessingLeasedUntil = null;
        UpdatedAt = now;
    }
}
