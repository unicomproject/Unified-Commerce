namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateSummaryResponse(
    Guid Id,
    string TemplateCode,
    string Name,
    int? ReturnWindowDays,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);