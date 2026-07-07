namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public record TaxClassListResponse(
    IReadOnlyCollection<TaxClassResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

