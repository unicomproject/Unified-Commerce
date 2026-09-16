#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class SaleCompletePermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sale_complete.message.success", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "Success message", true),
        new("pos.sale_complete.details.receipt_number", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Receipt number", true),
        new("pos.sale_complete.details.payment_method", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Payment method", true),
        new("pos.sale_complete.details.datetime", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Date/time", true),
        new("pos.sale_complete.details.cashier", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cashier", true),
        new("pos.sale_complete.details.customer", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Customer", true),
        new("pos.sale_complete.details.cash_received", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Cash received", true),
        new("pos.sale_complete.details.change_due", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Change due", true),
        new("pos.sale_complete.details.total_paid", "pos.receipts.digital.view", "SaleComplete", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Total paid", true),
    ];
}
