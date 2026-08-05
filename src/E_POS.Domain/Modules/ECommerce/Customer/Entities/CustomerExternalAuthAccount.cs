using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerExternalAuthAccount : AuditableEntity
{
    public const string GoogleProviderCode = "GOOGLE";

    public Guid TenantId { get; protected set; }
    public Guid CustomerAuthAccountId { get; protected set; }
    public string ProviderCode { get; protected set; } = string.Empty;
    public string ProviderSubject { get; protected set; } = string.Empty;
    public string? ProviderEmail { get; protected set; }
    public bool ProviderEmailVerified { get; protected set; }
    public DateTimeOffset LinkedAt { get; protected set; }
    public DateTimeOffset? LastLoginAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    protected CustomerExternalAuthAccount() { }

    public static CustomerExternalAuthAccount Create(
        Guid id,
        Guid tenantId,
        Guid customerAuthAccountId,
        string providerCode,
        string providerSubject,
        string? providerEmail,
        bool providerEmailVerified,
        DateTimeOffset now)
    {
        return new CustomerExternalAuthAccount
        {
            Id = id,
            TenantId = tenantId,
            CustomerAuthAccountId = customerAuthAccountId,
            ProviderCode = providerCode.Trim().ToUpperInvariant(),
            ProviderSubject = providerSubject.Trim(),
            ProviderEmail = string.IsNullOrWhiteSpace(providerEmail) ? null : providerEmail.Trim(),
            ProviderEmailVerified = providerEmailVerified,
            LinkedAt = now,
            Status = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RecordSuccessfulLogin(
        string? providerEmail,
        bool providerEmailVerified,
        DateTimeOffset now)
    {
        ProviderEmail = string.IsNullOrWhiteSpace(providerEmail) ? ProviderEmail : providerEmail.Trim();
        ProviderEmailVerified = providerEmailVerified;
        LastLoginAt = now;
        if (string.Equals(Status, "DISABLED", StringComparison.OrdinalIgnoreCase))
            Status = "ACTIVE";
        UpdatedAt = now;
    }

    public void Disable(DateTimeOffset now)
    {
        if (string.Equals(Status, "DELETED", StringComparison.OrdinalIgnoreCase))
            return;

        Status = "DISABLED";
        UpdatedAt = now;
    }

    public void MarkDeleted(DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedAt = now;
    }
}