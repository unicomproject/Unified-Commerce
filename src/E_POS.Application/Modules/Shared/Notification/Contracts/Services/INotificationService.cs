using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Contracts.Services;

public interface INotificationService
{
    Task<ApplicationResult<NotificationCreateResult>> CreateAsync(
        CreateNotificationEventRequest request,
        CancellationToken cancellationToken);
}