using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerAddress : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerId { get; protected set; }
    public string ContactName { get; protected set; } = string.Empty;
    public string ContactPhone { get; protected set; } = string.Empty;
    public string AddressLine1 { get; protected set; } = string.Empty;
    public string? AddressLine2 { get; protected set; }
    public string City { get; protected set; } = string.Empty;
    public string State { get; protected set; } = string.Empty;
    public string PostalCode { get; protected set; } = string.Empty;
    public string CountryCode { get; protected set; } = string.Empty;
    public string AddressType { get; protected set; } = "HOME"; // HOME, WORK, OTHER
    public bool IsDefaultShipping { get; protected set; }
    public bool IsDefaultBilling { get; protected set; }

    public virtual Customer Customer { get; protected set; } = null!;

    public static CustomerAddress Create(
        Guid id,
        Guid tenantId,
        Guid customerId,
        string contactName,
        string contactPhone,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string postalCode,
        string countryCode,
        string addressType,
        bool isDefaultShipping,
        bool isDefaultBilling,
        DateTimeOffset now)
    {
        return new CustomerAddress
        {
            Id = id,
            TenantId = tenantId,
            CustomerId = customerId,
            ContactName = contactName.Trim(),
            ContactPhone = contactPhone.Trim(),
            AddressLine1 = addressLine1.Trim(),
            AddressLine2 = addressLine2?.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            PostalCode = postalCode.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            AddressType = addressType.Trim().ToUpperInvariant(),
            IsDefaultShipping = isDefaultShipping,
            IsDefaultBilling = isDefaultBilling,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string contactName,
        string contactPhone,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string postalCode,
        string countryCode,
        string addressType,
        bool isDefaultShipping,
        bool isDefaultBilling,
        DateTimeOffset now)
    {
        ContactName = contactName.Trim();
        ContactPhone = contactPhone.Trim();
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        AddressType = addressType.Trim().ToUpperInvariant();
        IsDefaultShipping = isDefaultShipping;
        IsDefaultBilling = isDefaultBilling;
        UpdatedAt = now;
    }

    public void SetDefaultShipping(bool isDefault) => IsDefaultShipping = isDefault;
    public void SetDefaultBilling(bool isDefault) => IsDefaultBilling = isDefault;
}
