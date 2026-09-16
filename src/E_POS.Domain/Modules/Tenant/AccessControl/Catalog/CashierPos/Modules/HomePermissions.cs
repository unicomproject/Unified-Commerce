#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class HomePermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.sales.dashboard.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.home.profile.view", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Cashier profile", true),
        new("pos.home.profile.avatar", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Avatar", true),
        new("pos.home.profile.name", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cashier name", true),
        new("pos.home.profile.role", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Cashier role", true),
        new("pos.home.session_summary.view", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Session summary", true),
        new("pos.home.session_summary.total_sales", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Total sales", true),
        new("pos.home.session_summary.transaction_count", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Transaction count", true),
        new("pos.home.session_summary.returns", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Returns metric", true),
        new("pos.home.session_summary.discounts", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Discounts", true),
        new("pos.home.session_summary.net_sales", "pos.sales.dashboard.view", "Home", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Net sales", true),
        new("pos.home.actions.online_orders_entry", "commerce.online_order.orders.access", "Home", CashierPosPermissionSemanticType.Navigation, false, CashierPosPermissionDefinitionKind.New, "Home Online Orders entry chrome under commerce access", true),
        new("pos.home.actions.returns_entry", "pos.returns.search_sale.view", "Home", CashierPosPermissionSemanticType.Navigation, false, CashierPosPermissionDefinitionKind.New, "Home Returns entry chrome under returns view", true),
    ];
}
