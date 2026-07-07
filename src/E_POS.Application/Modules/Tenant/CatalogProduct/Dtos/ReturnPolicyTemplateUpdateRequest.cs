namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateUpdateRequest(string TemplateCode, string Name, int? ReturnWindowDays, string Status);

