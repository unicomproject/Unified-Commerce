namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;

/// <summary>
/// Canonical commerce Online Order permissions used by Cashier POS click &amp; collect.
/// Spelling is British <c>fulfilment</c> (see Permission_Code_List).
/// </summary>
public static class OnlineOrderPickingPermissions
{
    public const string OrdersAccess = "commerce.online_order.orders.access";
    public const string OrdersView = "commerce.online_order.orders.view";
    public const string FulfilmentStart = "commerce.online_order.fulfilment.start";
    public const string PickingView = "commerce.online_order.picking.view";
    public const string PickingPick = "commerce.online_order.picking.pick";
    public const string PickingScan = "commerce.online_order.picking.scan";
    public const string PickingManualEntry = "commerce.online_order.picking.manual_entry";
    public const string PickingReportIssue = "commerce.online_order.picking.report_issue";
    public const string PickingNote = "commerce.online_order.picking.note";
    public const string PackingView = "commerce.online_order.packing.view";
    public const string PackingPack = "commerce.online_order.packing.pack";
    public const string CollectionMarkReady = "commerce.online_order.collection.mark_ready";
    public const string CollectionViewReady = "commerce.online_order.collection.view_ready";
    public const string CollectionNotifyCustomer = "commerce.online_order.collection.notify_customer";

    /// <summary>American misspelling — must never be treated as a real permission.</summary>
    public const string FulfilmentStartWrongSpelling = "commerce.online_order.fulfillment.start";

    public static IReadOnlyList<string> All { get; } =
    [
        OrdersAccess,
        OrdersView,
        FulfilmentStart,
        PickingView,
        PickingPick,
        PickingScan,
        PickingManualEntry,
        PickingReportIssue,
        PickingNote,
        PackingView,
        PackingPack,
        CollectionMarkReady
    ];
}
