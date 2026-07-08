namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantDetailSubscriptionDto(
    Guid PlanId,
    string PlanName,
    string SubscriptionStatus,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

