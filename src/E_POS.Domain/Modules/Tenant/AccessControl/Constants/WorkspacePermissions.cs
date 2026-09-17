namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

/// <summary>Workspace entry grants; never imply operational permissions.</summary>
public static class WorkspacePermissions
{
    public const string PosAccess = "workspace.pos.access";
    public const string TenantAdminAccess = "workspace.tenant_admin.access";
}
