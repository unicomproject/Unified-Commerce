using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderDetailService
{
    Task<ApplicationResult<PosOnlineOrderListResponse>> ListAsync(
        TenantRequestContext context,
        PosOnlineOrderListQuery query,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderDetailResponse>> GetAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);
}
