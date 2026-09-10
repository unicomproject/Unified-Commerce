using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderPackingService
{
    Task<ApplicationResult<PosOnlineOrderPackReadyCommandResponse>> PackAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderPackRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderPackReadyCommandResponse>> MarkReadyAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderReadyRequest request,
        CancellationToken cancellationToken);
}
