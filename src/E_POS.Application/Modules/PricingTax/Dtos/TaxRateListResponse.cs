namespace E_POS.Application.Modules.PricingTax.Dtos;

public record TaxRateListResponse(
    IReadOnlyCollection<TaxRateResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
