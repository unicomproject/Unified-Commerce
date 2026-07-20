using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface ICustomerOrderService
{
    Task<ApplicationResult<CustomerOrderListReadModel>> GetAsync(
        Guid tenantId,
        Guid customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerOrderDetailReadModel>> GetDetailAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerOrderCancelResponse>> CancelAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CustomerOrderCancelRequest request,
        CancellationToken cancellationToken);
}
