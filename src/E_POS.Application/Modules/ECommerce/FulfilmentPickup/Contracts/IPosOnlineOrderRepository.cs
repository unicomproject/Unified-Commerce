using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Dtos;

namespace E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;

public interface IPosOnlineOrderRepository
{
    Task<bool> CanAccessOutletAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderListDto> ListAsync(
        Guid tenantId,
        PosOnlineOrderListQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderDetailDto?> GetAsync(
        Guid tenantId,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<PosStartFulfillmentDto?> StartFulfillmentAsync(
        Guid tenantId,
        Guid outletId,
        Guid orderId,
        Guid tenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosPickingOrderDto?> GetPickingAsync(
        Guid tenantId,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken);
    Task<PosFulfillmentCommandDto?> PickLineAsync(Guid tenantId, Guid outletId, Guid orderId, Guid lineId, Guid userId, PosPickLineRequest request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PosFulfillmentCommandDto?> ReportIssueAsync(Guid tenantId, Guid outletId, Guid orderId, Guid lineId, Guid userId, PosReportPickingIssueRequest request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PosFulfillmentCommandDto?> PackAsync(Guid tenantId, Guid outletId, Guid orderId, Guid userId, PosPackOrderRequest request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PosFulfillmentCommandDto?> MarkReadyAsync(Guid tenantId, Guid outletId, Guid orderId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
