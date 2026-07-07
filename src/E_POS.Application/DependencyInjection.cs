using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Application.Modules.AuthSecurity.Services;
using E_POS.Application.Modules.TenantAdministration.Contracts;
using E_POS.Application.Modules.TenantAdministration.Services;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Services;
using E_POS.Application.Modules.CatalogProduct.Validators;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Services;
using E_POS.Application.Modules.OutletTillDevice.Validators;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Application.Modules.PlatformAdministration.Validators;
using E_POS.Application.Modules.SubscriptionBilling.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Services;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Services;
using E_POS.Application.Modules.PricingTax.Validators;
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
        services.AddScoped<IPlatformAuditLogService, PlatformAuditLogService>();
        services.AddScoped<IPlatformSubscriptionPlanService, PlatformSubscriptionPlanService>();
        services.AddScoped<ITenantAuthService, TenantAuthService>();
        services.AddScoped<ITenantAdminContextService, TenantAdminContextService>();
        services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
        services.AddScoped<IDepartmentRequestValidator, DepartmentRequestValidator>();
        services.AddScoped<ICategoryRequestValidator, CategoryRequestValidator>();
        services.AddScoped<IBrandRequestValidator, BrandRequestValidator>();
        services.AddScoped<IProductRequestValidator, ProductRequestValidator>();
        services.AddScoped<ICollectionRequestValidator, CollectionRequestValidator>();
        services.AddScoped<IReturnPolicyTemplateRequestValidator, ReturnPolicyTemplateRequestValidator>();
        services.AddScoped<IReturnPolicyRequestValidator, ReturnPolicyRequestValidator>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IReturnPolicyTemplateService, ReturnPolicyTemplateService>();
        services.AddScoped<IReturnPolicyService, ReturnPolicyService>();
        services.AddScoped<IOutletRequestValidator, OutletRequestValidator>();
        services.AddScoped<ITillRequestValidator, TillRequestValidator>();
        services.AddScoped<IPosDeviceRequestValidator, PosDeviceRequestValidator>();
        services.AddScoped<IOutletService, OutletService>();
        services.AddScoped<ITillService, TillService>();
        services.AddScoped<IPosDeviceService, PosDeviceService>();
        services.AddScoped<ITillDeviceAssignmentService, TillDeviceAssignmentService>();
        services.AddScoped<IPriceListRequestValidator, PriceListRequestValidator>();
        services.AddScoped<IPriceListService, PriceListService>();
        services.AddScoped<IPriceListItemsRequestValidator, PriceListItemsRequestValidator>();
        services.AddScoped<IPriceListItemsService, PriceListItemsService>();
        services.AddScoped<ITaxSetupRequestValidator, TaxSetupRequestValidator>();
        services.AddScoped<IProductTaxAssignmentRequestValidator, ProductTaxAssignmentRequestValidator>();
        services.AddScoped<ITaxSetupService, TaxSetupService>();
        services.AddScoped<IProductTaxAssignmentService, ProductTaxAssignmentService>();

        return services;
    }
}
