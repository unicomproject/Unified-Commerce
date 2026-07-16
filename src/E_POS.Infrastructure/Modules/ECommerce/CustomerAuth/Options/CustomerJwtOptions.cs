namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;

public sealed class CustomerJwtOptions
{
    public const string SectionName = "CustomerJwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
