using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Shared.Notification.Mappers;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosNotificationService : IPosNotificationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private readonly IPosNotificationRepository _repository;
    // Mark-read shares the tenant user's inbox rows one-for-one with the tenant-admin
    // surface (same table, scoped by tenant + user), so it reuses that service rather
    // than duplicating the mutation. Only the read (GetInboxAsync) side needs POS-specific
    // source filtering, since that's what decides which rows a cashier sees at all.
    private readonly INotificationInboxService _inboxService;

    public PosNotificationService(
        IPosNotificationRepository repository,
        INotificationInboxService inboxService)
    {
        _repository = repository;
        _inboxService = inboxService;
    }

    public async Task<ApplicationResult<PosNotificationInboxResponseDto>> GetInboxAsync(
        TenantRequestContext context,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Notifications.View))
        {
            return ApplicationResult<PosNotificationInboxResponseDto>.Failure(
                new ApplicationError("pos_notifications.permission_denied",
                    "You do not have permission to view POS notifications."));
        }

        var allowedSources = PosNotificationSourceAccess.Resolve(context);
        var safePage = Math.Max(1, page);
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var result = await _repository.GetTenantUserInboxAsync(
            context.TenantId, context.UserId, allowedSources, safePage,
            safePageSize, cancellationToken);
        var unreadCount = await _repository.GetTenantUserUnreadCountAsync(
            context.TenantId, context.UserId, allowedSources, cancellationToken);

        return ApplicationResult<PosNotificationInboxResponseDto>.Success(
            new PosNotificationInboxResponseDto(
                result.Items.Select(NotificationMapper.ToInboxItem).ToList(),
                unreadCount,
                safePage,
                safePageSize,
                result.TotalCount,
                result.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(result.TotalCount / (double)safePageSize)));
    }

    public async Task<ApplicationResult<NotificationMarkReadResponse>> MarkReadAsync(
        TenantRequestContext context,
        Guid notificationId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Notifications.View))
        {
            return ApplicationResult<NotificationMarkReadResponse>.Failure(
                new ApplicationError("pos_notifications.permission_denied",
                    "You do not have permission to manage POS notifications."));
        }

        return await _inboxService.MarkTenantUserInboxItemReadAsync(
            context.TenantId, context.UserId, notificationId, ipAddress, userAgent, cancellationToken);
    }

    public async Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllReadAsync(
        TenantRequestContext context,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Notifications.View))
        {
            return ApplicationResult<NotificationMarkAllReadResponse>.Failure(
                new ApplicationError("pos_notifications.permission_denied",
                    "You do not have permission to manage POS notifications."));
        }

        return await _inboxService.MarkAllTenantUserInboxItemsReadAsync(
            context.TenantId, context.UserId, ipAddress, userAgent, cancellationToken);
    }
}
