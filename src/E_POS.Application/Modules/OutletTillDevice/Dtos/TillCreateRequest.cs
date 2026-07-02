namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record TillCreateRequest(Guid OutletId, string Name, string TillCode, string Status);
