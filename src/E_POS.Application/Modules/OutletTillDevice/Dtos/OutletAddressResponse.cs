namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletAddressResponse(Guid Id, string AddressType, string AddressLine1, string? AddressLine2, string City, string? StateOrProvince, string? PostalCode, string CountryCode);