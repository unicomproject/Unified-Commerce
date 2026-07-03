namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record PosDeviceUpdateRequest(
    Guid OutletId,
    string Name,
    string DeviceSerialNumber,
    string Status);