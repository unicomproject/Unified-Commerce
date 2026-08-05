using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Mappers;

public static class NotificationMapper
{
    public static NotificationInboxItemResponse ToInboxItem(NotificationInboxItemProjection row) => new()
    {
        Id = row.Id,
        MessageId = row.MessageId,
        EventCode = row.EventCode,
        SourceModule = row.SourceModule,
        SourceReferenceType = row.SourceReferenceType,
        SourceReferenceId = row.SourceReferenceId,
        Title = row.TitleText ?? string.Empty,
        Body = row.BodyText ?? string.Empty,
        LinkUrl = row.LinkUrl,
        Status = row.InboxStatus,
        IsRead = string.Equals(row.InboxStatus, "READ", StringComparison.OrdinalIgnoreCase),
        CreatedAt = row.CreatedAt,
        DeliveredAt = row.DeliveredAt,
        ReadAt = row.ReadAt
    };
}