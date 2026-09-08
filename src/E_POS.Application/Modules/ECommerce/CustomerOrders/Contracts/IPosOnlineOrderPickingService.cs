using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderPickingService
{
    Task<ApplicationResult<PosOnlineOrderPickingResponse>> GetAsync(
        TenantRequestContext context, Guid outletId, Guid orderId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderPickingCommandResponse>> PickLineAsync(
        TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickLineRequest request, CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderPickingCommandResponse>> ReportIssueAsync(
        TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickingIssueRequest request, CancellationToken cancellationToken);

    Task<ApplicationResult<PosOnlineOrderPickingNoteCommandResponse>> AddNoteAsync(
        TenantRequestContext context, Guid outletId, Guid orderId,
        PosOnlineOrderPickingNoteRequest request, CancellationToken cancellationToken);
}
