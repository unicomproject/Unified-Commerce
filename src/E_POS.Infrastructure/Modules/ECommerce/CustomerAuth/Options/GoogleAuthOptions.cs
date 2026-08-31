namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Max time allowed for Google certificate fetch + ID token validation.
    /// Prevents storefront Google login from hanging until the client 30s timeout.
    /// </summary>
    public int VerificationTimeoutSeconds { get; init; } = 8;
}