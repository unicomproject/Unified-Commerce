using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Infrastructure.Common;
using E_POS.Infrastructure.Modules.PlatformAdministration.Options;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
using E_POS.Infrastructure.Modules.PlatformAdministration.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace E_POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformJwtOptions>(configuration.GetSection(PlatformJwtOptions.SectionName));

        services.AddDbContext<EPosDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPlatformAuthRepository, PlatformAuthRepository>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITokenHashService, TokenHashService>();

        return services;
    }
}
