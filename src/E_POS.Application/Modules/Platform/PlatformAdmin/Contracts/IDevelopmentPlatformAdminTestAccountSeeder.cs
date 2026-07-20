namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

/// <summary>
/// Development-only seeder for login-capable Platform Admin billing permission test accounts.
/// Must only be invoked when the host environment is Development.
/// </summary>
public interface IDevelopmentPlatformAdminTestAccountSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
