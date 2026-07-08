using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class OutletAddress : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
    public string AddressLine1 { get; protected set; } = string.Empty;
    public string? AddressLine2 { get; protected set; }
    public string City { get; protected set; } = string.Empty;
    public string? StateOrProvince { get; protected set; }
    public string? PostalCode { get; protected set; }
    public string CountryCode { get; protected set; } = string.Empty;
    public string? ContactName { get; protected set; }
    public string? ContactPhone { get; protected set; }
    public bool IsPrimary { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static OutletAddress Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        string addressLine1,
        string? addressLine2,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string countryCode,
        string? contactName,
        string? contactPhone,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new OutletAddress
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            AddressType = OutletConstants.PhysicalAddressType,
            AddressLine1 = addressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            City = city.Trim(),
            StateOrProvince = string.IsNullOrWhiteSpace(stateOrProvince) ? null : stateOrProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim(),
            IsPrimary = true,
            Status = OutletConstants.ActiveStatus,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePhysicalAddress(
        string addressLine1,
        string? addressLine2,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string countryCode,
        string? contactName,
        string? contactPhone,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        AddressType = OutletConstants.PhysicalAddressType;
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = city.Trim();
        StateOrProvince = string.IsNullOrWhiteSpace(stateOrProvince) ? null : stateOrProvince.Trim();
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
