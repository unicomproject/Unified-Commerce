namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record PosDeviceUpdateRequest(
    Guid OutletId,
    string Name,
    string DeviceSerialNumber,
    string Status);
