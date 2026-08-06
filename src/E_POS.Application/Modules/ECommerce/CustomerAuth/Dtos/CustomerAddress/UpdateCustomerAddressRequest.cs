namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public class UpdateCustomerAddressRequest
{
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string AddressType { get; set; } = "HOME";
    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }
}
