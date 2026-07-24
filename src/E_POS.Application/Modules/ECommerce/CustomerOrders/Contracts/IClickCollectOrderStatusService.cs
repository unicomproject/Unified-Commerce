using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IClickCollectOrderStatusService
{
    Task<ApplicationResult<ClickCollectOrderStatusUpdateResponse>> UpdateStatusAsync(
        TenantRequestContext context,
        Guid orderId,
        ClickCollectOrderStatusUpdateRequest request,
        CancellationToken cancellationToken);
}
