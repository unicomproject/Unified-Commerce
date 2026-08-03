using System.Globalization;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public static class PlatformDashboardTrendCalculator
{
    public const string ChangeOk = "ok";
    public const string ChangeNewNoBaseline = "new_no_baseline";
    public const string ChangeNoHistory = "no_history";
    public const string ChangeUnavailable = "unavailable";

    public static bool TryGetTimeZone(string? timezoneId, out TimeZoneInfo? timeZone, out string? error)
    {
        timeZone = null;
        error = null;

        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            error = PlatformDashboardErrorCodes.TimezoneUnavailable;
            return false;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            error = PlatformDashboardErrorCodes.TimezoneUnavailable;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            error = PlatformDashboardErrorCodes.TimezoneUnavailable;
            return false;
        }
    }

    public static (DateTimeOffset CurrentStartUtc, DateTimeOffset CurrentEndUtc, DateTimeOffset PreviousStartUtc, DateTimeOffset PreviousEndUtc, DateOnly CurrentMonthStartLocal, int DaysInCurrentMonth)
        GetMonthWindows(DateTimeOffset utcNow, TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var currentMonthStartLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var nextMonthStartLocal = currentMonthStartLocal.AddMonths(1);
        var previousMonthStartLocal = currentMonthStartLocal.AddMonths(-1);

        var currentStartUtc = ToUtc(currentMonthStartLocal, timeZone);
        var currentEndUtc = ToUtc(nextMonthStartLocal, timeZone);
        var previousStartUtc = ToUtc(previousMonthStartLocal, timeZone);
        var previousEndUtc = currentStartUtc;
        var daysInMonth = DateTime.DaysInMonth(localNow.Year, localNow.Month);

        return (
            currentStartUtc,
            currentEndUtc,
            previousStartUtc,
            previousEndUtc,
            DateOnly.FromDateTime(currentMonthStartLocal),
            daysInMonth);
    }

    public static PlatformDashboardTrendSeriesDto BuildCountSeries(
        string metric,
        IReadOnlyList<(DateTimeOffset CreatedAt, Guid Id)> events,
        DateTimeOffset utcNow,
        TimeZoneInfo timeZone,
        string? currencyCode = null)
    {
        var windows = GetMonthWindows(utcNow, timeZone);
        var previousCount = events.Count(x => x.CreatedAt >= windows.PreviousStartUtc && x.CreatedAt < windows.PreviousEndUtc);
        var currentCount = events.Count(x => x.CreatedAt >= windows.CurrentStartUtc && x.CreatedAt < windows.CurrentEndUtc);
        var (changePercent, changeStatus) = ComputeChange(currentCount, previousCount, events.Count == 0);

        var points = new List<PlatformDashboardTrendPointDto>();
        for (var day = 1; day <= windows.DaysInCurrentMonth; day++)
        {
            var dayLocal = windows.CurrentMonthStartLocal.AddDays(day - 1);
            var dayStartUtc = ToUtc(dayLocal.ToDateTime(TimeOnly.MinValue), timeZone);
            var dayEndUtc = ToUtc(dayLocal.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
            if (dayStartUtc > utcNow)
            {
                break;
            }

            var end = dayEndUtc > utcNow ? utcNow.AddTicks(1) : dayEndUtc;
            var cumulative = events.Count(x => x.CreatedAt < end);
            points.Add(new PlatformDashboardTrendPointDto(
                dayLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cumulative));
        }

        return new PlatformDashboardTrendSeriesDto(metric, currencyCode, changePercent, changeStatus, points);
    }

    public static (decimal? ChangePercent, string ChangeStatus) ComputeChange(decimal current, decimal previous, bool noHistory)
    {
        if (noHistory && current == 0m && previous == 0m)
        {
            return (null, ChangeNoHistory);
        }

        if (previous == 0m)
        {
            return (null, ChangeNewNoBaseline);
        }

        var percent = Math.Round(((current - previous) / previous) * 100m, 2, MidpointRounding.ToEven);
        return (percent, ChangeOk);
    }

    private static DateTimeOffset ToUtc(DateTime localUnspecified, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, timeZone.GetUtcOffset(unspecified)).ToUniversalTime();
    }
}
