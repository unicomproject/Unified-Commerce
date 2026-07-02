using E_POS.Domain.Modules.SubscriptionBilling.Constants;

namespace E_POS.Application.Modules.SubscriptionBilling.Mappers;

public static class SubscriptionPlanMapper
{
    public static string ToApiBillingCycle(string billingInterval)
    {
        return billingInterval switch
        {
            SubscriptionPlanConstants.BillingInterval.Monthly => "monthly",
            SubscriptionPlanConstants.BillingInterval.Yearly => "yearly",
            SubscriptionPlanConstants.BillingInterval.OneTime => "one_time",
            _ => billingInterval.ToLowerInvariant()
        };
    }

    public static string? ToDbBillingInterval(string? billingCycle)
    {
        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return null;
        }

        return billingCycle.Trim().ToLowerInvariant() switch
        {
            "monthly" => SubscriptionPlanConstants.BillingInterval.Monthly,
            "yearly" or "annual" => SubscriptionPlanConstants.BillingInterval.Yearly,
            "one_time" or "one-time" => SubscriptionPlanConstants.BillingInterval.OneTime,
            _ => billingCycle.Trim().ToUpperInvariant()
        };
    }

    public static string NormalizeApiStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "published" => SubscriptionPlanConstants.Status.Active,
            "archived" => SubscriptionPlanConstants.Status.Retired,
            _ => status.Trim().ToLowerInvariant()
        };
    }

    public static string NormalizePlanCode(string? planCode)
    {
        return (planCode ?? string.Empty).Trim().ToUpperInvariant();
    }

    public static string NormalizeCurrency(string? currencyCode)
    {
        var normalized = (currencyCode ?? SubscriptionPlanConstants.DefaultBaseCurrency).Trim().ToUpperInvariant();
        return normalized.Length >= 3 ? normalized[..3] : SubscriptionPlanConstants.DefaultBaseCurrency;
    }
}
