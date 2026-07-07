namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateCreateRequest(string TemplateCode, string Name, int? ReturnWindowDays, string Status);

