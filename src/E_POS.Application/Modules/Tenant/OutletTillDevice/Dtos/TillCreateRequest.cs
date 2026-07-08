namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillCreateRequest(
    Guid OutletId,
    string TillName,
    string TillCode,
    string TillType,
    decimal DefaultOpeningFloatAmount,
    string CurrencyCode,
    bool IsCashManaged,
    string Status);
