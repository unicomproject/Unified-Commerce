using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class TenantUsageCounter : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PlatformFeatureId { get; protected set; }
    public string UsagePeriodStart { get; protected set; } = string.Empty;
    public decimal UsedQuantity { get; protected set; }
    public Guid FeatureLimitDefinitionId { get; protected set; }
    public string UsageScope { get; protected set; } = string.Empty;
    public Guid? ScopeReferenceId { get; protected set; }
    public decimal CurrentValue { get; protected set; }
    public decimal? LimitValue { get; protected set; }
    public DateTimeOffset PeriodStart { get; protected set; }
    public DateTimeOffset? PeriodEnd { get; protected set; }
    public DateTimeOffset? LastCalculatedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static TenantUsageCounter Create(
        Guid id,
        Guid tenantId,
        Guid featureLimitDefinitionId,
        Guid platformFeatureId,
        string usageScope,
        Guid? scopeReferenceId,
        decimal currentValue,
        decimal? limitValue,
        DateTimeOffset periodStart,
        DateTimeOffset? periodEnd,
        DateTimeOffset now,
        string? status = null,
        DateTimeOffset? lastCalculatedAt = null)
    {
        var normalizedCurrentValue = Math.Max(0m, currentValue);
        var normalizedLimitValue = limitValue.HasValue
            ? Math.Max(0m, limitValue.Value)
            : (decimal?)null;
        var normalizedUsageScope = NormalizeRequiredText(
            usageScope,
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant);
        var normalizedStatus = NormalizeRequiredText(
            status,
            SubscriptionCatalogConstants.RecordStatus.Active);

        return new TenantUsageCounter
        {
            Id = id,
            TenantId = tenantId,
            PlatformFeatureId = platformFeatureId,
            FeatureLimitDefinitionId = featureLimitDefinitionId,
            UsageScope = normalizedUsageScope,
            ScopeReferenceId = scopeReferenceId,
            UsagePeriodStart = periodStart.ToString("O"),
            UsedQuantity = normalizedCurrentValue,
            CurrentValue = normalizedCurrentValue,
            LimitValue = normalizedLimitValue,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            LastCalculatedAt = lastCalculatedAt,
            Status = normalizedStatus,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Represents a pre-7A row shape for migration backfill testing.
    /// </summary>
    public static TenantUsageCounter CreateLegacy(
        Guid id,
        Guid tenantId,
        Guid platformFeatureId,
        string usagePeriodStart,
        decimal usedQuantity,
        DateTimeOffset createdAt)
    {
        return new TenantUsageCounter
        {
            Id = id,
            TenantId = tenantId,
            PlatformFeatureId = platformFeatureId,
            UsagePeriodStart = usagePeriodStart,
            UsedQuantity = Math.Max(0m, usedQuantity),
            FeatureLimitDefinitionId = Guid.Empty,
            UsageScope = string.Empty,
            CurrentValue = 0m,
            PeriodStart = default,
            Status = string.Empty,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void SetCurrentValue(decimal value, DateTimeOffset now)
    {
        var normalized = Math.Max(0m, value);
        CurrentValue = normalized;
        UsedQuantity = normalized;
        UpdatedAt = now;
    }

    public void ApplyAlignmentBackfill(
        Guid featureLimitDefinitionId,
        string usageScope,
        string status,
        DateTimeOffset periodStart)
    {
        var normalizedCurrentValue = CurrentValue != 0m
            ? CurrentValue
            : Math.Max(0m, UsedQuantity);

        FeatureLimitDefinitionId = featureLimitDefinitionId;
        UsageScope = usageScope;
        Status = status;
        PeriodStart = periodStart;
        CurrentValue = normalizedCurrentValue;
        UsedQuantity = normalizedCurrentValue;
    }

    public static DateTimeOffset ParseUsagePeriodStart(string usagePeriodStart, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(usagePeriodStart))
        {
            return createdAt;
        }

        if (DateTimeOffset.TryParse(
                usagePeriodStart,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed;
        }

        return createdAt;
    }

    private static string NormalizeRequiredText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToUpperInvariant();
    }
}
