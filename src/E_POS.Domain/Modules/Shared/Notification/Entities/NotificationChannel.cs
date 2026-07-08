using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Notification.Entities;

public class NotificationChannel : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public string ChannelCode { get; protected set; } = string.Empty;
    public string ChannelName { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public bool IsSystemChannel { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public string? ProviderName { get; protected set; }
    public string? ProviderConfigJson { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public DateTimeOffset? ArchivedAt { get; protected set; }
    public Guid? ArchivedByPlatformUserId { get; protected set; }
    public Guid? ArchivedByTenantUserId { get; protected set; }
}

