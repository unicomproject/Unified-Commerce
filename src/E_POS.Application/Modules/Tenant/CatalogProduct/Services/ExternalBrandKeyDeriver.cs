namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Derives a stable, provider-neutral (Release 1) external brand key from a provider's free-text
/// brand field. Neither OpenFoodFacts nor UPCitemdb expose a stable brand identifier today, so the
/// R1 strategy is a normalized-text key derived from the display text itself.
/// </summary>
public static class ExternalBrandKeyDeriver
{
    /// <summary>
    /// Providers may return multiple comma-separated brand names in a single field (e.g.
    /// OpenFoodFacts "Coca-Cola, The Coca-Cola Company"). This deterministically takes the first
    /// meaningful (non-empty, trimmed) segment rather than the whole string or attempting to infer
    /// parent-company ownership.
    /// </summary>
    public static string? ExtractPrimaryBrandSegment(string? brandText)
    {
        if (string.IsNullOrWhiteSpace(brandText))
        {
            return null;
        }

        foreach (var segment in brandText.Split(','))
        {
            var trimmed = segment.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }
        }

        return null;
    }

    /// <summary>
    /// Normalizes a display-text brand segment into a comparison-safe key: trim, lowercase,
    /// punctuation removed, repeated whitespace collapsed. Example: "Coca-Cola" -> "coca cola".
    /// </summary>
    public static string? DeriveExternalBrandKey(string? brandSegment)
    {
        if (string.IsNullOrWhiteSpace(brandSegment))
        {
            return null;
        }

        var lowered = brandSegment.Trim().ToLowerInvariant();
        var chars = lowered.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();
        var collapsed = string.Join(
            ' ',
            new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrEmpty(collapsed) ? null : collapsed;
    }
}
