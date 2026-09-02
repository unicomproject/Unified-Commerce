using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class BusinessCapabilityCatalogService : IBusinessCapabilityCatalogService
{
    private static readonly Dictionary<string, (string ModuleCode, string ModuleName)> FeatureToModuleMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        [PlatformTenantFeatureCodes.TenantProfile] = ("TenantFoundation", "Tenant Foundation"),
        [PlatformTenantFeatureCodes.TenantSettings] = ("TenantFoundation", "Tenant Foundation"),
        [PlatformTenantFeatureCodes.OutletManagement] = ("OutletTillDevice", "Outlet & Till Device"),
        [PlatformTenantFeatureCodes.TillManagement] = ("OutletTillDevice", "Outlet & Till Device"),
        [PlatformTenantFeatureCodes.UserAccounts] = ("AccessControl", "Access Control & Identity"),
        [PlatformTenantFeatureCodes.RoleManagement] = ("AccessControl", "Access Control & Identity"),
        [PlatformTenantFeatureCodes.PermissionManagement] = ("AccessControl", "Access Control & Identity"),
        [PlatformTenantFeatureCodes.HardwareDeviceManagement] = ("HardwareCash", "Hardware & Peripheral Control"),
        [PlatformTenantFeatureCodes.PosCheckout] = ("POSOperations", "POS Core Checkout & Operations"),
        [PlatformTenantFeatureCodes.ProductCatalog] = ("CatalogProduct", "Product Catalog Management"),
        [PlatformTenantFeatureCodes.InventoryTracking] = ("Inventory", "Inventory & Stock Control"),
        [PlatformTenantFeatureCodes.OnlineStore] = ("Storefront", "Digital Storefront & E-Commerce"),
        [PlatformTenantFeatureCodes.SalesOrders] = ("Orders", "Sales & Order Fulfilment"),
        [PlatformTenantFeatureCodes.ClickCollect] = ("FulfilmentPickup", "Click & Collect Pickup"),
        [PlatformTenantFeatureCodes.SalesReports] = ("Reports", "Reporting & Analytics"),
        [PlatformTenantFeatureCodes.OfflineOperationSync] = ("OfflineSync", "Offline Queue & Sync Engine")
    };

    public Task<ApplicationResult<BusinessCapabilityMapResponseDto>> GetBusinessCapabilityMapAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var businessModules = new List<BusinessModuleMapDto>();
        var distinctCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var distinctFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var distinctPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var distinctTechModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var bmDef in BusinessCapabilityCatalog.Modules)
        {
            var capabilityDtos = new List<BusinessCapabilityMapDto>();
            var moduleFeaturesByCode = new Dictionary<string, TechnicalFeatureMapDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var capDef in bmDef.Capabilities)
            {
                distinctCapabilities.Add(capDef.Code);

                capabilityDtos.Add(new BusinessCapabilityMapDto(
                    capDef.Code,
                    capDef.Name,
                    capDef.Description,
                    capDef.CommercialClassification,
                    capDef.MappedTechnicalFeatureCodes));

                foreach (var featureCode in capDef.MappedTechnicalFeatureCodes)
                {
                    var canonicalCode = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(featureCode);
                    distinctFeatures.Add(canonicalCode);

                    if (!moduleFeaturesByCode.ContainsKey(canonicalCode))
                    {
                        var isPlanEligible = CommercialSubscriptionFeatureCatalog.IsCommercialSubscriptionSelectable(canonicalCode);
                        var featureGuid = GenerateDeterministicGuid(canonicalCode);
                        var rawPermissions = TenantAdminBootstrapPermissionCatalog.GetMappedPermissions(canonicalCode);

                        var permDtos = new List<PermissionMapDto>();
                        foreach (var permCode in rawPermissions)
                        {
                            distinctPermissions.Add(permCode);
                            permDtos.Add(new PermissionMapDto(
                                permCode,
                                FormatPermissionName(permCode),
                                DeriveActionType(permCode),
                                "TENANT",
                                true));
                        }

                        moduleFeaturesByCode[canonicalCode] = new TechnicalFeatureMapDto(
                            featureGuid,
                            canonicalCode,
                            FormatFeatureName(canonicalCode),
                            "TENANT",
                            true,
                            capDef.CommercialClassification,
                            isPlanEligible,
                            permDtos);
                    }
                }
            }

            var techModuleGroups = moduleFeaturesByCode.Values
                .GroupBy(f => FeatureToModuleMapping.TryGetValue(f.Code, out var modInfo) ? modInfo : ("TenantFoundation", "Tenant Foundation"))
                .Select(g =>
                {
                    distinctTechModules.Add(g.Key.Item1);
                    return new TechnicalModuleMapDto(
                        g.Key.Item1,
                        g.Key.Item2,
                        "TENANT",
                        g.ToList());
                })
                .ToList();

            businessModules.Add(new BusinessModuleMapDto(
                bmDef.Code,
                bmDef.Name,
                bmDef.Description,
                bmDef.DisplayOrder,
                bmDef.ReleaseCode,
                bmDef.CurrentR1Status,
                bmDef.CommercialState,
                capabilityDtos,
                techModuleGroups));
        }

        var summary = new BusinessCapabilityMapSummaryDto(
            BusinessModuleCount: businessModules.Count,
            BusinessCapabilityCount: distinctCapabilities.Count,
            TechnicalModuleCount: distinctTechModules.Count,
            TechnicalFeatureCount: distinctFeatures.Count,
            TenantPermissionCount: distinctPermissions.Count);

        var response = new BusinessCapabilityMapResponseDto(
            Release: BusinessCapabilityCatalog.ReleaseCode,
            CatalogVersion: "1.0.0",
            Summary: summary,
            BusinessModules: businessModules);

        return Task.FromResult(ApplicationResult<BusinessCapabilityMapResponseDto>.Success(response));
    }

    private static Guid GenerateDeterministicGuid(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
        return new Guid(bytes);
    }

    private static string FormatFeatureName(string featureCode)
    {
        return featureCode switch
        {
            PlatformTenantFeatureCodes.OutletManagement => "Outlet Management",
            PlatformTenantFeatureCodes.TillManagement => "Till Management",
            PlatformTenantFeatureCodes.UserAccounts => "User Accounts",
            PlatformTenantFeatureCodes.RoleManagement => "Role Management",
            PlatformTenantFeatureCodes.PermissionManagement => "Permission Management",
            PlatformTenantFeatureCodes.ProductCatalog => "Product Catalog",
            PlatformTenantFeatureCodes.InventoryTracking => "Inventory Tracking",
            PlatformTenantFeatureCodes.PosCheckout => "POS Checkout",
            PlatformTenantFeatureCodes.SalesOrders => "Sales Orders",
            PlatformTenantFeatureCodes.SalesReports => "Sales Reports",
            PlatformTenantFeatureCodes.OnlineStore => "Online Store",
            PlatformTenantFeatureCodes.ClickCollect => "Click & Collect",
            PlatformTenantFeatureCodes.HardwareDeviceManagement => "Hardware & Devices",
            PlatformTenantFeatureCodes.TenantSettings => "Tenant Settings",
            PlatformTenantFeatureCodes.TenantProfile => "Tenant Profile",
            PlatformTenantFeatureCodes.OfflineOperationSync => "Offline Operation & Sync",
            _ => featureCode
        };
    }

    private static string FormatPermissionName(string permissionCode)
    {
        var parts = permissionCode.Split('.');
        if (parts.Length >= 2)
        {
            var action = parts[^1].Replace("_", " ");
            var entity = parts[0].Replace("_", " ");
            return $"{char.ToUpper(action[0])}{action[1..]} {entity}";
        }
        return permissionCode;
    }

    private static string DeriveActionType(string permissionCode)
    {
        if (permissionCode.EndsWith(".view", StringComparison.OrdinalIgnoreCase) ||
            permissionCode.EndsWith(".read", StringComparison.OrdinalIgnoreCase))
        {
            return "READ";
        }
        if (permissionCode.EndsWith(".create", StringComparison.OrdinalIgnoreCase))
        {
            return "CREATE";
        }
        if (permissionCode.EndsWith(".update", StringComparison.OrdinalIgnoreCase) ||
            permissionCode.EndsWith(".manage", StringComparison.OrdinalIgnoreCase))
        {
            return "UPDATE";
        }
        if (permissionCode.EndsWith(".delete", StringComparison.OrdinalIgnoreCase))
        {
            return "DELETE";
        }
        return "EXECUTE";
    }
}
