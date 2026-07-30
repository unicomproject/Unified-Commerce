namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed class PlatformTenantListQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? Status { get; set; }

    /// <summary>
    /// Lifecycle group filter. Supported: <c>setup_pending</c> (draft, setup_pending, pending_activation, pending_payment).
    /// </summary>
    public string? StatusGroup { get; set; }

    public string? BillingStatus { get; set; }

    public Guid? PlanId { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }
}

