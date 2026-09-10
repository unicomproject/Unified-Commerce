#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class CashMovementPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.cash_drawer.movements.cash_in", "pos.cash_drawer.movements.create", "CashDrawer", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.New, "Independent Cash In action", true),
        new("pos.cash_drawer.movements.cash_out", "pos.cash_drawer.movements.create", "CashDrawer", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.New, "Independent Cash Out action", true),
        new("pos.cash_drawer.movements.cash_drop", "pos.cash_drawer.movements.create", "CashDrawer", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.New, "Independent Cash Drop action", true),
        new("pos.cash_movements.cash_in.till", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "cash_in Till", true),
        new("pos.cash_movements.cash_in.expected_cash", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_in Expected cash", true),
        new("pos.cash_movements.cash_in.available_cash", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_in Available/opening cash", true),
        new("pos.cash_movements.cash_in.amount_entry", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "cash_in Amount entry", true),
        new("pos.cash_movements.cash_in.reason", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "cash_in Reason", true),
        new("pos.cash_movements.cash_in.note", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "cash_in Note", true),
        new("pos.cash_movements.cash_in.manager_pin", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "cash_in Manager PIN", true),
        new("pos.cash_movements.cash_in.summary", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "cash_in Summary", true),
        new("pos.cash_movements.cash_in.resulting_balance", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_in Resulting balance", true),
        new("pos.cash_movements.cash_in.validation_message", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "cash_in Validation", true),
        new("pos.cash_movements.cash_in.confirm", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "cash_in Confirm", true),
        new("pos.cash_movements.cash_in.cancel", "pos.cash_drawer.movements.cash_in", "CashMovements", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "cash_in Cancel", true),
        new("pos.cash_movements.cash_out.till", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "cash_out Till", true),
        new("pos.cash_movements.cash_out.expected_cash", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_out Expected cash", true),
        new("pos.cash_movements.cash_out.available_cash", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_out Available/opening cash", true),
        new("pos.cash_movements.cash_out.amount_entry", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "cash_out Amount entry", true),
        new("pos.cash_movements.cash_out.reason", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "cash_out Reason", true),
        new("pos.cash_movements.cash_out.note", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "cash_out Note", true),
        new("pos.cash_movements.cash_out.manager_pin", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "cash_out Manager PIN", true),
        new("pos.cash_movements.cash_out.summary", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "cash_out Summary", true),
        new("pos.cash_movements.cash_out.resulting_balance", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_out Resulting balance", true),
        new("pos.cash_movements.cash_out.validation_message", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "cash_out Validation", true),
        new("pos.cash_movements.cash_out.confirm", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "cash_out Confirm", true),
        new("pos.cash_movements.cash_out.cancel", "pos.cash_drawer.movements.cash_out", "CashMovements", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "cash_out Cancel", true),
        new("pos.cash_movements.cash_drop.till", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Till", true),
        new("pos.cash_movements.cash_drop.expected_cash", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_drop Expected cash", true),
        new("pos.cash_movements.cash_drop.available_cash", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_drop Available/opening cash", true),
        new("pos.cash_movements.cash_drop.amount_entry", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "cash_drop Amount entry", true),
        new("pos.cash_movements.cash_drop.reason", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Reason", true),
        new("pos.cash_movements.cash_drop.note", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Note", true),
        new("pos.cash_movements.cash_drop.manager_pin", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "cash_drop Manager PIN", true),
        new("pos.cash_movements.cash_drop.summary", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Summary", true),
        new("pos.cash_movements.cash_drop.resulting_balance", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "cash_drop Resulting balance", true),
        new("pos.cash_movements.cash_drop.validation_message", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Validation", true),
        new("pos.cash_movements.cash_drop.confirm", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Confirm", true),
        new("pos.cash_movements.cash_drop.cancel", "pos.cash_drawer.movements.cash_drop", "CashMovements", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Split, "cash_drop Cancel", true),
    ];
}
