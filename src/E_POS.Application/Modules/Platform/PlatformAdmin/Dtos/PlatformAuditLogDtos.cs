namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed class PlatformAuditLogListQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public string? Action { get; set; }

    public Guid? ActorPlatformUserId { get; set; }

    public string? EntityType { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }
}

public sealed record PlatformAuditLogActorDto(
    Guid? PlatformUserId,
    string? Email);

public sealed record PlatformAuditLogListItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    PlatformAuditLogActorDto Actor,
    string Action,
    string Area,
    string EntityType,
    Guid? EntityId,
    string Summary,
    string? IpAddress,
    string? UserAgent);

public sealed record PlatformAuditLogListResponse(
    string AuditScope,
    string AuditScopeDescription,
    IReadOnlyList<PlatformAuditLogListItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

