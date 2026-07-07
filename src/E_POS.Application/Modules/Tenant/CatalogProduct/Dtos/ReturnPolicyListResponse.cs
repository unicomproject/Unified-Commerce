namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record ReturnPolicyListResponse(
    IReadOnlyList<ReturnPolicySummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
