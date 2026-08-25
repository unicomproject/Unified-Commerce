using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

/// <summary>
/// Maps canonical seeded permission codes to legacy or Flutter alias codes
/// returned in effective permission responses.
/// Product Setup: one-way legacy → catalog so authorization checks catalog.* only.
/// </summary>
public static class TenantPermissionAliases
{
    private static readonly IReadOnlyDictionary<string, string> CanonicalByLegacy =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenant.products.view"] = ProductConstants.ViewPermission,
            ["tenant.products.create"] = ProductConstants.CreatePermission,
            ["tenant.products.update"] = ProductConstants.UpdatePermission,
            ["tenant.products.delete"] = ProductConstants.DeletePermission,
            ["tax.classes.view"] = ProductConstants.TaxClassesViewPermission,
            ["tax.rates.view"] = ProductConstants.TaxRatesViewPermission,
        };

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
            [ProductConstants.ViewPermission] = ["tenant.products.view"],
            [ProductConstants.CreatePermission] = ["tenant.products.create"],
            [ProductConstants.UpdatePermission] = ["tenant.products.update"],
            [ProductConstants.DeletePermission] = ["tenant.products.delete"],
            [ProductConstants.TaxClassesViewPermission] = ["tax.classes.view"],
            [ProductConstants.TaxRatesViewPermission] = ["tax.rates.view"],
        };

    public static IReadOnlyList<string> Expand(IReadOnlyList<string> grantedCodes)
    {
        var expanded = new HashSet<string>(grantedCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var code in grantedCodes)
        {
            if (CanonicalByLegacy.TryGetValue(code, out var canonical))
            {
                expanded.Add(canonical);
            }

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
            .OrderBy(static code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
