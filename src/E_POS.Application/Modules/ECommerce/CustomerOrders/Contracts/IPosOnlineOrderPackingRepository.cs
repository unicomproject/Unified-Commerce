using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderPackingRepository
{
    Task<PosOnlineOrderPackingRepositoryResult> PackAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderPackRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderPackingRepositoryResult> MarkReadyAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderReadyRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosOnlineOrderPackingRepositoryResult(
    bool IsSuccess,
    PosOnlineOrderPackReadyCommandResponse? Command = null,
    string? ErrorCode = null)
{
    public static PosOnlineOrderPackingRepositoryResult Success(PosOnlineOrderPackReadyCommandResponse value) =>
        new(true, value);

    public static PosOnlineOrderPackingRepositoryResult Failure(string errorCode) =>
        new(false, ErrorCode: errorCode);
}
