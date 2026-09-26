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

    /// <summary>
    /// Shared REP date boundary: inclusive business-local From/To dates become
    /// [local midnight of From, local midnight after To) in UTC. Either side may be open.
    /// </summary>
    public static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtcExclusive) ResolveBusinessDateRange(
        DateOnly? from,
        DateOnly? to,
        string? tenantTimezone) =>
        (from.HasValue ? ToUtcRange(from.Value, from.Value, tenantTimezone).FromUtc : null,
         to.HasValue ? ToUtcRange(to.Value, to.Value, tenantTimezone).ToUtcExclusive : null);

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
