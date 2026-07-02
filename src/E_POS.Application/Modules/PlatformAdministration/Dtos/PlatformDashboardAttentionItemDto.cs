namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformDashboardAttentionItemDto(
    string Type,
    string Title,
    string Description,
    int Count,
    string Severity);
