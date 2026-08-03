namespace E_POS.LocalPrintAgent.Idempotency;

public interface IPrintRequestStore
{
    Task<PrintRequestClaim> TryClaimAsync(Guid requestId, string payloadHash, CancellationToken cancellationToken);
    Task CompleteAsync(Guid requestId, bool success, string resultCode, CancellationToken cancellationToken);
    Task<PrintRequestStatus?> GetStatusAsync(Guid requestId, CancellationToken cancellationToken);
    Task<bool> ProbeAsync(CancellationToken cancellationToken);
    long CountCompleted();
    long CountUnresolved();
    long RecordAgentStart();
}

public sealed record PrintRequestClaim(bool Acquired, bool PayloadConflict, string? PreviousResultCode);

public sealed record PrintRequestStatus(
    Guid RequestId,
    string State,
    string ResultCode,
    bool? Success,
    DateTimeOffset ClaimedAt,
    DateTimeOffset? CompletedAt);

public sealed class IdempotencyRecordCorruptedException(Guid requestId)
    : IOException($"The idempotency record for request {requestId} is corrupted.")
{
    public Guid RequestId { get; } = requestId;
}
