namespace E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;

public interface IDomainVerificationProvider
{
    Task<DomainVerificationProviderResult> VerifyTxtRecordAsync(
        string domainName,
        string expectedTokenHash,
        CancellationToken cancellationToken);
}

public sealed record DomainVerificationProviderResult(
    DomainVerificationProviderStatus Status,
    string? FailureCode = null);

public enum DomainVerificationProviderStatus
{
    Verified,
    NotFound,
    Timeout,
    Unavailable,
    Failed
}
