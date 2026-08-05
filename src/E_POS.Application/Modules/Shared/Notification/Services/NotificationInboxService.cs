using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Shared.Notification.Mappers;

namespace E_POS.Application.Modules.Shared.Notification.Services;

public sealed class NotificationInboxService : INotificationInboxService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    private readonly INotificationRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationInboxService(
        INotificationRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<NotificationInboxListResponse>> GetCustomerInboxAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null)
            return ApplicationResult<NotificationInboxListResponse>.Failure(contextError);

        var safePage = page <= 0 ? DefaultPage : page;
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var result = await _repository.GetCustomerInboxAsync(
            tenantId,
            customerId,
            safePage,
            safePageSize,
            cancellationToken);

        return ApplicationResult<NotificationInboxListResponse>.Success(new NotificationInboxListResponse
        {
            Items = result.Items.Select(NotificationMapper.ToInboxItem).ToList(),
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)safePageSize)
        });
    }

    public async Task<ApplicationResult<NotificationUnreadCountResponse>> GetCustomerUnreadCountAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null)
            return ApplicationResult<NotificationUnreadCountResponse>.Failure(contextError);

        var count = await _repository.GetCustomerUnreadCountAsync(tenantId, customerId, cancellationToken);
        return ApplicationResult<NotificationUnreadCountResponse>.Success(new NotificationUnreadCountResponse
        {
            UnreadCount = count
        });
    }

    public async Task<ApplicationResult<NotificationMarkReadResponse>> MarkCustomerInboxItemReadAsync(
        Guid tenantId,
        Guid customerId,
        Guid inboxItemId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null)
            return ApplicationResult<NotificationMarkReadResponse>.Failure(contextError);

        if (inboxItemId == Guid.Empty)
        {
            return ApplicationResult<NotificationMarkReadResponse>.Failure(
                Error("notifications.invalid_inbox_item", "A valid notification id is required."));
        }

        var item = await _repository.MarkCustomerInboxItemReadAsync(
            tenantId,
            customerId,
            inboxItemId,
            _dateTimeProvider.UtcNow,
            NormalizeIp(ipAddress),
            NormalizeUserAgent(userAgent),
            cancellationToken);

        if (item is null)
        {
            return ApplicationResult<NotificationMarkReadResponse>.Failure(
                Error("notifications.not_found", "Notification was not found."));
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return ApplicationResult<NotificationMarkReadResponse>.Success(new NotificationMarkReadResponse
        {
            Id = item.Id,
            Status = item.InboxStatus,
            ReadAt = item.ReadAt
        });
    }

    public async Task<ApplicationResult<NotificationMarkAllReadResponse>> MarkAllCustomerInboxItemsReadAsync(
        Guid tenantId,
        Guid customerId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null)
            return ApplicationResult<NotificationMarkAllReadResponse>.Failure(contextError);

        var now = _dateTimeProvider.UtcNow;
        var updatedCount = await _repository.MarkAllCustomerInboxItemsReadAsync(
            tenantId,
            customerId,
            now,
            NormalizeIp(ipAddress),
            NormalizeUserAgent(userAgent),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return ApplicationResult<NotificationMarkAllReadResponse>.Success(new NotificationMarkAllReadResponse
        {
            UpdatedCount = updatedCount,
            ReadAt = now
        });
    }

    private static ApplicationError? ValidateCustomerContext(Guid tenantId, Guid customerId) =>
        tenantId == Guid.Empty || customerId == Guid.Empty
            ? Error("notifications.invalid_customer_context", "A valid customer session is required.")
            : null;

    private static string? NormalizeIp(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 45)];

    private static string? NormalizeUserAgent(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ApplicationError Error(string code, string message) => new(code, message);
}