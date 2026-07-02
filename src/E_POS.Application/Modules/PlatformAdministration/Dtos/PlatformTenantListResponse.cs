namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantListResponse(
    IReadOnlyList<PlatformTenantListItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
