#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class NotificationPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.notifications.alerts.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.notifications.panel.view", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Notification panel", true),
        new("pos.notifications.panel.unread_count", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Unread count", true),
        new("pos.notifications.messages.list", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Message list", true),
        new("pos.notifications.messages.title", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Message title", true),
        new("pos.notifications.messages.body", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Message body", true),
        new("pos.notifications.messages.timestamp", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Message timestamp", true),
        new("pos.notifications.messages.open", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Open message", true),
        new("pos.notifications.messages.mark_read", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Mark read", true),
        new("pos.notifications.messages.dismiss", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Dismiss", true),
        new("pos.notifications.messages.mark_all_read", "pos.notifications.alerts.view", "Notifications", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Mark all read", true),
    ];
}
