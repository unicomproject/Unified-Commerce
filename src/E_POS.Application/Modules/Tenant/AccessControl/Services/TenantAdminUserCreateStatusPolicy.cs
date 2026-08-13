using E_POS.Domain.Modules.Tenant.AccessControl.Constants;

namespace E_POS.Application.Modules.Tenant.AccessControl.Services;

public static class TenantAdminUserCreateStatusPolicy
{
    public static IReadOnlyList<string> SupportedStatuses { get; } =
    [
        TenantUserConstants.StatusInvited,
        TenantUserConstants.StatusInactive,
    ];

    public static string? Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToUpperInvariant();
        return SupportedStatuses.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : null;
    }
}
