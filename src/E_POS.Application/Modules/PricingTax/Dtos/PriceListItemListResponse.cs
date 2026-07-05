namespace E_POS.Application.Modules.PricingTax.Dtos;

public record PriceListItemListResponse(
    IReadOnlyCollection<PriceListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
