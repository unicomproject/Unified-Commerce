using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using Microsoft.EntityFrameworkCore;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;

namespace E_POS.Api.Extensions;

/// <summary>
/// Startup boundary for Development-only Platform Admin test-account seeding.
/// The environment guard lives here so Production/Staging never resolve or run the seeder.
/// </summary>
public static class DevelopmentPlatformAdminTestAccountSeedHost
{
    public static bool ShouldSeed(IHostEnvironment environment) =>
        environment.IsDevelopment();

    public static async Task RunIfDevelopmentAsync(
        WebApplication app,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldSeed(app.Environment) ||
            app.Configuration.GetValue<bool>("DevelopmentSeed:PlatformAdmin:Disabled"))
        {
            return;
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
            await db.Database.ExecuteSqlRawAsync(DevelopmentMerchandiseCatalogSeedData.UpSql, cancellationToken);
            
            var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentPlatformAdminTestAccountSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(DevelopmentPlatformAdminTestAccountSeedHost));
            logger.LogError(
                ex,
                "Development Platform Admin test-account seeding failed. Application startup continues; configure secrets and required roles, then restart.");
        }
    }
}
