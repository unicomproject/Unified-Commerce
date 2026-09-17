using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderCollectionService
{
    Task<ApplicationResult<PosOnlineOrderCollectionValidateResponse>> ValidateQrAsync(
        TenantRequestContext context,
        Guid outletId,
        PosOnlineOrderCollectionValidateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderCollectionCompleteResponse>> CompleteAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderCollectionCompleteRequest request,
        CancellationToken cancellationToken);
}
