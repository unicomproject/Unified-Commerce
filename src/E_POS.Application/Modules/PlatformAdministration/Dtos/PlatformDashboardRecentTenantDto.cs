namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformDashboardRecentTenantDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);
