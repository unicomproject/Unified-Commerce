namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

/// <summary>
/// Gates which top-level workspace (Tenant Admin vs. POS) a signed-in tenant user can
/// land in. Entitlement-independent — every active tenant role should carry the code
/// for whichever workspace(s) it operates in.
/// </summary>
public static class WorkspacePermissions
{
    public const string TenantAdminAccess = "workspace.tenant_admin.access";
    public const string PosAccess = "workspace.pos.access";
}
