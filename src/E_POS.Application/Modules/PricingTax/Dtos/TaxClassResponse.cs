namespace E_POS.Application.Modules.PricingTax.Dtos;

public class TaxClassResponse
{
    public Guid Id { get; set; }
    public string TaxClassCode { get; set; } = string.Empty;
    public string TaxClassName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefaultTaxClass { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // Detailed assigned rates
    public List<TaxClassRateDetailResponse> AssignedRates { get; set; } = new();
}

public class TaxClassRateDetailResponse
{
    public Guid TaxRateId { get; set; }
    public string TaxRateCode { get; set; } = string.Empty;
    public string TaxRateName { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public bool IsCompound { get; set; }
    public int SortOrder { get; set; }
}
