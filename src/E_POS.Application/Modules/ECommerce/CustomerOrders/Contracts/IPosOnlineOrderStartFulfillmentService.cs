using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderStartFulfillmentService
{
    Task<ApplicationResult<PosOnlineOrderStartFulfillmentResponse>> StartAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderStartFulfillmentRequest request,
        CancellationToken cancellationToken);
}
