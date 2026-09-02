namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

public static class TenantUserAccessScopes
{
    public const string AllOutlets = "ALL_OUTLETS";
    public const string SelectedOutlets = "SELECTED_OUTLETS";
    public const string NoOutletAccess = "NO_OUTLET_ACCESS";

    public const string AllAccessibleTills = "ALL_ACCESSIBLE_TILLS";
    public const string SelectedTills = "SELECTED_TILLS";
    public const string NoTillAccess = "NO_TILL_ACCESS";

    public static IReadOnlyList<string> SupportedOutletScopes { get; } =
        [AllOutlets, SelectedOutlets, NoOutletAccess];

    public static IReadOnlyList<string> SupportedTillScopes { get; } =
        [AllAccessibleTills, SelectedTills, NoTillAccess];

    public static string? NormalizeOutletScope(string? value) =>
        Normalize(value, SupportedOutletScopes);

    public static string? NormalizeTillScope(string? value) =>
        Normalize(value, SupportedTillScopes);

    private static string? Normalize(string? value, IReadOnlyCollection<string> supported)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return supported.Contains(normalized, StringComparer.Ordinal) ? normalized : null;
    }
}
