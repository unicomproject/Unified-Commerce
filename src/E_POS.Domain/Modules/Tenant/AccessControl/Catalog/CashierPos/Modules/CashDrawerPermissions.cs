#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CashDrawerPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.cash_drawer.position.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.cash_drawer.physical.manage", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.cash_drawer.movements.create", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.cash_drawer.summary.till", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Till", true),
        new("pos.cash_drawer.summary.status", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Status, false, CashierPosPermissionDefinitionKind.Split, "Status", true),
        new("pos.cash_drawer.summary.opening_cash", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Opening cash", true),
        new("pos.cash_drawer.summary.cash_sales", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Cash sales", true),
        new("pos.cash_drawer.summary.expected_cash", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Expected cash", true),
        new("pos.cash_drawer.movements.list", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Movement list", true),
        new("pos.cash_drawer.movements.type", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Movement type", true),
        new("pos.cash_drawer.movements.date", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Movement date", true),
        new("pos.cash_drawer.movements.time", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Movement time", true),
        new("pos.cash_drawer.movements.cashier", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Movement cashier", true),
        new("pos.cash_drawer.movements.amount_view", "pos.cash_drawer.position.view", "CashDrawer", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Movement amount", true),
        new("pos.cash_drawer.open_popup.view", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Open drawer popup", true),
        new("pos.cash_drawer.open_popup.cancel", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Cancel", true),
        new("pos.cash_drawer.open_popup.continue", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "Continue", true),
        new("pos.cash_drawer.open_reason.provide_change", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.New, "Open reason: provide change", true),
        new("pos.cash_drawer.open_reason.till_check", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.New, "Open reason: till check", true),
        new("pos.cash_drawer.open_reason.cash_count", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.New, "Open reason: cash count", true),
        new("pos.cash_drawer.open_reason.manager_operation", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.New, "Open reason: manager operation", true),
        new("pos.cash_drawer.open_reason.other", "pos.cash_drawer.physical.manage", "CashDrawer", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.New, "Open reason: other", true),
    ];
}
