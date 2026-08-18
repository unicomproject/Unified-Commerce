namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public class TaxClassUpdateRequest
{
    public string TaxClassName { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefaultTaxClass { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<Guid>? AssignedRateIds { get; set; }
}

