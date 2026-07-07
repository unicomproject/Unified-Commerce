using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantProfile : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string? LegalName { get; protected set; }
    public string? RegistrationNumber { get; protected set; }
    public string? TaxNumber { get; protected set; }
    public string? PrimaryContactName { get; protected set; }
    public string? PrimaryEmail { get; protected set; }
    public string? PrimaryPhone { get; protected set; }
    public string? WebsiteUrl { get; protected set; }
    public string? CountryCode { get; protected set; }

    public static TenantProfile Create(
        Guid id,
        Guid tenantId,
        string? legalName,
        string? registrationNumber,
        string? taxNumber,
        string? primaryContactName,
        string? primaryEmail,
        string? primaryPhone,
        string? websiteUrl,
        string? countryCode,
        DateTimeOffset now)
    {
        return new TenantProfile
        {
            Id = id,
            TenantId = tenantId,
            LegalName = NormalizeOptionalText(legalName),
            RegistrationNumber = NormalizeOptionalText(registrationNumber),
            TaxNumber = NormalizeOptionalText(taxNumber),
            PrimaryContactName = NormalizeOptionalText(primaryContactName),
            PrimaryEmail = NormalizeOptionalText(primaryEmail),
            PrimaryPhone = NormalizeOptionalText(primaryPhone),
            WebsiteUrl = NormalizeOptionalText(websiteUrl),
            CountryCode = NormalizeOptionalText(countryCode),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

