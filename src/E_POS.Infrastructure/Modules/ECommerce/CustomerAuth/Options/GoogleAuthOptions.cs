namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; init; } = string.Empty;
}