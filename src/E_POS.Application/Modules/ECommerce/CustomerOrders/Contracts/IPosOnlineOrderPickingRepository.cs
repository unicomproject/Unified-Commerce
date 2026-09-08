using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderPickingRepository
{
    Task<PosOnlineOrderPickingRepositoryResult> GetAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        DateTimeOffset serverTime, CancellationToken cancellationToken);

    Task<PosOnlineOrderPickingRepositoryResult> PickLineAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickLineRequest request, DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderPickingRepositoryResult> ReportIssueAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickingIssueRequest request, DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosOnlineOrderPickingRepositoryResult> AddNoteAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        PosOnlineOrderPickingNoteRequest request, DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosOnlineOrderPickingRepositoryResult(
    bool IsSuccess,
    PosOnlineOrderPickingResponse? Picking = null,
    PosOnlineOrderPickingCommandResponse? Command = null,
    PosOnlineOrderPickingNoteCommandResponse? NoteCommand = null,
    string? ErrorCode = null)
{
    public static PosOnlineOrderPickingRepositoryResult QuerySuccess(PosOnlineOrderPickingResponse value) =>
        new(true, Picking: value);

    public static PosOnlineOrderPickingRepositoryResult CommandSuccess(PosOnlineOrderPickingCommandResponse value) =>
        new(true, Command: value);

    public static PosOnlineOrderPickingRepositoryResult NoteSuccess(PosOnlineOrderPickingNoteCommandResponse value) =>
        new(true, NoteCommand: value);

    public static PosOnlineOrderPickingRepositoryResult Failure(string errorCode) =>
        new(false, ErrorCode: errorCode);
}
