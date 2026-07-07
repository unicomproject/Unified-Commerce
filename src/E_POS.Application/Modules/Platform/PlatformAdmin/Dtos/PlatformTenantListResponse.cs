namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantListResponse(
    IReadOnlyList<PlatformTenantListItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

