using E_POS.Application.Common.Models;
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

    public PosNotificationService(IPosNotificationRepository repository)
    {
        _repository = repository;
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
}
