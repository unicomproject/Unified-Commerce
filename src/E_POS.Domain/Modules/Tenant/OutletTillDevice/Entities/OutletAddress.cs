using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class OutletAddress : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
    public string AddressLine1 { get; protected set; } = string.Empty;
    public string? AddressLine2 { get; protected set; }
    public string City { get; protected set; } = string.Empty;
    public string? StateOrProvince { get; protected set; }
    public string? PostalCode { get; protected set; }
    public string CountryCode { get; protected set; } = string.Empty;

    public static OutletAddress Create(Guid id, Guid outletId, string addressLine1, string? addressLine2, string city, string? stateOrProvince, string? postalCode, string countryCode, DateTimeOffset now)
    {
        return new OutletAddress
        {
            Id = id,
            OutletId = outletId,
            AddressType = OutletConstants.PhysicalAddressType,
            AddressLine1 = addressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            City = city.Trim(),
            StateOrProvince = string.IsNullOrWhiteSpace(stateOrProvince) ? null : stateOrProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePhysicalAddress(string addressLine1, string? addressLine2, string city, string? stateOrProvince, string? postalCode, string countryCode, DateTimeOffset now)
    {
        AddressType = OutletConstants.PhysicalAddressType;
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = city.Trim();
        StateOrProvince = string.IsNullOrWhiteSpace(stateOrProvince) ? null : stateOrProvince.Trim();
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }
}
