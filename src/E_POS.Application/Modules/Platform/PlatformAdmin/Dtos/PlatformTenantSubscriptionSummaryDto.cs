namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantSubscriptionSummaryDto(
    Guid PlanId,
    string PlanName,
    string PlanCode,
    string SubscriptionStatus);

