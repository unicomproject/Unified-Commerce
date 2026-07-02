namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed class PlatformTenantListQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? BillingStatus { get; set; }

    public Guid? PlanId { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }
}
