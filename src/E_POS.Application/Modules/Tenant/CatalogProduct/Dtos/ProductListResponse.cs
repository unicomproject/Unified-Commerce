namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public record ProductListResponse(
    IReadOnlyCollection<ProductSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

