using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;

namespace E_POS.Application.Modules.Shared.Notification.Channels;

public sealed class InAppNotificationChannelHandler : INotificationChannelHandler
{
    private const string ChannelCode = "IN_APP";
    private const string ChannelName = "In-app Notifications";
    private const string MessageType = "TRANSACTIONAL";
    private const string DeliveredStatus = "DELIVERED";
    private const string UnreadStatus = "UNREAD";
    private const int MaxMessageNumberLength = 80;

    private readonly INotificationRepository _repository;

    public InAppNotificationChannelHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public string ChannelType => NotificationChannelTypes.InApp;

    public async Task<NotificationChannelHandleResult> HandleAsync(
        NotificationChannelContext context,
        CancellationToken cancellationToken)
    {
        var channel = await _repository.GetOrCreateSystemChannelAsync(
            NotificationChannelTypes.InApp,
            ChannelCode,
            ChannelName,
            context.Now,
            cancellationToken);

        var messageNumber = BuildMessageNumber(context.EventNumber);
        if (await _repository.MessageExistsAsync(context.Request.TenantId, messageNumber, cancellationToken))
            return NotificationChannelHandleResult.None;

        var message = await _repository.AddMessageAsync(
            new NotificationMessageCreateData(
                context.Request.TenantId,
                context.EventId,
                channel.Id,
                messageNumber,
                MessageType,
                NotificationChannelTypes.InApp,
                context.Request.Recipient,
                context.Request.Content,
                NormalizePriority(context.Request.Priority),
                DeliveredStatus,
                context.Now,
                context.Now),
            cancellationToken);

        await _repository.AddInboxItemAsync(
            new NotificationInboxItemCreateData(
                context.Request.TenantId,
                message.Id,
                context.Request.Recipient,
                context.Request.Content,
                UnreadStatus,
                context.Now,
                context.Now),
            cancellationToken);

        return new NotificationChannelHandleResult(1);
    }

    private static string BuildMessageNumber(string eventNumber)
    {
        var value = $"{eventNumber}-INAPP";
        return value.Length <= MaxMessageNumberLength ? value : value[..MaxMessageNumberLength];
    }

    private static string NormalizePriority(string? priority)
    {
        var normalized = string.IsNullOrWhiteSpace(priority)
            ? NotificationPriorities.Normal
            : priority.Trim().ToUpperInvariant();

        return normalized is NotificationPriorities.Low or NotificationPriorities.Normal or NotificationPriorities.High or NotificationPriorities.Urgent
            ? normalized
            : NotificationPriorities.Normal;
    }
}