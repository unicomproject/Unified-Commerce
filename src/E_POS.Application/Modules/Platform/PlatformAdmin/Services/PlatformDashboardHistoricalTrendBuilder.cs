using System.Globalization;
using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

/// <summary>
/// Historical Dashboard trends using Platform Default Timezone day boundaries.
/// MRR history reconstructs ACTIVE status from subscription history and plan prices from
/// subscription snapshot + PLAN_CHANGED ChangeData prices when present.
/// </summary>
public static class PlatformDashboardHistoricalTrendBuilder
{
    public sealed record HistoryEvent(
        Guid TenantSubscriptionId,
        string ChangeType,
        DateTimeOffset ChangedAt,
        string? OldStatus,
        string? NewStatus,
        string? ChangeData);

    public sealed record SubscriptionHistoryState(
        Guid Id,
        string CurrencyCode,
        decimal CurrentPlanPrice,
        string BillingCycle,
        string? PlanBillingInterval,
        string? DiscountType,
        decimal? DiscountValue,
        string CurrentStatus,
        DateTimeOffset StartedAt,
        DateTimeOffset CreatedAt,
        IReadOnlyList<PlatformDashboardMrrCalculator.AddonMrrInput> Addons,
        IReadOnlyList<HistoryEvent> Events);

    public static PlatformDashboardTrendSeriesDto BuildActiveSubscriptionSeries(
        IReadOnlyList<SubscriptionHistoryState> subscriptions,
        DateTimeOffset utcNow,
        TimeZoneInfo timeZone)
    {
        var windows = PlatformDashboardTrendCalculator.GetMonthWindows(utcNow, timeZone);
        var previousPeriodEndActive = CountActiveAt(subscriptions, windows.PreviousEndUtc.AddTicks(-1));
        var current = CountActiveAt(subscriptions, utcNow);
        var (changePercent, changeStatus) = PlatformDashboardTrendCalculator.ComputeChange(
            current,
            previousPeriodEndActive,
            subscriptions.Count == 0);

        var points = new List<PlatformDashboardTrendPointDto>();
        for (var day = 1; day <= windows.DaysInCurrentMonth; day++)
        {
            var dayLocal = windows.CurrentMonthStartLocal.AddDays(day - 1);
            var dayStartUtc = ToUtc(dayLocal.ToDateTime(TimeOnly.MinValue), timeZone);
            if (dayStartUtc > utcNow)
            {
                break;
            }

            var dayEndUtc = ToUtc(dayLocal.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
            var asOf = dayEndUtc > utcNow ? utcNow : dayEndUtc.AddTicks(-1);
            points.Add(new PlatformDashboardTrendPointDto(
                dayLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CountActiveAt(subscriptions, asOf)));
        }

        return new PlatformDashboardTrendSeriesDto(
            "subscriptions",
            null,
            changePercent,
            changeStatus,
            points);
    }

    public static (bool Success, IReadOnlyList<PlatformDashboardTrendSeriesDto> Series, string? ErrorCode)
        BuildMrrSeries(
            IReadOnlyList<SubscriptionHistoryState> subscriptions,
            IReadOnlyDictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata> currencies,
            DateTimeOffset utcNow,
            TimeZoneInfo timeZone)
    {
        var windows = PlatformDashboardTrendCalculator.GetMonthWindows(utcNow, timeZone);
        var currenciesNeeded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reconstructable = subscriptions.Where(CanResolvePriceTimeline).ToList();
        // Subscriptions with unpriced PLAN_CHANGED history are excluded (never rewrite past from tip price).
        if (reconstructable.Count == 0)
        {
            if (subscriptions.Any(s => !CanResolvePriceTimeline(s)))
            {
                return (false, [], PlatformDashboardErrorCodes.MrrHistoryIncomplete);
            }

            return (true, [], null);
        }

        var previousByCurrency = AggregateMrrByCurrency(reconstructable, currencies, windows.PreviousEndUtc.AddTicks(-1), currenciesNeeded);
        if (previousByCurrency is null)
        {
            return (false, [], PlatformDashboardErrorCodes.CurrencyMetadataUnavailable);
        }

        var currentByCurrency = AggregateMrrByCurrency(reconstructable, currencies, utcNow, currenciesNeeded);
        if (currentByCurrency is null)
        {
            return (false, [], PlatformDashboardErrorCodes.CurrencyMetadataUnavailable);
        }

        var allCurrencies = previousByCurrency.Keys
            .Union(currentByCurrency.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var series = new List<PlatformDashboardTrendSeriesDto>();
        foreach (var currency in allCurrencies)
        {
            if (!currencies.ContainsKey(currency) &&
                !currencies.Keys.Any(k => string.Equals(k, currency, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, [], PlatformDashboardErrorCodes.CurrencyMetadataUnavailable);
            }

            var previous = previousByCurrency.GetValueOrDefault(currency);
            var current = currentByCurrency.GetValueOrDefault(currency);
            var (changePercent, changeStatus) = PlatformDashboardTrendCalculator.ComputeChange(
                current,
                previous,
                noHistory: previous == 0m && current == 0m && !HadAnyEligible(reconstructable, currency));

            var points = new List<PlatformDashboardTrendPointDto>();
            for (var day = 1; day <= windows.DaysInCurrentMonth; day++)
            {
                var dayLocal = windows.CurrentMonthStartLocal.AddDays(day - 1);
                var dayStartUtc = ToUtc(dayLocal.ToDateTime(TimeOnly.MinValue), timeZone);
                if (dayStartUtc > utcNow)
                {
                    break;
                }

                var dayEndUtc = ToUtc(dayLocal.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
                var asOf = dayEndUtc > utcNow ? utcNow : dayEndUtc.AddTicks(-1);
                var dayTotals = AggregateMrrByCurrency(reconstructable, currencies, asOf, currenciesNeeded);
                if (dayTotals is null)
                {
                    return (false, [], PlatformDashboardErrorCodes.CurrencyMetadataUnavailable);
                }

                var amount = dayTotals.GetValueOrDefault(currency);
                var meta = currencies.TryGetValue(currency, out var m)
                    ? m
                    : currencies.First(kv => string.Equals(kv.Key, currency, StringComparison.OrdinalIgnoreCase)).Value;
                points.Add(new PlatformDashboardTrendPointDto(
                    dayLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PlatformDashboardMrrCalculator.RoundToCurrency(amount, meta.DecimalPlaces)));
            }

            series.Add(new PlatformDashboardTrendSeriesDto(
                "mrr",
                currency.ToUpperInvariant(),
                changePercent,
                changeStatus,
                points));
        }

        return (true, series, null);
    }

    private static bool HadAnyEligible(IReadOnlyList<SubscriptionHistoryState> subscriptions, string currency) =>
        subscriptions.Any(s => string.Equals(s.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase));

    private static int CountActiveAt(IReadOnlyList<SubscriptionHistoryState> subscriptions, DateTimeOffset asOf) =>
        subscriptions.Count(s => string.Equals(ResolveStatusAt(s, asOf), TenantSubscriptionStatusConstants.Active, StringComparison.OrdinalIgnoreCase));

    private static Dictionary<string, decimal>? AggregateMrrByCurrency(
        IReadOnlyList<SubscriptionHistoryState> subscriptions,
        IReadOnlyDictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata> currencies,
        DateTimeOffset asOf,
        HashSet<string> currenciesNeeded)
    {
        var inputs = new List<PlatformDashboardMrrCalculator.SubscriptionMrrInput>();
        foreach (var sub in subscriptions)
        {
            var status = ResolveStatusAt(sub, asOf);
            if (!string.Equals(status, TenantSubscriptionStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (asOf < sub.StartedAt && asOf < sub.CreatedAt)
            {
                continue;
            }

            var price = ResolvePlanPriceAt(sub, asOf);
            if (price is null)
            {
                return null;
            }

            currenciesNeeded.Add(sub.CurrencyCode);
            inputs.Add(new PlatformDashboardMrrCalculator.SubscriptionMrrInput(
                sub.Id,
                TenantSubscriptionStatusConstants.Active,
                sub.CurrencyCode,
                price.Value,
                sub.BillingCycle,
                sub.PlanBillingInterval,
                sub.DiscountType,
                sub.DiscountValue,
                sub.Addons));
        }

        var result = PlatformDashboardMrrCalculator.Calculate(inputs, currencies);
        if (!result.Success)
        {
            return null;
        }

        return result.Groups.ToDictionary(g => g.CurrencyCode, g => g.Amount, StringComparer.OrdinalIgnoreCase);
    }

    public static string ResolveStatusAt(SubscriptionHistoryState sub, DateTimeOffset asOf)
    {
        var statusEvents = sub.Events
            .Where(e => string.Equals(e.ChangeType, TenantSubscriptionHistoryChangeTypeConstants.StatusChanged, StringComparison.OrdinalIgnoreCase)
                        && e.ChangedAt <= asOf)
            .OrderBy(e => e.ChangedAt)
            .ThenBy(e => e.NewStatus)
            .ToList();

        if (statusEvents.Count > 0)
        {
            return statusEvents[^1].NewStatus ?? sub.CurrentStatus;
        }

        // No status history yet — if created after asOf, treat as not started; else current status
        // only when asOf >= created (current row is the tip of timeline with no prior changes).
        if (asOf < sub.CreatedAt && asOf < sub.StartedAt)
        {
            return string.Empty;
        }

        return sub.CurrentStatus;
    }

    public static decimal? ResolvePlanPriceAt(SubscriptionHistoryState sub, DateTimeOffset asOf)
    {
        var planEvents = sub.Events
            .Where(e => string.Equals(e.ChangeType, TenantSubscriptionHistoryChangeTypeConstants.PlanChanged, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.ChangedAt)
            .ToList();

        if (planEvents.Count == 0)
        {
            // Subscription PlanPrice snapshot is valid for the whole lifetime when never changed.
            return sub.CurrentPlanPrice;
        }

        // Walk timeline: start from first event's old price if available, else fail when rewriting past.
        decimal? priceBeforeFirst = TryReadOldPrice(planEvents[0]);
        var futureChanges = planEvents.Where(e => e.ChangedAt > asOf).ToList();
        var pastChanges = planEvents.Where(e => e.ChangedAt <= asOf).ToList();

        if (pastChanges.Count == 0)
        {
            // asOf is before first plan change — need old price on first change event.
            return priceBeforeFirst;
        }

        var last = pastChanges[^1];
        var newPrice = TryReadNewPrice(last);
        if (newPrice is not null)
        {
            return newPrice;
        }

        // Legacy history without prices: only safe if no further ambiguity — refuse.
        if (futureChanges.Count == 0 && pastChanges.Count == planEvents.Count)
        {
            // All changes are in the past; current PlanPrice is the tip.
            return sub.CurrentPlanPrice;
        }

        return null;
    }

    private static bool CanResolvePriceTimeline(SubscriptionHistoryState sub)
    {
        var planEvents = sub.Events
            .Where(e => string.Equals(e.ChangeType, TenantSubscriptionHistoryChangeTypeConstants.PlanChanged, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (planEvents.Count == 0)
        {
            return true;
        }

        // Require priced ChangeData for every plan change so past MRR is not rewritten from the tip price.
        return planEvents.All(e => TryReadOldPrice(e) is not null && TryReadNewPrice(e) is not null);
    }

    private static decimal? TryReadOldPrice(HistoryEvent e) => TryReadPrice(e.ChangeData, "oldPlanPrice");

    private static decimal? TryReadNewPrice(HistoryEvent e) => TryReadPrice(e.ChangeData, "newPlanPrice");

    private static decimal? TryReadPrice(string? changeData, string property)
    {
        if (string.IsNullOrWhiteSpace(changeData))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(changeData);
            if (doc.RootElement.TryGetProperty(property, out var node) &&
                node.TryGetDecimal(out var value))
            {
                return value;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static DateTimeOffset ToUtc(DateTime localUnspecified, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, timeZone.GetUtcOffset(unspecified)).ToUniversalTime();
    }
}
