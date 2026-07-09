namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosTillSessionRepository
{
    Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken);

    Task<OpenTillRepositoryResult> OpenTillAsync(
        Guid tenantId,
        Guid tenantUserId,
        OpenTillCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CloseTillRepositoryResult> CloseTillAsync(
        Guid tenantId,
        Guid tenantUserId,
        CloseTillCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record OpenTillCommand(
    Guid DeviceId,
    Guid TillId,
    decimal OpeningFloat,
    string? OpeningNote);

public sealed record OpenTillRepositoryResult(
    bool IsSuccess,
    string? ErrorCode,
    CurrentTillSessionDbSnapshot? Snapshot);

public sealed record CurrentTillSessionResolveResult(
    bool IsSuccess,
    string? ErrorCode,
    CurrentTillSessionDbSnapshot? Snapshot);

public sealed record CurrentTillSessionDbSnapshot(
    Guid SessionId,
    Guid OutletId,
    Guid TillId,
    Guid OpenedDeviceId,
    decimal OpeningFloat,
    string Status,
    DateTimeOffset OpenedAt,
    string? OpeningNote);

public sealed record CloseTillCommand(
    Guid DeviceId,
    Guid TillId,
    decimal CountedCash,
    decimal? ExpectedCash,
    string? MismatchReason,
    string? ClosingNote);

public sealed record CloseTillRepositoryResult(
    bool IsSuccess,
    string? ErrorCode,
    ClosedTillSessionDbSnapshot? Snapshot);

public sealed record ClosedTillSessionDbSnapshot(
    Guid SessionId,
    Guid OutletId,
    Guid TillId,
    decimal OpeningFloat,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal CashDifference,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    string? ClosingNote);
