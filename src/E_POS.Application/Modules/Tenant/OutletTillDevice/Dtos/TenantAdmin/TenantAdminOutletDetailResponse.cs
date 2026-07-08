namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminOutletDetailResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode,
    string OutletType,
    string Status,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? DistrictOrProvince,
    string? PostalCode,
    string? PhoneNumber,
    string? EmailAddress,
    string? ManagerName,
    string? OperatingHours,
    DateTimeOffset? OpeningDate,
    string? TaxRegistrationId,
    string? Notes);
