namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletAddressResponse(
    Guid Id,
    string AddressType,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateOrProvince,
    string? PostalCode,
    string CountryCode,
    string? ContactName,
    string? ContactPhone,
    bool IsPrimary,
    string Status);
