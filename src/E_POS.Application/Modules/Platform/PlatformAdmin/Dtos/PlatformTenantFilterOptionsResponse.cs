namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantFilterOptionPlanDto(
    Guid Id,
    string Name,
    string PlanCode);

public sealed record PlatformTenantFilterOptionsResponse(
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> BillingStatuses,
    IReadOnlyList<string> OperatingModes,
    IReadOnlyList<PlatformTenantFilterOptionPlanDto> Plans);

