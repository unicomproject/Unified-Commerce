namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;

public static class OnlineOrderPickingPermissions
{
    public const string OrdersAccess = "commerce.online_order.orders.access";
    public const string OrdersView = "commerce.online_order.orders.view";
    public const string PickingView = "commerce.online_order.picking.view";
    public const string PickingPick = "commerce.online_order.picking.pick";
    public const string PickingScan = "commerce.online_order.picking.scan";
    public const string PickingManualEntry = "commerce.online_order.picking.manual_entry";
    public const string PickingReportIssue = "commerce.online_order.picking.report_issue";
    public const string PickingNote = "commerce.online_order.picking.note";

    public static IReadOnlyList<string> All { get; } =
    [
        OrdersAccess,
        OrdersView,
        PickingView,
        PickingPick,
        PickingScan,
        PickingManualEntry,
        PickingReportIssue,
        PickingNote
    ];
}
