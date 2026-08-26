namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed class PlatformUserListQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? Role { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }
}
