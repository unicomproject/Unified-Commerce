using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosHoldRepository
{
    Task<PosCancelHoldRepositoryResult> CancelHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid holdId,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosRecallHoldRepositoryResult> RecallHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        Guid holdId,
        PosRecallHoldRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosCreateHoldRepositoryResult> CreateHoldAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> permissions,
        PosCreateHoldRequestDto request,
        DateTimeOffset heldAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<PosGetActiveHoldsRepositoryResult> GetActiveHoldsAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosHoldListQueryDto query,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosCreateHoldRepositoryResult(
    string? ErrorCode,
    PosHoldListItemDto? Hold)
{
    public bool IsSuccess => ErrorCode is null && Hold is not null;
}

public sealed record PosRecallHoldRepositoryResult(
    string? ErrorCode,
    PosRecallHoldResponseDto? Recall)
{
    public bool IsSuccess => ErrorCode is null && Recall is not null;
}

public sealed record PosCancelHoldRepositoryResult(string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;
}

public sealed record PosGetActiveHoldsRepositoryResult(
    string? ErrorCode,
    IReadOnlyList<PosHoldListItemDto>? Holds,
    int TotalCount = 0,
    int TotalValue = 0,
    string? Currency = null)
{
    public bool IsSuccess => ErrorCode is null && Holds is not null &&
        !string.IsNullOrWhiteSpace(Currency);
}
