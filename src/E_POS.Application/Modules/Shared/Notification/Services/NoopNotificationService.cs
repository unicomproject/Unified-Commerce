using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Services;

public sealed class NoopNotificationService : INotificationService
{
    public static NoopNotificationService Instance { get; } = new();

    private NoopNotificationService()
    {
    }

    public Task<ApplicationResult<NotificationCreateResult>> CreateAsync(
        CreateNotificationEventRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(ApplicationResult<NotificationCreateResult>.Success(new NotificationCreateResult
        {
            EventId = Guid.Empty,
            EventNumber = request.EventNumber ?? string.Empty,
            AlreadyExisted = false,
            CreatedMessageCount = 0
        }));
}