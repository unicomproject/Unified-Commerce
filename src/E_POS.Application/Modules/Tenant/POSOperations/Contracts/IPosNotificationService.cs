using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosNotificationService
{
    Task<ApplicationResult<PosNotificationInboxResponseDto>> GetInboxAsync(
        TenantRequestContext context,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<NotificationMarkReadResponse>> MarkReadAsync(
        TenantRequestContext context,
        Guid notificationId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllReadAsync(
        TenantRequestContext context,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
