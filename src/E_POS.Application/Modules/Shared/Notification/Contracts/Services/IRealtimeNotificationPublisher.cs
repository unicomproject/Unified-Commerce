namespace E_POS.Application.Modules.Shared.Notification.Contracts.Services;

public interface IRealtimeNotificationPublisher
{
    Task PublishToTenantUserAsync(
        Guid tenantUserId,
        RealtimeNotificationPayload payload,
        CancellationToken cancellationToken);
}

public sealed record RealtimeNotificationPayload(
    string EventCode,
    string Title,
    string Body,
    string? ActionUrl,
    Guid? SourceReferenceId);
