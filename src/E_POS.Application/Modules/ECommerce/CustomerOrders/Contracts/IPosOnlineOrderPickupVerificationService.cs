using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderPickupVerificationService
{
    Task<ApplicationResult<PosOnlineOrderPickupVerifyResponse>> VerifyAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderPickupVerifyRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderPickupCollectResponse>> CollectAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);
}
