namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public class TaxRateUpdateRequest
{
    public string TaxRateName { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public bool IsCompound { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
}

