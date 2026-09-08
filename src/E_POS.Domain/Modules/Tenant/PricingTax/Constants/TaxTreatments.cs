namespace E_POS.Domain.Modules.Tenant.PricingTax.Constants;

/// <summary>
/// Canonical Tax Setup treatment (Second Brain Tax Management 2026-09-03).
/// Distinct from product TaxPriceMode (INCLUSIVE/EXCLUSIVE).
/// </summary>
public static class TaxTreatments
{
    public const string Taxable = "TAXABLE";
    public const string ZeroRated = "ZERO_RATED";
    public const string Exempt = "EXEMPT";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Taxable,
        ZeroRated,
        Exempt
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value.Trim());

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    /// <summary>
    /// Maps legacy free-text TaxType into canonical treatment for backfill.
    /// Ambiguous 0% / OTHER defaults to TAXABLE (safest; EXEMPT only when explicitly indicated).
    /// </summary>
    public static string MapFromLegacyTaxType(string? taxType, decimal? knownRatePercent = null)
    {
        var normalized = (taxType ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized is "EXEMPT" or "TAX_EXEMPT" or "EXEMPTION")
            return Exempt;

        if (normalized is "ZERO_RATED" or "ZERO-RATED" or "ZERORATED")
            return ZeroRated;

        if (normalized is "TAXABLE" or "PERCENTAGE" or "VAT" or "GST" or "SALES_TAX" or "SERVICE_TAX")
        {
            if (knownRatePercent == 0m && normalized is "ZERO_RATED" or "ZERO-RATED")
                return ZeroRated;
            return Taxable;
        }

        if (knownRatePercent == 0m && (normalized.Contains("ZERO") || normalized.Contains("0%")))
            return ZeroRated;

        if (normalized.Contains("EXEMPT"))
            return Exempt;

        // OTHER / empty / unknown → TAXABLE (do not invent EXEMPT)
        return Taxable;
    }

    public static decimal? ResolveInitialRate(string treatment, decimal? requestedRate)
    {
        var t = Normalize(treatment);
        return t switch
        {
            ZeroRated => 0m,
            Exempt => null,
            _ => requestedRate
        };
    }

    public static bool AllowsPercentageSchedule(string treatment)
    {
        var t = Normalize(treatment);
        return t is Taxable or ZeroRated;
    }
}
