namespace E_POS.Application.Common.Models;

public sealed record TenantRequestContext(Guid TenantId, Guid UserId, IReadOnlyCollection<string> Permissions)
{
    public bool HasPermission(string permissionCode)
    {
        return Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }
}