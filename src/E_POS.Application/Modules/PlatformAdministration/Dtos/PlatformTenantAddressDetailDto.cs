namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantAddressDetailDto(
    string AddressType,
    string? Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? CountryCode);
