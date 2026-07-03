namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateUpdateRequest(string TemplateCode, string Name, int? ReturnWindowDays, string Status);
