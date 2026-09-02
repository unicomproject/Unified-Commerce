namespace E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;

public interface ICertificateProvisioningProvider
{
    Task<CertificateProvisioningProviderResult> RequestAsync(
        Guid tenantId,
        Guid domainId,
        string domainName,
        CancellationToken cancellationToken);

    Task<CertificateProvisioningProviderResult> GetStatusAsync(
        Guid tenantId,
        Guid domainId,
        string domainName,
        CancellationToken cancellationToken);
}

public sealed record CertificateProvisioningProviderResult(
    CertificateProvisioningProviderStatus Status,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? ExpiresAt = null,
    string? FailureCode = null);

public enum CertificateProvisioningProviderStatus
{
    NotRequested,
    Provisioning,
    Active,
    Failed,
    Timeout,
    Unavailable
}
