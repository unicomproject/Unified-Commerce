using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosNotificationInboxResponseDto(
    IReadOnlyList<NotificationInboxItemResponse> Notifications,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
