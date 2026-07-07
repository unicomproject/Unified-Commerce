namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public class TaxRateCreateRequest
{
    public Guid TaxJurisdictionId { get; set; }
    public string TaxRateCode { get; set; } = string.Empty;
    public string TaxRateName { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public bool IsCompound { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
}

