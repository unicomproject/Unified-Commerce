namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillResponse(
    Guid Id,
    Guid OutletId,
    string OutletCode,
    string OutletName,
    string TillAreaName,
    int TillNumber,
    string TillCode,
    string Name,
    string Status,
    bool IsDeviceAssigned,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
