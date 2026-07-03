using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Application.Modules.AuthSecurity.Services;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Services;
using E_POS.Application.Modules.OutletTillDevice.Validators;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Application.Modules.PlatformAdministration.Validators;
using E_POS.Application.Modules.SubscriptionBilling.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace E_POS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlatformAuthRequestValidator, PlatformAuthRequestValidator>();
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();
        services.AddScoped<IPlatformPermissionChecker, PlatformPermissionChecker>();
        services.AddScoped<IPlatformDashboardService, PlatformDashboardService>();
        services.AddScoped<IPlatformTenantService, PlatformTenantService>();
        services.AddScoped<IPlatformPermissionCatalogService, PlatformPermissionCatalogService>();
        services.AddScoped<IPlatformModulesCatalogService, PlatformModulesCatalogService>();
        services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();
        services.AddScoped<IPlatformRoleService, PlatformRoleService>();
        services.AddScoped<IPlatformUserService, PlatformUserService>();
        services.AddScoped<IPlatformSubscriptionPlanService, PlatformSubscriptionPlanService>();
        services.AddScoped<ITenantAuthService, TenantAuthService>();
        services.AddScoped<IOutletRequestValidator, OutletRequestValidator>();
        services.AddScoped<ITillRequestValidator, TillRequestValidator>();
        services.AddScoped<IPosDeviceRequestValidator, PosDeviceRequestValidator>();
        services.AddScoped<IOutletService, OutletService>();
        services.AddScoped<ITillService, TillService>();
        services.AddScoped<IPosDeviceService, PosDeviceService>();
        services.AddScoped<ITillDeviceAssignmentService, TillDeviceAssignmentService>();

        return services;
    }
}
