using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IClickCollectOrderStatusRepository
{
    Task<ClickCollectOrderStatusUpdateRepositoryResult> UpdateStatusAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid orderId,
        string targetStatus,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record ClickCollectOrderStatusUpdateRepositoryResult(
    string? ErrorCode,
    string? ErrorMessage,
    ClickCollectOrderStatusUpdateResponse? Response)
{
    public bool IsSuccess => ErrorCode is null && Response is not null;

    public static ClickCollectOrderStatusUpdateRepositoryResult Success(ClickCollectOrderStatusUpdateResponse response) =>
        new(null, null, response);

    public static ClickCollectOrderStatusUpdateRepositoryResult Failure(string errorCode, string? errorMessage = null) =>
        new(errorCode, errorMessage, null);
}
