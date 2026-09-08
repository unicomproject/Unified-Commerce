namespace E_POS.Infrastructure.Modules.Tenant.OnlineStoreSetup.Services;

public sealed class OnlineStoreSetupOptions
{
    public const string SectionName = "OnlineStoreSetup";

    public string HostedDomain { get; set; } = "oneverz.shop";
}

public sealed class DomainVerificationOptions
{
    public const string SectionName = "OnlineStoreDomainVerification";

    public bool Enabled { get; set; }
    public string QueryEndpoint { get; set; } = string.Empty;
    public string RecordNamePrefix { get; set; } = "_oneverz-verification";
    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class CertificateProvisioningOptions
{
    public const string SectionName = "OnlineStoreCertificateProvisioning";

    public bool Enabled { get; set; }
    public string ProvisionEndpoint { get; set; } = string.Empty;
    public string StatusEndpoint { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 20;
}
