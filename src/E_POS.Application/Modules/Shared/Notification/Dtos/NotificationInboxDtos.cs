namespace E_POS.Application.Modules.Shared.Notification.Dtos;

public sealed class NotificationInboxListResponse
{
    public IReadOnlyList<NotificationInboxItemResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class NotificationInboxItemResponse
{
    public Guid Id { get; init; }
    public Guid MessageId { get; init; }
    public string EventCode { get; init; } = string.Empty;
    public string? SourceModule { get; init; }
    public string? SourceReferenceType { get; init; }
    public Guid? SourceReferenceId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? LinkUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
}

public sealed class NotificationUnreadCountResponse
{
    public int UnreadCount { get; init; }
}

public sealed class NotificationMarkReadResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset? ReadAt { get; init; }
}

public sealed class NotificationMarkAllReadResponse
{
    public int UpdatedCount { get; init; }
    public DateTimeOffset ReadAt { get; init; }
}