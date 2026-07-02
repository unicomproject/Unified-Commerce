namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantSummaryResponse(
    int TotalTenants,
    int ActiveTenants,
    int SuspendedTenants,
    int TrialTenants,
    int PendingActivationTenants,
    int PendingBillingCount,
    int TotalOutlets,
    int TotalTills);
