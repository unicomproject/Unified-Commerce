using E_POS.Application.Modules.Tenant.AccessControl.Contracts;

namespace E_POS.Api.Extensions;

/// <summary>
/// Development-only startup boundary for authenticated Tenant Role Access test accounts.
/// </summary>
public static class DevelopmentTenantRoleAccessTestAccountSeedHost
{
    public static bool ShouldSeed(IHostEnvironment environment) => environment.IsDevelopment();

    public static async Task RunIfDevelopmentAsync(
        WebApplication app,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldSeed(app.Environment) ||
            app.Configuration.GetValue<bool>("DevelopmentSeed:TenantRoleAccess:Disabled"))
        {
            return;
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentTenantRoleAccessTestAccountSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(DevelopmentTenantRoleAccessTestAccountSeedHost));
            logger.LogError(
                exception,
                "Development Tenant Role Access test-account seeding failed. Application startup continues.");
        }
    }
}
