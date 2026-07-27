namespace E_POS.Infrastructure.Integrations.Email;

/// <summary>
/// Azure Communication Services Email options.
/// Secrets (ConnectionString) must come from user-secrets / Key Vault / environment — never commit real values.
/// </summary>
public sealed class AzureCommunicationEmailOptions
{
    public const string SectionName = "AzureCommunicationEmail";

    /// <summary>ACS connection string. Preferred for local development via user-secrets.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>ACS endpoint URI. Used with DefaultAzureCredential in production.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Verified ACS sender address (required when ACS is configured).</summary>
    public string SenderAddress { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = "OneVerz";

    /// <summary>
    /// When ACS is not configured, allow returning admin_secure_link for local development only.
    /// Must remain false in production configuration.
    /// </summary>
    public bool AllowAdminSecureLinkFallback { get; set; }
}
