using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantAddress : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
    public string AddressLine1 { get; protected set; } = string.Empty;
    public string? AddressLine2 { get; protected set; }
    public string? City { get; protected set; }
    public string? StateOrProvince { get; protected set; }
    public string? PostalCode { get; protected set; }
    public string CountryCode { get; protected set; } = string.Empty;
    public bool IsPrimary { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static TenantAddress CreateRegistered(
        Guid id,
        Guid tenantId,
        string addressLine1,
        string? addressLine2,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string countryCode,
        bool isPrimary,
        string status,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new TenantAddress
        {
            Id = id,
            TenantId = tenantId,
            AddressType = "REGISTERED",
            AddressLine1 = addressLine1.Trim(),
            AddressLine2 = NormalizeOptionalText(addressLine2),
            City = NormalizeOptionalText(city),
            StateOrProvince = NormalizeOptionalText(stateOrProvince),
            PostalCode = NormalizeOptionalText(postalCode),
            CountryCode = countryCode.Trim(),
            IsPrimary = isPrimary,
            Status = status.Trim(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
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

