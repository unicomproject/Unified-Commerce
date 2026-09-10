#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class DiscountPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.manual_discount.apply", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.discount.panel.view", "pos.sales.manual_discount.apply", "Discount", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Discount panel", true),
        new("pos.discount.panel.amount_entry", "pos.sales.manual_discount.apply", "Discount", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "Discount amount entry", true),
        new("pos.discount.panel.reason_entry", "pos.sales.manual_discount.apply", "Discount", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Discount reason", true),
        new("pos.discount.panel.apply_action", "pos.sales.manual_discount.apply", "Discount", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Apply discount control", true),
        new("pos.discount.panel.cancel_action", "pos.sales.manual_discount.apply", "Discount", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Cancel discount control", true),
    ];
}
