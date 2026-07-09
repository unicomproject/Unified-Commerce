namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public sealed record CanonicalCapacityLimitDefinition(
    Guid Id,
    string LimitKey,
    Guid PlatformFeatureId);
