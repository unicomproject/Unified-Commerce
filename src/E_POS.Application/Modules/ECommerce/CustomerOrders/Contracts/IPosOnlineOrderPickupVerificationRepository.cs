using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderPickupVerificationRepository
{
    Task<PosOnlineOrderPickupVerifyRepositoryResult> VerifyAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        string pickupCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderPickupCollectRepositoryResult> CollectAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosOnlineOrderPickupVerifyRepositoryResult(
    bool IsSuccess,
    PosOnlineOrderPickupVerifyResponse? Value = null,
    string? ErrorCode = null,
    int? RemainingAttempts = null)
{
    public static PosOnlineOrderPickupVerifyRepositoryResult Success(PosOnlineOrderPickupVerifyResponse value) =>
        new(true, value);

    public static PosOnlineOrderPickupVerifyRepositoryResult Failure(string errorCode, int? remainingAttempts = null) =>
        new(false, ErrorCode: errorCode, RemainingAttempts: remainingAttempts);
}

public sealed record PosOnlineOrderPickupCollectRepositoryResult(
    bool IsSuccess,
    PosOnlineOrderPickupCollectResponse? Value = null,
    string? ErrorCode = null)
{
    public static PosOnlineOrderPickupCollectRepositoryResult Success(PosOnlineOrderPickupCollectResponse value) =>
        new(true, value);

    public static PosOnlineOrderPickupCollectRepositoryResult Failure(string errorCode) =>
        new(false, ErrorCode: errorCode);
}
