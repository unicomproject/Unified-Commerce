namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record CurrentDeviceResponseDto(
    Guid TenantId,
    CurrentDeviceDeviceDto Device,
    CurrentDeviceOutletDto Outlet,
    CurrentDeviceTillDto Till);

public sealed record CurrentDeviceDeviceDto(
    Guid Id,
    string DeviceCode,
    string DeviceName,
    string DeviceType,
    string? Platform,
    Guid OutletId,
    Guid TillId,
    bool IsTrusted);

public sealed record CurrentDeviceOutletDto(
    Guid Id,
    string Name);

public sealed record CurrentDeviceTillDto(
    Guid Id,
    string Code,
    string Name,
    decimal DefaultOpeningFloatAmount,
    string CurrencyCode);
