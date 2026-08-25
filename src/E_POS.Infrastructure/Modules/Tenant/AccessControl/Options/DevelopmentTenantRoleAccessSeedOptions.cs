namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Options;

/// <summary>
/// Development-only credentials for fixed Role Access test identities.
/// Bind from user-secrets or environment variables; passwords must never be committed.
/// </summary>
public sealed class DevelopmentTenantRoleAccessSeedOptions
{
    public const string SectionName = "DevelopmentSeed:TenantRoleAccess";

    public DevelopmentTenantRoleAccessAccountOptions TenantAdmin { get; set; } = new();

    public DevelopmentTenantRoleAccessAccountOptions Cashier { get; set; } = new();
}

public sealed class DevelopmentTenantRoleAccessAccountOptions
{
    public string? Password { get; set; }

    public bool HasPassword => !string.IsNullOrWhiteSpace(Password);
}
