namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CollectionListResponse(
    IReadOnlyList<CollectionSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
