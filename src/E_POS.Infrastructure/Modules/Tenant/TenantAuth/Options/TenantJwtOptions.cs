namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;

public sealed class TenantJwtOptions
{
    public const string SectionName = "TenantJwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
