namespace E_POS.Domain.Modules.Tenant.Discount.Constants;

/// <summary>
/// Canonical internal policy codes used as authority envelopes for cashier-entered discounts.
/// These policies are resolved by the manual discount workflow and are not automatic catalog offers.
/// </summary>
public static class ManualDiscountPolicyCodes
{
    public const string OrderPercentage = "POS_MANUAL_PERCENTAGE";
    public const string OrderFixedAmount = "POS_MANUAL_FIXED";
    public const string LinePercentage = "POS_MANUAL_PERCENTAGE_LINE";
    public const string LineFixedAmount = "POS_MANUAL_FIXED_LINE";

    public static bool Contains(string? policyCode) => policyCode is
        OrderPercentage or OrderFixedAmount or LinePercentage or LineFixedAmount;

    public static string? Resolve(string calculationMethod, string scope) =>
        (calculationMethod, scope) switch
        {
            ("PERCENTAGE", "ORDER") => OrderPercentage,
            ("FIXED_AMOUNT", "ORDER") => OrderFixedAmount,
            ("PERCENTAGE", "LINE") => LinePercentage,
            ("FIXED_AMOUNT", "LINE") => LineFixedAmount,
            _ => null
        };
}
