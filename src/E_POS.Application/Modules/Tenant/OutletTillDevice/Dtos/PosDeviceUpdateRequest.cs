namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record PosDeviceUpdateRequest(
    Guid OutletId,
    string DeviceName,
    string DeviceType,
    string Status);
