namespace E_POS.Application.Modules.Tenant.Reports.Services;

public static class ReportBusinessDateCalculator
{
    public const string DefaultTimezone = "UTC";

    public static DateOnly FromInstant(DateTimeOffset completedAt, string? tenantTimezone)
    {
        var timezone = ResolveTimezone(tenantTimezone);
        var local = TimeZoneInfo.ConvertTime(completedAt, timezone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtcExclusive) ToUtcRange(
        DateOnly from,
        DateOnly to,
        string? tenantTimezone)
    {
        var timezone = ResolveTimezone(tenantTimezone);
        var fromLocal = from.ToDateTime(TimeOnly.MinValue);
        var toLocalExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return (
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(fromLocal, timezone), TimeSpan.Zero),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(toLocalExclusive, timezone), TimeSpan.Zero));
    }

    private static TimeZoneInfo ResolveTimezone(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
