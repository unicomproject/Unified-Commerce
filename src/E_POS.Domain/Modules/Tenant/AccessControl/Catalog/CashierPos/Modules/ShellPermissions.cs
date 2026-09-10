#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class ShellPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.shell.topbar.container", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Top bar container", true),
        new("pos.shell.topbar.brand", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Brand/logo", true),
        new("pos.shell.topbar.session_status", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Status, false, CashierPosPermissionDefinitionKind.Split, "Till session status", true),
        new("pos.shell.topbar.outlet", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Outlet", true),
        new("pos.shell.topbar.till", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Till", true),
        new("pos.shell.topbar.connectivity", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Status, false, CashierPosPermissionDefinitionKind.Split, "Connectivity state", true),
        new("pos.shell.topbar.clock", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Clock", true),
        new("pos.shell.topbar.notification_bell", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Navigation, false, CashierPosPermissionDefinitionKind.Split, "Notification bell chrome", true),
        new("pos.shell.bottom_nav.container", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Bottom navigation container", true),
        new("pos.shell.navigation.settings", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Navigation, false, CashierPosPermissionDefinitionKind.New, "Settings destination — no existing business permission", true),
        new("pos.shell.navigation.offline_banner", "pos.sales.dashboard.view", "Shell", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.New, "Offline/connectivity banner surface", true),
    ];
}
