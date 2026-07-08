namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillUpdateRequest(
    Guid OutletId,
    string TillAreaName,
    int TillNumber,
    string Name,
    string TillCode,
    string Status);

