using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Application.Modules.AuthSecurity.Dtos;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Infrastructure.Common;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.AuthSecurity.Options;
using E_POS.Infrastructure.Modules.AuthSecurity.Repositories;
using E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.PlatformAdministration.Options;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
using E_POS.Infrastructure.Modules.SubscriptionBilling.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformJwtOptions>(configuration.GetSection(PlatformJwtOptions.SectionName));
        services.Configure<TenantJwtOptions>(configuration.GetSection(TenantJwtOptions.SectionName));

        services.AddDbContext<EPosDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
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
        services.AddScoped<IPlatformRoleRepository, PlatformRoleRepository>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IPlatformSubscriptionPlanRepository, PlatformSubscriptionPlanRepository>();
        services.AddScoped<ITenantAuthRepository, TenantAuthRepository>();
        services.AddScoped<IOutletRepository, OutletRepository>();
        services.AddScoped<ITillRepository, TillRepository>();
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