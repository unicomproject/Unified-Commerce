namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillCreateRequest(Guid OutletId, string Name, string TillCode, string Status);

