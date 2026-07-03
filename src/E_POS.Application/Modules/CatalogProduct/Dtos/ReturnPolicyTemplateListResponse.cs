namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateListResponse(
    IReadOnlyList<ReturnPolicyTemplateSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);