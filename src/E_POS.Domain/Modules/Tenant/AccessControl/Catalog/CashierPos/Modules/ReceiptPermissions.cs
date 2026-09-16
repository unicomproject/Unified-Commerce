#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class ReceiptPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.order_history.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.receipts.digital.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.receipts.physical.print", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.receipts.history.reprint", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.orders.history.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.receipts.details.store", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Store", true),
        new("pos.receipts.details.receipt_number", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Receipt number", true),
        new("pos.receipts.details.datetime", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Date/time", true),
        new("pos.receipts.details.cashier", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cashier", true),
        new("pos.receipts.details.customer", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Customer", true),
        new("pos.receipts.details.terminal", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Terminal", true),
        new("pos.receipts.details.payment_method", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Payment method", true),
        new("pos.receipts.details.items", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Items", true),
        new("pos.receipts.details.item_quantity", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Item quantity", true),
        new("pos.receipts.details.item_value", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Item value", true),
        new("pos.receipts.details.item_rate", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Item rate", true),
        new("pos.receipts.details.subtotal", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Subtotal", true),
        new("pos.receipts.details.discount", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Discount", true),
        new("pos.receipts.details.total", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Total", true),
        new("pos.receipts.details.paid_amount", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Paid amount", true),
        new("pos.receipts.details.change_due", "pos.receipts.digital.view", "Receipts", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Change due", true),
    ];
}
