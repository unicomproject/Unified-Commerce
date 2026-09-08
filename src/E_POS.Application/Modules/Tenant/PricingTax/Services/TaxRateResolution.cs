using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Services;

public static class TaxRateResolution
{
    public const string Historical = "HISTORICAL";
    public const string Current = "CURRENT";
    public const string Scheduled = "SCHEDULED";

    public static DateOnly ToBusinessDate(DateTimeOffset utcTimestamp, string? tenantTimezone)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(tenantTimezone))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tenantTimezone);
                var local = TimeZoneInfo.ConvertTime(utcTimestamp, tz);
                return DateOnly.FromDateTime(local.DateTime);
            }
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall through to UTC date.
        }
        catch (InvalidTimeZoneException)
        {
        }

        return DateOnly.FromDateTime(utcTimestamp.UtcDateTime);
    }

    public static TaxRate? ResolveCurrent(IEnumerable<TaxRate> rates, DateOnly businessDate)
    {
        return rates
            .Where(r => r.IsApplicableOn(businessDate))
            .OrderByDescending(r => r.ValidFrom ?? DateOnly.MinValue)
            .ThenByDescending(r => r.CreatedAt)
            .FirstOrDefault();
    }

    public static TaxRate? ResolveNext(IEnumerable<TaxRate> rates, DateOnly businessDate)
    {
        return rates
            .Where(r => r.IsFutureRelativeTo(businessDate))
            .OrderBy(r => r.ValidFrom)
            .ThenBy(r => r.CreatedAt)
            .FirstOrDefault();
    }

    public static string Classify(TaxRate rate, DateOnly businessDate)
    {
        if (rate.IsFutureRelativeTo(businessDate))
            return Scheduled;
        if (rate.IsHistoricalRelativeTo(businessDate))
            return Historical;
        if (rate.IsApplicableOn(businessDate))
            return Current;
        return Historical;
    }

    public static bool PeriodsOverlap(DateOnly? aFrom, DateOnly? aUntil, DateOnly? bFrom, DateOnly? bUntil)
    {
        var aStart = aFrom ?? DateOnly.MinValue;
        var aEnd = aUntil ?? DateOnly.MaxValue;
        var bStart = bFrom ?? DateOnly.MinValue;
        var bEnd = bUntil ?? DateOnly.MaxValue;
        return aStart <= bEnd && bStart <= aEnd;
    }
}
