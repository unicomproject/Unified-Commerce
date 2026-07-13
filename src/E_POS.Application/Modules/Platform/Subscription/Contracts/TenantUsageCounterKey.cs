using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public sealed record TenantUsageCounterKey(
    Guid TenantId,
    Guid FeatureLimitDefinitionId,
    string UsageScope,
    Guid? ScopeReferenceId,
    DateTimeOffset PeriodStart)
{
    public string NormalizedUsageScope => string.IsNullOrWhiteSpace(UsageScope)
        ? TenantUsageCounterAlignmentConstants.UsageScope.Tenant
        : UsageScope.Trim().ToUpperInvariant();
}
