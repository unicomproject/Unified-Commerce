namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantDetailSubscriptionDto(
    Guid PlanId,
    string PlanName,
    string SubscriptionStatus,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);
