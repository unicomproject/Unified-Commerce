namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ProductStructureConstants
{
    public const string Simple = "SIMPLE";
    public const string Variant = "VARIANT";
    public const string Bundle = "BUNDLE";
    public const string DefaultDraftStructure = Simple;

    public static bool TryNormalize(string? structure, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(structure))
        {
            return false;
        }

        var candidate = structure.Trim().ToUpperInvariant();
        if (candidate is Simple or Variant or Bundle)
        {
            normalized = candidate;
            return true;
        }

        return false;
    }

    public static string Normalize(string? structure)
    {
        if (TryNormalize(structure, out var normalized))
        {
            return normalized;
        }

        return DefaultDraftStructure;
    }

    public static bool IsValid(string? structure)
    {
        return TryNormalize(structure, out _);
    }
}

