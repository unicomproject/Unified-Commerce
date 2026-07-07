namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public class TaxClassCreateRequest
{
    public string TaxClassCode { get; set; } = string.Empty;
    public string TaxClassName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefaultTaxClass { get; set; }
    public List<Guid>? AssignedRateIds { get; set; }
}

