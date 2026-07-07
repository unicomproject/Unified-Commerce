using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantAddress : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
    public string? Line1 { get; protected set; }
    public string? Line2 { get; protected set; }
    public string? City { get; protected set; }
    public string? State { get; protected set; }
    public string? PostalCode { get; protected set; }
    public string? CountryCode { get; protected set; }

    public static TenantAddress CreateRegistered(
        Guid id,
        Guid tenantId,
        string? line1,
        string? line2,
        string? city,
        string? state,
        string? postalCode,
        string? countryCode,
        DateTimeOffset now)
    {
        return new TenantAddress
        {
            Id = id,
            TenantId = tenantId,
            AddressType = "REGISTERED",
            Line1 = NormalizeOptionalText(line1),
            Line2 = NormalizeOptionalText(line2),
            City = NormalizeOptionalText(city),
            State = NormalizeOptionalText(state),
            PostalCode = NormalizeOptionalText(postalCode),
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

