namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ProductUnitModelConstants
{
    public const string SingleUnit = "SINGLE_UNIT";
    public const string MultipleUnits = "MULTIPLE_UNITS";

    public static bool IsValid(string? model) =>
        string.Equals(model, SingleUnit, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(model, MultipleUnits, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? model)
    {
        if (string.Equals(model, MultipleUnits, StringComparison.OrdinalIgnoreCase))
        {
            return MultipleUnits;
        }

        return SingleUnit;
    }
}
