#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class TillPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pos.till.session.open", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.till.session.close", null, "Existing", CashierPosPermissionSemanticType.Action, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.till.session.view", null, "Existing", CashierPosPermissionSemanticType.Screen, false, CashierPosPermissionDefinitionKind.Existing, "Approved existing canonical business permission", true),
        new("pos.till.opening.starting_cash_view", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Starting cash view", true),
        new("pos.till.opening.starting_cash_entry", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "Starting cash entry", true),
        new("pos.till.opening.validation_message", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "Validation", true),
        new("pos.till.opening.note_view", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Note view", true),
        new("pos.till.opening.note_entry", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Note entry", true),
        new("pos.till.opening.quick_amounts", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Quick amounts", true),
        new("pos.till.opening.quick_slot_1", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Quick slot 1", true),
        new("pos.till.opening.quick_slot_2", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Quick slot 2", true),
        new("pos.till.opening.quick_slot_3", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Quick slot 3", true),
        new("pos.till.opening.numpad", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Container, false, CashierPosPermissionDefinitionKind.Split, "Numpad", true),
        new("pos.till.opening.backspace", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Backspace", true),
        new("pos.till.opening.clear", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Clear", true),
        new("pos.till.opening.confirm_message", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Message, false, CashierPosPermissionDefinitionKind.Split, "Confirm message", true),
        new("pos.till.opening.key_0", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 0", true),
        new("pos.till.opening.key_1", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 1", true),
        new("pos.till.opening.key_2", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 2", true),
        new("pos.till.opening.key_3", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 3", true),
        new("pos.till.opening.key_4", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 4", true),
        new("pos.till.opening.key_5", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 5", true),
        new("pos.till.opening.key_6", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 6", true),
        new("pos.till.opening.key_7", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 7", true),
        new("pos.till.opening.key_8", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 8", true),
        new("pos.till.opening.key_9", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 9", true),
        new("pos.till.opening.key_00", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key 00", true),
        new("pos.till.opening.key_decimal", "pos.till.session.open", "Till", CashierPosPermissionSemanticType.Control, false, CashierPosPermissionDefinitionKind.Split, "Open till numpad key decimal", true),
        new("pos.till.closing.back", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Navigation, false, CashierPosPermissionDefinitionKind.Split, "Back", true),
        new("pos.till.closing.till", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Till", true),
        new("pos.till.closing.opened_by", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Opened by", true),
        new("pos.till.closing.opened_time", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Field, false, CashierPosPermissionDefinitionKind.Split, "Opened time", true),
        new("pos.till.closing.expected_cash", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Expected cash", true),
        new("pos.till.closing.counted_cash_entry", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Input, true, CashierPosPermissionDefinitionKind.Split, "Counted cash entry", true),
        new("pos.till.closing.difference", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Difference", true),
        new("pos.till.closing.balance_status", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Status, false, CashierPosPermissionDefinitionKind.Split, "Balance status", true),
        new("pos.till.closing.mismatch_reason", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Mismatch reason", true),
        new("pos.till.closing.notes", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Input, false, CashierPosPermissionDefinitionKind.Split, "Notes", true),
        new("pos.till.closing.summary", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Section, false, CashierPosPermissionDefinitionKind.Split, "Summary", true),
        new("pos.till.closing.expected_cash_summary", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Expected cash summary", true),
        new("pos.till.closing.counted_cash_summary", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Counted cash summary", true),
        new("pos.till.closing.difference_summary", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.SensitiveField, true, CashierPosPermissionDefinitionKind.Split, "Difference summary", true),
        new("pos.till.closing.status_summary", "pos.till.session.close", "Till", CashierPosPermissionSemanticType.Status, false, CashierPosPermissionDefinitionKind.Split, "Status summary", true),
    ];
}
