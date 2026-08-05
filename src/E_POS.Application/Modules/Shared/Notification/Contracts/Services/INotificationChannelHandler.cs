using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Contracts.Services;

public interface INotificationChannelHandler
{
    string ChannelType { get; }

    Task<NotificationChannelHandleResult> HandleAsync(
        NotificationChannelContext context,
        CancellationToken cancellationToken);
}

public sealed record NotificationChannelContext(
    Guid EventId,
    string EventNumber,
    CreateNotificationEventRequest Request,
    DateTimeOffset Now);

public sealed record NotificationChannelHandleResult(int CreatedMessageCount)
{
    public static NotificationChannelHandleResult None { get; } = new(0);
}