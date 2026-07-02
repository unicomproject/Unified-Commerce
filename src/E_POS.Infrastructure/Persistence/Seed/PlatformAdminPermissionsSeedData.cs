using E_POS.Domain.Modules.PlatformAdministration.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

public sealed record PlatformPermissionSeedDefinition(
    Guid Id,
    string PermissionCode,
    string Name,
    string Description);

public static class PlatformAdminSeedConstants
{
    public static readonly Guid SuperAdministratorRoleId = Guid.Parse("51000000-0000-0000-0000-000000000001");
    public static readonly Guid DevelopmentPlatformUserId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid DevelopmentPlatformUserRoleId = Guid.Parse("66000000-0000-0000-0000-000000000001");
    public const string DevelopmentPlatformUserEmail = "POSUNIQUE001@GMAIL.COM";
}

public static class PlatformAdminPermissionsSeedData
{
    public static IReadOnlyList<PlatformPermissionSeedDefinition> Definitions { get; } =
    [
        new(Guid.Parse("62000000-0000-0000-0000-000000000001"), PlatformPermissionCodes.DashboardView, "View Platform Dashboard", "View platform dashboard summaries."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000002"), PlatformPermissionCodes.TenantsView, "View Tenants", "View platform tenants."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000003"), PlatformPermissionCodes.TenantsCreate, "Create Tenants", "Create platform tenants."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000004"), PlatformPermissionCodes.TenantsUpdate, "Update Tenants", "Update platform tenants."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000005"), PlatformPermissionCodes.TenantsActivate, "Activate Tenants", "Activate platform tenants."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000006"), PlatformPermissionCodes.TenantsSuspend, "Suspend Tenants", "Suspend platform tenants."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000007"), PlatformPermissionCodes.TenantsEntitlementsUpdate, "Update Tenant Entitlements", "Update tenant feature entitlements."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000008"), PlatformPermissionCodes.SubscriptionPlansView, "View Subscription Plans", "View platform subscription plans."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000009"), PlatformPermissionCodes.SubscriptionPlansCreate, "Create Subscription Plans", "Create platform subscription plans."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000010"), PlatformPermissionCodes.SubscriptionPlansEdit, "Edit Subscription Plans", "Edit platform subscription plans."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000011"), PlatformPermissionCodes.SubscriptionPlansDuplicate, "Duplicate Subscription Plans", "Duplicate platform subscription plans."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000012"), PlatformPermissionCodes.SubscriptionPlansArchive, "Archive Subscription Plans", "Archive platform subscription plans."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000013"), PlatformPermissionCodes.SubscriptionPlansDelete, "Delete Subscription Plans", "Delete platform subscription plans."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000014"), PlatformPermissionCodes.ModulesView, "View Modules Catalog", "View platform modules catalog."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000015"), PlatformPermissionCodes.FeaturesView, "View Features Catalog", "View platform features catalog."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000016"), PlatformPermissionCodes.UsersView, "View Platform Users", "View platform users."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000017"), PlatformPermissionCodes.UsersCreate, "Create Platform Users", "Create platform users."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000018"), PlatformPermissionCodes.UsersUpdate, "Update Platform Users", "Update platform users."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000019"), PlatformPermissionCodes.UsersRolesAssign, "Assign Platform User Roles", "Assign platform roles to platform users."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000020"), PlatformPermissionCodes.AuditView, "View Platform Audit Logs", "View platform audit logs."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000021"), PlatformPermissionCodes.SettingsView, "View Platform Settings", "View platform settings."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000022"), PlatformPermissionCodes.SettingsUpdate, "Update Platform Settings", "Update platform settings."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000023"), PlatformPermissionCodes.BillingView, "View Platform Billing", "View platform billing records."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000024"), PlatformPermissionCodes.BillingManage, "Manage Platform Billing", "Manage platform billing and payment links."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000025"), PlatformPermissionCodes.IntegrationsManage, "Manage Platform Integrations", "Manage platform integrations."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000026"), PlatformPermissionCodes.PermissionsView, "View Permission Catalog", "View platform permission catalog."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000027"), PlatformPermissionCodes.RolesView, "View Platform Roles", "View platform roles."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000028"), PlatformPermissionCodes.RolesCreate, "Create Platform Roles", "Create platform roles."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000029"), PlatformPermissionCodes.RolesUpdate, "Update Platform Roles", "Update platform roles."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000030"), PlatformPermissionCodes.RolePermissionsView, "View Platform Role Permissions", "View platform role permissions."),
        new(Guid.Parse("62000000-0000-0000-0000-000000000031"), PlatformPermissionCodes.RolePermissionsUpdate, "Update Platform Role Permissions", "Update platform role permissions.")
    ];

    public static string UpSql => $"""
        INSERT INTO platform_permissions (id, permission_code, name, description, status, created_at, updated_at)
        VALUES
            {string.Join(",\n            ", Definitions.Select(FormatPermissionInsertRow))}
        ON CONFLICT (permission_code) DO UPDATE
        SET name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO platform_roles (id, role_code, name, description, status, created_at, updated_at)
        VALUES (
            '{PlatformAdminSeedConstants.SuperAdministratorRoleId}',
            '{PlatformRoleCodes.SuperAdministrator}',
            'Super Administrator',
            'Full TM-EPOS platform administration access.',
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (role_code) DO UPDATE
        SET name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO platform_role_permissions (id, platform_role_id, platform_permission_id, description, created_at, updated_at)
        SELECT
            ('67000000-0000-0000-0000-0000000000' || LPAD(ROW_NUMBER() OVER (ORDER BY permission.id)::text, 2, '0'))::uuid,
            role.id,
            permission.id,
            'TM-EPOS super administrator permission seed.',
            now(),
            now()
        FROM platform_roles role
        CROSS JOIN platform_permissions permission
        WHERE role.role_code = '{PlatformRoleCodes.SuperAdministrator}'
          AND permission.permission_code IN ({FormatPermissionCodeInList()})
        ON CONFLICT (platform_role_id, platform_permission_id) DO NOTHING;

        INSERT INTO platform_user_roles (id, platform_user_id, platform_role_id, description, created_at, updated_at)
        SELECT
            '{PlatformAdminSeedConstants.DevelopmentPlatformUserRoleId}',
            platform_users.id,
            platform_roles.id,
            'Assign TM-EPOS super administrator role to development platform admin.',
            now(),
            now()
        FROM platform_users
        CROSS JOIN platform_roles
        WHERE platform_users.normalized_email = '{PlatformAdminSeedConstants.DevelopmentPlatformUserEmail}'
          AND platform_roles.role_code = '{PlatformRoleCodes.SuperAdministrator}'
        ON CONFLICT (platform_user_id, platform_role_id) DO NOTHING;
        """;

    public static string DownSql => $"""
        DELETE FROM platform_user_roles
        WHERE id = '{PlatformAdminSeedConstants.DevelopmentPlatformUserRoleId}'
           OR (
                platform_user_id = '{PlatformAdminSeedConstants.DevelopmentPlatformUserId}'
            AND platform_role_id = '{PlatformAdminSeedConstants.SuperAdministratorRoleId}'
           );

        DELETE FROM platform_role_permissions
        WHERE platform_role_id = '{PlatformAdminSeedConstants.SuperAdministratorRoleId}'
          AND platform_permission_id IN (
              SELECT id FROM platform_permissions
              WHERE permission_code IN ({FormatPermissionCodeInList()}));

        DELETE FROM platform_roles
        WHERE id = '{PlatformAdminSeedConstants.SuperAdministratorRoleId}'
          AND role_code = '{PlatformRoleCodes.SuperAdministrator}';

        DELETE FROM platform_permissions
        WHERE permission_code IN ({FormatPermissionCodeInList()});
        """;

    private static string FormatPermissionInsertRow(PlatformPermissionSeedDefinition definition)
    {
        return $"('{definition.Id}', '{definition.PermissionCode}', '{definition.Name}', '{definition.Description}', 'ACTIVE', now(), now())";
    }

    private static string FormatPermissionCodeInList()
    {
        return string.Join(", ", Definitions.Select(definition => $"'{definition.PermissionCode}'"));
    }
}
