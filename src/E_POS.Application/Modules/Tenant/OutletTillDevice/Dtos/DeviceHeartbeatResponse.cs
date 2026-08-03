namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record DeviceHeartbeatResponse(
    Guid DeviceId,
    DateTimeOffset ServerTime,
    DateTimeOffset LastSeenAt,
    bool IsTrusted);
