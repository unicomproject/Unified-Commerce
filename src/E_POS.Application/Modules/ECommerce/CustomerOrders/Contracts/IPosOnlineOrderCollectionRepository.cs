using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderCollectionRepository
{
    Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>> ValidateQrAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>> CompleteAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        long expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosOnlineOrderCollectionRepositoryResult<T>(
    bool IsSuccess,
    T? Value = default,
    string? ErrorCode = null)
{
    public static PosOnlineOrderCollectionRepositoryResult<T> Success(T value) =>
        new(true, value);

    public static PosOnlineOrderCollectionRepositoryResult<T> Failure(string errorCode) =>
        new(false, ErrorCode: errorCode);
}
