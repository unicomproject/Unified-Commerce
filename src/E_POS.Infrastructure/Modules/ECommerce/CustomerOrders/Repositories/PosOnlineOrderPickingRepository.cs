using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed partial class PosOnlineOrderPickingRepository : CustomerOrderRepositoryBase, IPosOnlineOrderPickingRepository, IPosOnlineOrderReadyRepository
{
    public const string LinePickedEvent = "FULFILLMENT_LINE_PICKED";
    public const string IssueReportedEvent = "FULFILLMENT_LINE_ISSUE_REPORTED";
    public const string PickingCompletedEvent = "FULFILLMENT_PICKING_COMPLETED";
    public const string PickingNoteAddedEvent = "FULFILLMENT_PICKING_NOTE_ADDED";
    private const int PickingNoteHistoryLimit = 50;

    public PosOnlineOrderPickingRepository(
        EPosDbContext dbContext,
        IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }
}
