namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyTemplateListResponse(
    IReadOnlyList<ReturnPolicyTemplateSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
