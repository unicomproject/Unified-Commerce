namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

/// <summary>
/// Reconciles fixed, development-only tenant accounts used for authenticated Role Access verification.
/// </summary>
public interface IDevelopmentTenantRoleAccessTestAccountSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
