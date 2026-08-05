using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface ICustomerOrderRepository :
    ICustomerOrderReadRepository,
    ICustomerOrderCancelRepository
{
}

public interface ICustomerOrderReadRepository
{
    Task<CustomerOrderListReadModel> GetAsync(
        Guid tenantId,
        Guid customerId,
        string? normalizedStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CustomerOrderDetailReadModel?> GetDetailAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CancellationToken cancellationToken);
}

public interface ICustomerOrderCancelRepository
{
    Task<CustomerOrderCancelRepositoryResult> CancelAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record CustomerOrderCancelRepositoryResult(
    string? ErrorCode,
    string? ErrorMessage,
    CustomerOrderCancelResponse? Response,
    CustomerOrderNotificationContext? NotificationContext)
{
    public bool IsSuccess => ErrorCode is null && Response is not null;

    public static CustomerOrderCancelRepositoryResult Success(
        CustomerOrderCancelResponse response,
        CustomerOrderNotificationContext? notificationContext = null) =>
        new(null, null, response, notificationContext);

    public static CustomerOrderCancelRepositoryResult Failure(string errorCode, string? errorMessage = null) =>
        new(errorCode, errorMessage, null, null);
}