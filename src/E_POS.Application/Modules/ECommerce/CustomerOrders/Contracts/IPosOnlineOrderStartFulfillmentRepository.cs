using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderStartFulfillmentRepository
{
    Task<PosOnlineOrderStartFulfillmentRepositoryResult> StartAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        long expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosOnlineOrderStartFulfillmentRepositoryResult(
    string? ErrorCode,
    PosOnlineOrderStartFulfillmentResponse? Response)
{
    public bool IsSuccess => ErrorCode is null && Response is not null;

    public static PosOnlineOrderStartFulfillmentRepositoryResult Success(PosOnlineOrderStartFulfillmentResponse response) =>
        new(null, response);

    public static PosOnlineOrderStartFulfillmentRepositoryResult Failure(string errorCode) =>
        new(errorCode, null);
}
