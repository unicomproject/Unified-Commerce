using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Dtos;

namespace E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;

public interface IPosOnlineOrderService
{
    Task<ApplicationResult<PosOnlineOrderListDto>> ListAsync(
        TenantRequestContext context,
        PosOnlineOrderListQuery query,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderDetailDto>> GetAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosStartFulfillmentDto>> StartFulfillmentAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosPickingOrderDto>> GetPickingAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosFulfillmentCommandDto>> PickLineAsync(TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId, PosPickLineRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<PosFulfillmentCommandDto>> ReportIssueAsync(TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId, PosReportPickingIssueRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<PosFulfillmentCommandDto>> PackAsync(TenantRequestContext context, Guid outletId, Guid orderId, PosPackOrderRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<PosFulfillmentCommandDto>> MarkReadyAsync(TenantRequestContext context, Guid outletId, Guid orderId, CancellationToken cancellationToken);
}
