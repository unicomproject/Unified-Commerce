using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public class TaxAggregateCreateRequest
{
    public string TaxName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TaxAggregateUpdateRequest
{
    public string TaxName { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TaxAggregateResponse
{
    public Guid Id { get; set; } // Maps to TaxClassId
    public string TaxName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public record TaxAggregateListResponse(
    IReadOnlyCollection<TaxAggregateResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
