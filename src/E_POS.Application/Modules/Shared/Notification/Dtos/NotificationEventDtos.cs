namespace E_POS.Application.Modules.Shared.Notification.Dtos;

public sealed class CreateNotificationEventRequest
{
    public Guid TenantId { get; init; }
    public string EventCode { get; init; } = string.Empty;
    public string EventName { get; init; } = string.Empty;
    public string SourceModule { get; init; } = string.Empty;
    public string? SourceReferenceType { get; init; }
    public Guid? SourceReferenceId { get; init; }
    public string? EventNumber { get; init; }
    public string Priority { get; init; } = string.Empty;
    public NotificationRecipientDto Recipient { get; init; } = new();
    public NotificationContentDto Content { get; init; } = new();
    public Guid? CreatedByTenantUserId { get; init; }
    public Guid? CreatedByPlatformUserId { get; init; }
}

public sealed class NotificationRecipientDto
{
    public string RecipientType { get; init; } = string.Empty;
    public Guid? PlatformUserId { get; init; }
    public Guid? TenantUserId { get; init; }
    public Guid? CustomerId { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
}

public sealed class NotificationContentDto
{
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
}

public sealed class NotificationCreateResult
{
    public Guid EventId { get; init; }
    public string EventNumber { get; init; } = string.Empty;
    public bool AlreadyExisted { get; init; }
    public int CreatedMessageCount { get; init; }
}