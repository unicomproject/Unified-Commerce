namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillUpdateRequest(Guid OutletId, string Name, string TillCode, string Status);

