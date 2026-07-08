namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public record PriceListListResponse(
    IReadOnlyCollection<PriceListSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

