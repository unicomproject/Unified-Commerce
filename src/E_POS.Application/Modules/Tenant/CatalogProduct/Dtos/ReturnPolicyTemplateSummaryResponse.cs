namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateSummaryResponse(
    Guid Id,
    string TemplateCode,
    string Name,
    int? ReturnWindowDays,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
