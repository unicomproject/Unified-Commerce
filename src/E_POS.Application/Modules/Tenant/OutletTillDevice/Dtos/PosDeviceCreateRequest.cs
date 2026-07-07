namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record PosDeviceCreateRequest(
    Guid OutletId,
    string Name,
    string DeviceSerialNumber,
    string Status);
