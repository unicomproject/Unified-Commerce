using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;

/// <summary>
/// Builds a stable module/feature catalog from flat platform permission rows.
/// Grouping keys are derived from permission code segments (not subscription billing modules).
/// </summary>
public static class PlatformPermissionCatalogMapper
{
    private static readonly string[] ModuleSortOrder =
    [
        "dashboard",
        "tenants",
        "subscription_plans",
        "return_policy_templates",
        "modules",
        "features",
        "users",
        "audit",
        "settings",
        "billing",
        "integrations",
        "permissions",
        "roles"
    ];

    private static readonly Dictionary<string, string> ModuleNames = new(StringComparer.Ordinal)
    {
        ["dashboard"] = "Dashboard",
        ["tenants"] = "Tenants",
        ["subscription_plans"] = "Subscription Plans",
        ["return_policy_templates"] = "Return Policy Templates",
        ["modules"] = "Modules Catalog",
        ["features"] = "Features Catalog",
        ["users"] = "Platform Users",
        ["audit"] = "Audit",
        ["settings"] = "Settings",
        ["billing"] = "Billing",
        ["integrations"] = "Integrations",
        ["permissions"] = "Permissions",
        ["roles"] = "Roles"
    };

    private static readonly Dictionary<string, string> FeatureNames = new(StringComparer.Ordinal)
    {
        ["general"] = "General",
        ["entitlements"] = "Entitlements",
        ["permissions"] = "Role Permissions"
    };

    public static PlatformPermissionCatalogResponse BuildCatalog(
        IReadOnlyList<PlatformPermissionCatalogItem> permissions)
    {
        var sortedPermissions = FilterBusinessPermissions(permissions)
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .ToList();

        var modules = sortedPermissions
            .GroupBy(permission => GetModuleKey(permission.Code), StringComparer.Ordinal)
            .OrderBy(group => GetModuleSortIndex(group.Key))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => BuildModule(group.Key, group.ToList()))
            .ToList();

        return new PlatformPermissionCatalogResponse(modules);
    }

    public static PlatformPermissionFlatResponse BuildFlat(
        IReadOnlyList<PlatformPermissionCatalogItem> permissions)
    {
        var permissionDtos = FilterBusinessPermissions(permissions)
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .Select(ToPermissionDto)
            .ToList();

        return new PlatformPermissionFlatResponse(permissionDtos, permissionDtos.Count);
    }

    public static bool IsBootstrapPermission(string permissionCode)
    {
        return string.Equals(
            permissionCode,
            PlatformBootstrapPermissionCodes.AdminAccess,
            StringComparison.Ordinal);
    }

    private static PlatformPermissionModuleDto BuildModule(
        string moduleKey,
        IReadOnlyList<PlatformPermissionCatalogItem> permissions)
    {
        var features = permissions
            .GroupBy(permission => GetFeatureKey(permission.Code), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => BuildFeature(moduleKey, group.Key, group.ToList()))
            .ToList();

        return new PlatformPermissionModuleDto(
            moduleKey,
            GetModuleName(moduleKey),
            GetModuleDescription(moduleKey),
            features);
    }

    private static PlatformPermissionFeatureDto BuildFeature(
        string moduleKey,
        string featureKey,
        IReadOnlyList<PlatformPermissionCatalogItem> permissions)
    {
        var permissionDtos = permissions
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .Select(ToPermissionDto)
            .ToList();

        return new PlatformPermissionFeatureDto(
            featureKey,
            GetFeatureName(moduleKey, featureKey),
            GetFeatureDescription(moduleKey, featureKey),
            permissionDtos);
    }

    private static IEnumerable<PlatformPermissionCatalogItem> FilterBusinessPermissions(
        IReadOnlyList<PlatformPermissionCatalogItem> permissions)
    {
        var businessCodes = PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        return permissions.Where(permission =>
            businessCodes.Contains(permission.Code) &&
            !IsBootstrapPermission(permission.Code));
    }

    private static PlatformPermissionDto ToPermissionDto(PlatformPermissionCatalogItem permission)
    {
        return new PlatformPermissionDto(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Description,
            permission.Status,
            IsSystem: false,
            IsBootstrap: IsBootstrapPermission(permission.Code));
    }

    private static string GetModuleKey(string permissionCode)
    {
        var segments = permissionCode.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 ||
            !string.Equals(segments[0], "platform", StringComparison.Ordinal))
        {
            return "unknown";
        }

        return segments[1];
    }

    private static string GetFeatureKey(string permissionCode)
    {
        var segments = permissionCode.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 3)
        {
            return "general";
        }

        if (segments.Length >= 4)
        {
            return segments[2];
        }

        return "general";
    }

    private static int GetModuleSortIndex(string moduleKey)
    {
        for (var index = 0; index < ModuleSortOrder.Length; index++)
        {
            if (string.Equals(ModuleSortOrder[index], moduleKey, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return ModuleSortOrder.Length;
    }

    private static string GetModuleName(string moduleKey)
    {
        return ModuleNames.TryGetValue(moduleKey, out var name)
            ? name
            : HumanizeSegment(moduleKey);
    }

    private static string GetFeatureName(string moduleKey, string featureKey)
    {
        if (FeatureNames.TryGetValue(featureKey, out var name))
        {
            return name;
        }

        if (string.Equals(featureKey, "general", StringComparison.Ordinal))
        {
            return GetModuleName(moduleKey);
        }

        return HumanizeSegment(featureKey);
    }

    private static string? GetModuleDescription(string moduleKey)
    {
        return moduleKey switch
        {
            "dashboard" => "Platform dashboard visibility.",
            "tenants" => "Platform tenant lifecycle and entitlements.",
            "subscription_plans" => "Subscription plan catalog management.",
            "return_policy_templates" => "Platform return policy template administration.",
            "modules" => "Platform module catalog visibility.",
            "features" => "Platform feature catalog visibility.",
            "users" => "Platform user administration.",
            "audit" => "Platform audit log visibility.",
            "settings" => "Platform settings administration.",
            "billing" => "Platform billing administration.",
            "integrations" => "Platform integration management.",
            "permissions" => "Platform permission catalog visibility.",
            "roles" => "Platform role and role-permission administration.",
            _ => null
        };
    }

    private static string? GetFeatureDescription(string moduleKey, string featureKey)
    {
        if (string.Equals(featureKey, "entitlements", StringComparison.Ordinal))
        {
            return "Tenant feature entitlement updates.";
        }

        if (string.Equals(featureKey, "permissions", StringComparison.Ordinal) &&
            string.Equals(moduleKey, "roles", StringComparison.Ordinal))
        {
            return "Role permission assignment visibility and updates.";
        }

        return null;
    }

    private static string HumanizeSegment(string segment)
    {
        return string.Join(
            ' ',
            segment.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}


