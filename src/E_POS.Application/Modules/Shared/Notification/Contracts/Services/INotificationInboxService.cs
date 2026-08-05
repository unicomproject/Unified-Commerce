using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Contracts.Services;

public interface INotificationInboxService
{
    Task<ApplicationResult<NotificationInboxListResponse>> GetCustomerInboxAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<NotificationUnreadCountResponse>> GetCustomerUnreadCountAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<NotificationMarkReadResponse>> MarkCustomerInboxItemReadAsync(
        Guid tenantId,
        Guid customerId,
        Guid inboxItemId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllCustomerInboxItemsReadAsync(
        Guid tenantId,
        Guid customerId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}