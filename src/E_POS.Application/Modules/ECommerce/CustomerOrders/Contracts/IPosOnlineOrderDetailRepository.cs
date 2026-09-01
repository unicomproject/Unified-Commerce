using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderDetailRepository
{
    Task<PosOnlineOrderListRepositoryResult> ListAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosOnlineOrderListQuery query,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderDetailRepositoryResult> GetAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken);
}

public sealed record PosOnlineOrderListRepositoryResult(
    string? ErrorCode,
    PosOnlineOrderListResponse? List)
{
    public bool IsSuccess => ErrorCode is null && List is not null;

    public static PosOnlineOrderListRepositoryResult Success(PosOnlineOrderListResponse list) =>
        new(null, list);

    public static PosOnlineOrderListRepositoryResult Failure(string errorCode) =>
        new(errorCode, null);
}

public sealed record PosOnlineOrderDetailRepositoryResult(
    string? ErrorCode,
    PosOnlineOrderDetailResponse? Detail)
{
    public bool IsSuccess => ErrorCode is null && Detail is not null;

    public static PosOnlineOrderDetailRepositoryResult Success(PosOnlineOrderDetailResponse detail) =>
        new(null, detail);

    public static PosOnlineOrderDetailRepositoryResult Failure(string errorCode) =>
        new(errorCode, null);
}
