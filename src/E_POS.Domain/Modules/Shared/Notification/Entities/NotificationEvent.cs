using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid NotificationEventTypeId { get; protected set; }
    public string EventNumber { get; protected set; } = string.Empty;
    public string EventCode { get; protected set; } = string.Empty;
    public string? SourceModule { get; protected set; }
    public string? SourceReferenceType { get; protected set; }
    public Guid? SourceReferenceId { get; protected set; }
    public string Priority { get; protected set; } = string.Empty;
    public string EventStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ScheduledAt { get; protected set; }
    public DateTimeOffset? ProcessingStartedAt { get; protected set; }
    public DateTimeOffset? ProcessedAt { get; protected set; }
    public DateTimeOffset? FailedAt { get; protected set; }
    public string? FailureReason { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }

    public static NotificationEvent CreateProcessed(
        Guid tenantId,
        Guid notificationEventTypeId,
        string eventNumber,
        string eventCode,
        string? sourceModule,
        string? sourceReferenceType,
        Guid? sourceReferenceId,
        string priority,
        DateTimeOffset now,
        Guid? createdByTenantUserId,
        Guid? createdByPlatformUserId)
    {
        return new NotificationEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NotificationEventTypeId = notificationEventTypeId,
            EventNumber = eventNumber.Trim().ToUpperInvariant(),
            EventCode = eventCode.Trim(),
            SourceModule = string.IsNullOrWhiteSpace(sourceModule) ? null : sourceModule.Trim(),
            SourceReferenceType = string.IsNullOrWhiteSpace(sourceReferenceType) ? null : sourceReferenceType.Trim(),
            SourceReferenceId = sourceReferenceId,
            Priority = priority.Trim().ToUpperInvariant(),
            EventStatus = "PROCESSED",
            ProcessedAt = now,
            CreatedAt = now,
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedByPlatformUserId = createdByPlatformUserId
        };
    }
}