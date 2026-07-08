using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

/// <summary>
/// Maps canonical seeded permission codes to legacy or Flutter alias codes
/// returned in effective permission responses.
/// </summary>
public static class TenantPermissionAliases
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AliasesByCanonical =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [PosPermissions.Home.View] = [PosPermissions.Home.ViewDashboard],
            [SalesPermissions.Sale.Create] = [PosPermissions.NewSale.View],
            [SalesPermissions.Sale.LegacyCreate] =
            [
                SalesPermissions.Sale.Create,
                PosPermissions.NewSale.View,
            ],
            [TillConstants.ManagePermission] = ["tenant.till.manage"],
            [SalesPermissions.Park.Create] =
            [
                SalesPermissions.Park.LegacyPark,
                SalesPermissions.Park.LegacyRecall,
                SalesPermissions.Park.LegacyView,
            ],
        };

    public static IReadOnlyList<string> Expand(IReadOnlyList<string> grantedCodes)
    {
        var expanded = new HashSet<string>(grantedCodes, StringComparer.Ordinal);

        foreach (var code in grantedCodes)
        {
            if (!AliasesByCanonical.TryGetValue(code, out var aliases))
            {
                continue;
            }

            foreach (var alias in aliases)
            {
                expanded.Add(alias);
            }
        }

        return expanded
            .OrderBy(static code => code, StringComparer.Ordinal)
            .ToList();
    }
}
