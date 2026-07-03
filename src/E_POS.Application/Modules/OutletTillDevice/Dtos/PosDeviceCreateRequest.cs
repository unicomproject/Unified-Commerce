namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record PosDeviceCreateRequest(
    Guid OutletId,
    string Name,
    string DeviceSerialNumber,
    string Status);