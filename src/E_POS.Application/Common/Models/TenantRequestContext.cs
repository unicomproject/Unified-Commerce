using E_POS.Domain.Modules.Tenant.AccessControl.Constants;

namespace E_POS.Application.Common.Models;

public sealed record TenantRequestContext(Guid TenantId, Guid UserId, IReadOnlyCollection<string> Permissions)
{
    public bool HasPermission(string permissionCode)
    {
        var expanded = TenantPermissionAliases.Expand(
            Permissions as IReadOnlyList<string> ?? Permissions.ToList());
        return expanded.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }
}
