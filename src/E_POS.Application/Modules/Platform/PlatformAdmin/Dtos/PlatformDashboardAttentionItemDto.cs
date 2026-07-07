namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformDashboardAttentionItemDto(
    string Type,
    string Title,
    string Description,
    int Count,
    string Severity);

