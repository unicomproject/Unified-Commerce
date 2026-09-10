#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CustomerPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.customers.management.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.customers.management.create", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.customers.management.update", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.customers.list.search", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Search", true),
        new("pos.customers.list.filters", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Filters", true),
        new("pos.customers.list.id", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "ID", true),
        new("pos.customers.list.name", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Name", true),
        new("pos.customers.list.phone", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Phone", true),
        new("pos.customers.list.email", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Email", true),
        new("pos.customers.list.source", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Source", true),
        new("pos.customers.list.status", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Status", true),
        new("pos.customers.list.order_count", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Order count", true),
        new("pos.customers.list.total_spend", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Total spend", true),
        new("pos.customers.list.pagination", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Pagination", true),
        new("pos.customers.details.joined_date", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Joined date", true),
        new("pos.customers.details.average_order_value", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "AOV", true),
        new("pos.customers.history.recent_purchases", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Recent purchases", true),
        new("pos.customers.history.purchase_amounts", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Purchase amounts", true),
        new("pos.customers.history.purchase_history", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Purchase history", true),
        new("pos.customers.management.attach_sale", "pos.customers.management.view", "Customers", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.New, "Attach customer to active sale", true),
        new("pos.customers.management.deactivate", "pos.customers.management.update", "Customers", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.New, "Deactivate customer", true),
    ];
}
