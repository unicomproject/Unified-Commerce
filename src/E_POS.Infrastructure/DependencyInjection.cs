using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Infrastructure.Common;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Infrastructure.Modules.Tenant.PricingTax.Repositories;


namespace E_POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformJwtOptions>(configuration.GetSection(PlatformJwtOptions.SectionName));
        services.Configure<TenantJwtOptions>(configuration.GetSection(TenantJwtOptions.SectionName));

        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<EPosDbContext>(options =>
        {
            options.UseNpgsql(dataSource);
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IJwtTokenFactory, JwtTokenFactory>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ITokenHashService, TokenHashService>();
        services.AddScoped<IAuthSessionValidator, AuthSessionValidator>();
        services.AddScoped<IPlatformPermissionRepository, PlatformPermissionRepository>();
        services.AddScoped<IPlatformAuthRepository, PlatformAuthRepository>();
        services.AddScoped<IPlatformDashboardRepository, PlatformDashboardRepository>();
        services.AddScoped<IPlatformTenantRepository, PlatformTenantRepository>();
        services.AddScoped<IPlatformPermissionCatalogRepository, PlatformPermissionCatalogRepository>();
        services.AddScoped<IPlatformModulesCatalogRepository, PlatformModulesCatalogRepository>();
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();
        services.AddScoped<IPlatformRoleRepository, PlatformRoleRepository>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IPlatformAuditLogRepository, PlatformAuditLogRepository>();
        services.AddScoped<IPlatformSubscriptionPlanRepository, PlatformSubscriptionPlanRepository>();
        services.AddScoped<ITenantAuthRepository, TenantAuthRepository>();
        services.AddScoped<ITenantAdminContextRepository, TenantAdminContextRepository>();
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IReturnPolicyTemplateRepository, ReturnPolicyTemplateRepository>();
        services.AddScoped<IReturnPolicyRepository, ReturnPolicyRepository>();
        services.AddScoped<ICodeSequenceRepository, CodeSequenceRepository>();
        services.AddScoped<IOutletRepository, OutletRepository>();
        services.AddScoped<ITillRepository, TillRepository>();
        services.AddScoped<IPosDeviceRepository, PosDeviceRepository>();
        services.AddScoped<ITillDeviceAssignmentRepository, TillDeviceAssignmentRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<IPriceListItemsRepository, PriceListItemsRepository>();
        services.AddScoped<ITaxSetupRepository, TaxSetupRepository>();
        services.AddScoped<IProductTaxAssignmentRepository, ProductTaxAssignmentRepository>();
        services.AddScoped<ITenantLookupRepository, TenantLookupRepository>();
        services.AddScoped(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<PlatformJwtOptions>>().Value;
            return new PlatformJwtSettings(options.Issuer, options.Audience, options.SigningKey, options.AccessTokenMinutes, options.RefreshTokenDays);
        });
        services.AddScoped(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<TenantJwtOptions>>().Value;
            return new TenantJwtSettings(options.Issuer, options.Audience, options.SigningKey, options.AccessTokenMinutes, options.RefreshTokenDays);
        });

        return services;
    }
}

