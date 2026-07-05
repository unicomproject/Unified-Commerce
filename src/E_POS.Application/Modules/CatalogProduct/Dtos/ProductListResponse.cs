namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public record ProductListResponse(
    IReadOnlyCollection<ProductSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
