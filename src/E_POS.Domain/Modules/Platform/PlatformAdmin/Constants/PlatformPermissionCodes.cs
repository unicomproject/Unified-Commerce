namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

/// <summary>
/// TM-EPOS Option A platform permission codes (plural domain segments).
/// Authoritative assignable catalogue for seeds, role assignment, and authz.
/// Angular <c>permission-keys.ts</c> holds a guarded UI subset (31 codes); the five
/// <c>return_policy_templates.*</c> codes are backend/API-only until Platform Admin UI ships.
/// </summary>
public static class PlatformPermissionCodes
{
    public const string DashboardView = "platform.dashboard.view";

    public const string TenantsView = "platform.tenants.view";
    public const string TenantsCreate = "platform.tenants.create";
    public const string TenantsUpdate = "platform.tenants.update";
    public const string TenantsActivate = "platform.tenants.activate";
    public const string TenantsSuspend = "platform.tenants.suspend";
    public const string TenantsEntitlementsUpdate = "platform.tenants.entitlements.update";

    public const string SubscriptionPlansView = "platform.subscription_plans.view";
    public const string SubscriptionPlansCreate = "platform.subscription_plans.create";
    public const string SubscriptionPlansEdit = "platform.subscription_plans.edit";
    public const string SubscriptionPlansDuplicate = "platform.subscription_plans.duplicate";
    public const string SubscriptionPlansArchive = "platform.subscription_plans.archive";
    public const string SubscriptionPlansDelete = "platform.subscription_plans.delete";

    public const string ReturnPolicyTemplatesView = "platform.return_policy_templates.view";
    public const string ReturnPolicyTemplatesCreate = "platform.return_policy_templates.create";
    public const string ReturnPolicyTemplatesUpdate = "platform.return_policy_templates.update";
    public const string ReturnPolicyTemplatesDelete = "platform.return_policy_templates.delete";
    public const string ReturnPolicyTemplatesManage = "platform.return_policy_templates.manage";

    public const string ModulesView = "platform.modules.view";
    public const string FeaturesView = "platform.features.view";

    public const string UsersView = "platform.users.view";
    public const string UsersCreate = "platform.users.create";
    public const string UsersUpdate = "platform.users.update";
    public const string UsersRolesAssign = "platform.users.roles.assign";

    public const string AuditView = "platform.audit.view";

    public const string SettingsView = "platform.settings.view";
    public const string SettingsUpdate = "platform.settings.update";

    public const string BillingView = "platform.billing.view";
    public const string BillingManage = "platform.billing.manage";

    public const string IntegrationsManage = "platform.integrations.manage";

    public const string PermissionsView = "platform.permissions.view";

    public const string RolesView = "platform.roles.view";
    public const string RolesCreate = "platform.roles.create";
    public const string RolesUpdate = "platform.roles.update";
    public const string RolePermissionsView = "platform.roles.permissions.view";
    public const string RolePermissionsUpdate = "platform.roles.permissions.update";

    public static IReadOnlyList<string> All { get; } =
    [
        DashboardView,
        TenantsView,
        TenantsCreate,
        TenantsUpdate,
        TenantsActivate,
        TenantsSuspend,
        TenantsEntitlementsUpdate,
        SubscriptionPlansView,
        SubscriptionPlansCreate,
        SubscriptionPlansEdit,
        SubscriptionPlansDuplicate,
        SubscriptionPlansArchive,
        SubscriptionPlansDelete,
        ReturnPolicyTemplatesView,
        ReturnPolicyTemplatesCreate,
        ReturnPolicyTemplatesUpdate,
        ReturnPolicyTemplatesDelete,
        ReturnPolicyTemplatesManage,
        ModulesView,
        FeaturesView,
        UsersView,
        UsersCreate,
        UsersUpdate,
        UsersRolesAssign,
        AuditView,
        SettingsView,
        SettingsUpdate,
        BillingView,
        BillingManage,
        IntegrationsManage,
        PermissionsView,
        RolesView,
        RolesCreate,
        RolesUpdate,
        RolePermissionsView,
        RolePermissionsUpdate
    ];
}

