#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

internal static class PreAuthPermissions
{
    internal static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        new("pre_auth.login.screen.view", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Login screen container", false),
        new("pre_auth.login.branding.view", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Login branding", false),
        new("pre_auth.login.email.input", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Email input", false),
        new("pre_auth.login.password.input", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Password input", false),
        new("pre_auth.login.password_visibility.toggle", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Password visibility", false),
        new("pre_auth.login.submit.execute", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Login submit", false),
        new("pre_auth.login.validation.message", null, "PreAuth", CashierPosPermissionSemanticType.PreAuthConfiguration, false, CashierPosPermissionDefinitionKind.PreAuth, "Validation message", false),
    ];
}
