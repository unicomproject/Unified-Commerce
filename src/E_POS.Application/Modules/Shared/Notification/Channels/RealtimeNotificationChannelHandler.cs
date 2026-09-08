using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;

namespace E_POS.Application.Modules.Shared.Notification.Channels;

public sealed class RealtimeNotificationChannelHandler : INotificationChannelHandler
{
    private readonly IRealtimeNotificationPublisher _publisher;

    public RealtimeNotificationChannelHandler(IRealtimeNotificationPublisher publisher)
    {
        _publisher = publisher;
    }

    public string ChannelType => NotificationChannelTypes.Push;

    public async Task<NotificationChannelHandleResult> HandleAsync(
        NotificationChannelContext context,
        CancellationToken cancellationToken)
    {
        var recipient = context.Request.Recipient;
        if (recipient.RecipientType != NotificationRecipientTypes.TenantUser || recipient.TenantUserId is not { } tenantUserId)
            return NotificationChannelHandleResult.None;

        await _publisher.PublishToTenantUserAsync(
            tenantUserId,
            new RealtimeNotificationPayload(
                context.Request.EventCode,
                context.Request.Content.Title,
                context.Request.Content.Body,
                context.Request.Content.ActionUrl,
                context.Request.SourceReferenceId),
            cancellationToken);

        return NotificationChannelHandleResult.None;
    }
}
