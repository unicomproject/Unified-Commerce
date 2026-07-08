namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletAddressRequest(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateOrProvince,
    string? PostalCode,
    string CountryCode,
    string? ContactName,
    string? ContactPhone);
