namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record TillSummaryResponse(
    Guid Id,
    Guid OutletId,
    string OutletCode,
    string OutletName,
    string TillCode,
    string Name,
    string Status,
    bool IsDeviceAssigned,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);