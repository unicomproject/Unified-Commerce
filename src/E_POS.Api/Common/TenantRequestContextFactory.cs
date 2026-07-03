using System.Security.Claims;
using System.Text.Json;
using E_POS.Application.Common.Models;

namespace E_POS.Api.Common;

public sealed class TenantRequestContextFactory : ITenantRequestContextFactory
{
    public bool TryCreate(ClaimsPrincipal principal, out TenantRequestContext context)
    {
        var tenantUserIdValue = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = principal.FindFirstValue("tenant_id");
        var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out var tenantUserId);
        var hasTenantId = Guid.TryParse(tenantIdValue, out var tenantId);

        context = new TenantRequestContext(tenantId, tenantUserId, GetPermissionClaims(principal));
        return hasTenantUserId && hasTenantId;
    }

    private static IReadOnlyCollection<string> GetPermissionClaims(ClaimsPrincipal principal)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in principal.FindAll("permissions"))
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            if (claim.Value.TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                AddJsonPermissionClaims(claim.Value, permissions);
                continue;
            }

            permissions.Add(claim.Value);
        }

        return permissions;
    }

    private static void AddJsonPermissionClaims(string claimValue, HashSet<string> permissions)
    {
        try
        {
            foreach (var permission in JsonSerializer.Deserialize<string[]>(claimValue) ?? [])
            {
                if (!string.IsNullOrWhiteSpace(permission))
                {
                    permissions.Add(permission);
                }
            }
        }
        catch (JsonException)
        {
            // Malformed optional permission claims must not grant access or break tenant context creation.
        }
    }
}