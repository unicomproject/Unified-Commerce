namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyUpdateRequest(string PolicyCode, string Name, int? ReturnWindowDays, string Status);
