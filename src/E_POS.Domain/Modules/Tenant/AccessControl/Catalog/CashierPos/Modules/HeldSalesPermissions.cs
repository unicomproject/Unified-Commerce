#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class HeldSalesPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.held_sales.create", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.held_sales.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.sales.held_sales.recall", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.held_sales.popup.view", "pos.sales.held_sales.create", "HeldSales", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Park popup", true),
        new("pos.held_sales.popup.reference", "pos.sales.held_sales.create", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Park reference", true),
        new("pos.held_sales.popup.note", "pos.sales.held_sales.create", "HeldSales", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Park note", true),
        new("pos.held_sales.popup.expiry", "pos.sales.held_sales.create", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Park expiry", true),
        new("pos.held_sales.list.filters", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Filters", true),
        new("pos.held_sales.list.active_count", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Active count", true),
        new("pos.held_sales.list.customer", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Customer", true),
        new("pos.held_sales.list.value", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Value", true),
        new("pos.held_sales.list.item_count", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Item count", true),
        new("pos.held_sales.list.parked_time", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Parked time", true),
        new("pos.held_sales.list.expiry_time", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Expiry time", true),
        new("pos.held_sales.list.items", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Items", true),
        new("pos.held_sales.list.pagination", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Pagination", true),
        new("pos.held_sales.list.summary", "pos.sales.held_sales.view", "HeldSales", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Summary values", true),
        new("pos.sales.held_sales.cancel", "pos.sales.held_sales.create", "HeldSales", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.DocumentedResolved, "Approve independent cancel; historical create-alias insufficient for fine-grained target", true),
    ];
}
