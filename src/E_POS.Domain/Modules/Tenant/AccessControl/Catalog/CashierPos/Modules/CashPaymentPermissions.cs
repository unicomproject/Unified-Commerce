#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CashPaymentPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.payments.cash.accept", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.cash_payment.summary.order", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Order summary", true),
        new("pos.cash_payment.line.item", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Item", true),
        new("pos.cash_payment.line.quantity", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Quantity", true),
        new("pos.cash_payment.line.price", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Price", true),
        new("pos.cash_payment.line.item_total", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Item total", true),
        new("pos.cash_payment.summary.subtotal", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Subtotal", true),
        new("pos.cash_payment.summary.discount", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Discount", true),
        new("pos.cash_payment.summary.tax", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Tax", true),
        new("pos.cash_payment.summary.total_due", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Total due", true),
        new("pos.cash_payment.tender.amount_received_view", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Amount received display", true),
        new("pos.cash_payment.tender.amount_received_entry", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "Amount received entry", true),
        new("pos.cash_payment.tender.due_amount", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Due amount", true),
        new("pos.cash_payment.tender.exact", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Exact Cash", true),
        new("pos.cash_payment.quick_amounts.container", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Quick Amount container", true),
        new("pos.cash_payment.quick_amounts.slot_1", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Quick Amount slot 1", true),
        new("pos.cash_payment.quick_amounts.slot_2", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Quick Amount slot 2", true),
        new("pos.cash_payment.quick_amounts.slot_3", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Quick Amount slot 3", true),
        new("pos.cash_payment.numpad.container", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Numpad container", true),
        new("pos.cash_payment.numpad.digit_0", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 0", true),
        new("pos.cash_payment.numpad.digit_1", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 1", true),
        new("pos.cash_payment.numpad.digit_2", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 2", true),
        new("pos.cash_payment.numpad.digit_3", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 3", true),
        new("pos.cash_payment.numpad.digit_4", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 4", true),
        new("pos.cash_payment.numpad.digit_5", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 5", true),
        new("pos.cash_payment.numpad.digit_6", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 6", true),
        new("pos.cash_payment.numpad.digit_7", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 7", true),
        new("pos.cash_payment.numpad.digit_8", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 8", true),
        new("pos.cash_payment.numpad.digit_9", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 9", true),
        new("pos.cash_payment.numpad.digit_00", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Key 00", true),
        new("pos.cash_payment.numpad.decimal", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Decimal", true),
        new("pos.cash_payment.controls.backspace", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Backspace", true),
        new("pos.cash_payment.controls.clear", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Clear", true),
        new("pos.cash_payment.tender.change_due", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Change due", true),
        new("pos.cash_payment.completion.execute", "pos.payments.cash.accept", "CashPayment", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Complete sale", true),
    ];
}
