namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyCreateRequest(string PolicyCode, string Name, int? ReturnWindowDays, string Status);
