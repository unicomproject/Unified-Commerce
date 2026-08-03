using System.Globalization;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public static class PlatformDashboardMrrCalculator
{
    public sealed record SubscriptionMrrInput(
        Guid SubscriptionId,
        string SubscriptionStatus,
        string CurrencyCode,
        decimal PlanPrice,
        string BillingCycle,
        string? PlanBillingInterval,
        string? DiscountType,
        decimal? DiscountValue,
        IReadOnlyList<AddonMrrInput> Addons);

    public sealed record AddonMrrInput(
        string Status,
        decimal UnitPrice,
        int Quantity,
        string CurrencyCode,
        bool AutoRenew);

    public sealed record CurrencyMetadata(
        string CurrencyCode,
        int DecimalPlaces);

    public sealed record MrrCalculationResult(
        bool Success,
        IReadOnlyList<PlatformDashboardMrrGroupDto> Groups,
        string? FailedCurrencyCode,
        string? ErrorCode);

    public static MrrCalculationResult Calculate(
        IReadOnlyList<SubscriptionMrrInput> subscriptions,
        IReadOnlyDictionary<string, CurrencyMetadata> currencyMetadataByCode)
    {
        var eligible = subscriptions
            .Where(IsEligibleForMrr)
            .ToList();

        if (eligible.Count == 0)
        {
            return new MrrCalculationResult(true, [], null, null);
        }

        var runningTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var currenciesNeeded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var subscription in eligible)
        {
            var currency = NormalizeCurrency(subscription.CurrencyCode);
            if (string.IsNullOrWhiteSpace(currency))
            {
                return Unavailable(currency ?? "(empty)");
            }

            currenciesNeeded.Add(currency);
            var contribution = CalculateSubscriptionContribution(subscription);
            runningTotals[currency] = runningTotals.GetValueOrDefault(currency) + contribution;
        }

        foreach (var currency in currenciesNeeded)
        {
            if (!TryResolveMetadata(currency, currencyMetadataByCode, out _))
            {
                return Unavailable(currency);
            }
        }

        var groups = runningTotals
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                TryResolveMetadata(pair.Key, currencyMetadataByCode, out var metadata);
                var rounded = RoundToCurrency(pair.Value, metadata!.DecimalPlaces);
                return new PlatformDashboardMrrGroupDto(metadata.CurrencyCode, metadata.DecimalPlaces, rounded);
            })
            .ToList();

        return new MrrCalculationResult(true, groups, null, null);
    }

    public static bool IsEligibleForMrr(SubscriptionMrrInput subscription)
    {
        if (!string.Equals(subscription.SubscriptionStatus, TenantSubscriptionStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsOneTime(subscription.BillingCycle) || IsOneTime(subscription.PlanBillingInterval))
        {
            return false;
        }

        return true;
    }

    public static decimal NormalizeToMonthly(decimal amount, string? billingCycle)
    {
        if (IsYearly(billingCycle))
        {
            return amount / 12m;
        }

        if (IsQuarterly(billingCycle))
        {
            return amount / 3m;
        }

        return amount;
    }

    public static decimal RoundToCurrency(decimal amount, int decimalPlaces) =>
        Math.Round(amount, decimalPlaces, MidpointRounding.ToEven);

    private static decimal CalculateSubscriptionContribution(SubscriptionMrrInput subscription)
    {
        var planMonthly = NormalizeToMonthly(subscription.PlanPrice, subscription.BillingCycle);

        var addonsMonthly = subscription.Addons
            .Where(IsActiveRecurringAddon)
            .Sum(addon => NormalizeToMonthly(addon.UnitPrice * Math.Max(1, addon.Quantity), subscription.BillingCycle));

        var subtotal = planMonthly + addonsMonthly;
        var discount = CalculateDiscount(subscription, planMonthly, addonsMonthly);
        var net = subtotal - discount;
        return net < 0m ? 0m : net;
    }

    private static decimal CalculateDiscount(SubscriptionMrrInput subscription, decimal planMonthly, decimal addonsMonthly)
    {
        if (string.IsNullOrWhiteSpace(subscription.DiscountType) || subscription.DiscountValue is null)
        {
            return 0m;
        }

        var value = subscription.DiscountValue.Value;
        if (string.Equals(subscription.DiscountType, TenantSubscriptionBillingConstants.DiscountTypePercent, StringComparison.OrdinalIgnoreCase))
        {
            var baseAmount = planMonthly + addonsMonthly;
            return baseAmount * (value / 100m);
        }

        if (string.Equals(subscription.DiscountType, TenantSubscriptionBillingConstants.DiscountTypeFixed, StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeToMonthly(value, subscription.BillingCycle);
        }

        return 0m;
    }

    private static bool IsActiveRecurringAddon(AddonMrrInput addon) =>
        addon.AutoRenew &&
        (string.Equals(addon.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(addon.Status, "active", StringComparison.OrdinalIgnoreCase));

    private static bool TryResolveMetadata(
        string currency,
        IReadOnlyDictionary<string, CurrencyMetadata> metadataByCode,
        out CurrencyMetadata? metadata)
    {
        metadata = null;
        if (string.IsNullOrWhiteSpace(currency))
        {
            return false;
        }

        if (!metadataByCode.TryGetValue(currency, out var found) &&
            !metadataByCode.TryGetValue(currency.ToUpperInvariant(), out found))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(found.CurrencyCode))
        {
            return false;
        }

        // ISO 4217 minor units are typically 0–3; reject negative / absurd values.
        if (found.DecimalPlaces < 0 || found.DecimalPlaces > 4)
        {
            return false;
        }

        metadata = found;
        return true;
    }

    private static MrrCalculationResult Unavailable(string currencyCode) =>
        new(false, [], currencyCode, PlatformDashboardErrorCodes.CurrencyMetadataUnavailable);

    private static string NormalizeCurrency(string? currencyCode) =>
        (currencyCode ?? string.Empty).Trim().ToUpperInvariant();

    private static bool IsOneTime(string? value) =>
        string.Equals(value, "ONE_TIME", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "one_time", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "onetime", StringComparison.OrdinalIgnoreCase);

    private static bool IsYearly(string? value) =>
        string.Equals(value, TenantSubscriptionBillingConstants.BillingCycleYearly, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "YEARLY", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "ANNUAL", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "annually", StringComparison.OrdinalIgnoreCase);

    private static bool IsQuarterly(string? value) =>
        string.Equals(value, "quarterly", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "QUARTERLY", StringComparison.OrdinalIgnoreCase);
}
