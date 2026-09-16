namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;

public static class StripeMoney
{
    // Stripe's zero-decimal currencies — amounts for these must NOT be multiplied/divided by 100.
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA",
        "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    public static long ToMinorUnits(decimal amount, string currencyCode) =>
        ZeroDecimalCurrencies.Contains(currencyCode)
            ? (long)Math.Round(amount, MidpointRounding.AwayFromZero)
            : (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public static decimal FromMinorUnits(long amount, string currencyCode) =>
        ZeroDecimalCurrencies.Contains(currencyCode)
            ? amount
            : amount / 100m;
}
