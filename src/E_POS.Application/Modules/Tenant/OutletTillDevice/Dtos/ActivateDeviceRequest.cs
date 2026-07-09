namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record ActivateDeviceRequest(
    string ActivationCode,
    string DeviceFingerprint,
    string DeviceName,
    string DeviceType,
    string? Platform,
    string? AppVersion);
