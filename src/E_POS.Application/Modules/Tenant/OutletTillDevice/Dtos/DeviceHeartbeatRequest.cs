namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record DeviceHeartbeatRequest(
    string DeviceFingerprint,
    string? AppVersion = null);
