namespace E_POS.Infrastructure.Modules.PlatformAdministration.Options;

public sealed class PlatformJwtOptions
{
    public const string SectionName = "PlatformJwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
