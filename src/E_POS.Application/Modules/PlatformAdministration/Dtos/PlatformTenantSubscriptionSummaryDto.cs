namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantSubscriptionSummaryDto(
    Guid PlanId,
    string PlanName,
    string PlanCode,
    string SubscriptionStatus);
