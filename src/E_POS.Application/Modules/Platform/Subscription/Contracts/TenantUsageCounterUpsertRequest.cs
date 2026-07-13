namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public sealed record TenantUsageCounterUpsertRequest(
    Guid TenantId,
    Guid FeatureLimitDefinitionId,
    string UsageScope,
    Guid? ScopeReferenceId,
    DateTimeOffset PeriodStart,
    DateTimeOffset? PeriodEnd = null,
    decimal CurrentValue = 0m,
    decimal? LimitValue = null);
