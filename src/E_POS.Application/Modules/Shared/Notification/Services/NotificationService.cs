using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Constants;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.Shared.Notification.Services;

public sealed class NotificationService : INotificationService
{
    private const int MaxEventNumberLength = 80;
    private const int MaxEventCodeLength = 120;
    private const int MaxEventNameLength = 180;
    private const int MaxSourceModuleLength = 120;
    private const int MaxSourceReferenceTypeLength = 120;
    private const int MaxTitleLength = 250;
    private const int MaxBodyLength = 700;
    private const int MaxActionUrlLength = 700;

    private readonly INotificationRepository _repository;
    private readonly IReadOnlyList<INotificationChannelHandler> _channelHandlers;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationService(
        INotificationRepository repository,
        IEnumerable<INotificationChannelHandler> channelHandlers,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _channelHandlers = channelHandlers.ToList();
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<NotificationCreateResult>> CreateAsync(
        CreateNotificationEventRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return ApplicationResult<NotificationCreateResult>.Failure(validationError);

        var now = _dateTimeProvider.UtcNow;
        var priority = NormalizePriority(request.Priority);
        var eventNumber = NormalizeEventNumber(request);
        var existing = await _repository.FindEventByNumberAsync(
            request.TenantId,
            eventNumber,
            cancellationToken);

        if (existing is not null)
        {
            return ApplicationResult<NotificationCreateResult>.Success(new NotificationCreateResult
            {
                EventId = existing.Id,
                EventNumber = existing.EventNumber,
                AlreadyExisted = true,
                CreatedMessageCount = 0
            });
        }

        var eventType = await _repository.GetOrCreateSystemEventTypeAsync(
            request.EventCode.Trim(),
            request.EventName.Trim(),
            request.SourceModule.Trim(),
            priority,
            now,
            cancellationToken);

        var notificationEvent = await _repository.AddEventAsync(
            new NotificationEventCreateData(
                request.TenantId,
                eventType.Id,
                eventNumber,
                request.EventCode.Trim(),
                request.SourceModule.Trim(),
                TrimToNull(request.SourceReferenceType),
                request.SourceReferenceId,
                priority,
                now,
                request.CreatedByTenantUserId,
                request.CreatedByPlatformUserId),
            cancellationToken);

        var totalMessages = 0;
        foreach (var handler in _channelHandlers)
        {
            var result = await handler.HandleAsync(
                new NotificationChannelContext(notificationEvent.Id, eventNumber, request, now),
                cancellationToken);
            totalMessages += result.CreatedMessageCount;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return ApplicationResult<NotificationCreateResult>.Success(new NotificationCreateResult
        {
            EventId = notificationEvent.Id,
            EventNumber = eventNumber,
            AlreadyExisted = false,
            CreatedMessageCount = totalMessages
        });
    }

    private static ApplicationError? Validate(CreateNotificationEventRequest request)
    {
        if (request.TenantId == Guid.Empty)
            return Error("notification.invalid_tenant", "A valid tenant is required for notification events.");

        if (!HasLength(request.EventCode, 1, MaxEventCodeLength))
            return Error("notification.invalid_event_code", "A valid notification event code is required.");

        if (!HasLength(request.EventName, 1, MaxEventNameLength))
            return Error("notification.invalid_event_name", "A valid notification event name is required.");

        if (!HasLength(request.SourceModule, 1, MaxSourceModuleLength))
            return Error("notification.invalid_source_module", "A valid notification source module is required.");

        if (!string.IsNullOrWhiteSpace(request.SourceReferenceType) && request.SourceReferenceType.Trim().Length > MaxSourceReferenceTypeLength)
            return Error("notification.invalid_source_reference", "Notification source reference type is too long.");

        if (!string.IsNullOrWhiteSpace(request.EventNumber) && request.EventNumber.Trim().Length > MaxEventNumberLength)
            return Error("notification.invalid_event_number", "Notification event number is too long.");

        if (!IsValidRecipient(request.Recipient))
            return Error("notification.invalid_recipient", "A valid notification recipient is required.");

        if (!HasLength(request.Content.Title, 1, MaxTitleLength))
            return Error("notification.invalid_title", "A valid notification title is required.");

        if (!HasLength(request.Content.Body, 1, MaxBodyLength))
            return Error("notification.invalid_body", "A valid notification body is required.");

        if (!string.IsNullOrWhiteSpace(request.Content.ActionUrl) && request.Content.ActionUrl.Trim().Length > MaxActionUrlLength)
            return Error("notification.invalid_action_url", "Notification action URL is too long.");

        return null;
    }

    private static bool IsValidRecipient(NotificationRecipientDto recipient)
    {
        var type = recipient.RecipientType.Trim().ToUpperInvariant();
        return type switch
        {
            NotificationRecipientTypes.Customer => recipient.CustomerId.HasValue && recipient.CustomerId.Value != Guid.Empty && recipient.PlatformUserId is null && recipient.TenantUserId is null,
            NotificationRecipientTypes.TenantUser => recipient.TenantUserId.HasValue && recipient.TenantUserId.Value != Guid.Empty && recipient.PlatformUserId is null && recipient.CustomerId is null,
            NotificationRecipientTypes.PlatformUser => recipient.PlatformUserId.HasValue && recipient.PlatformUserId.Value != Guid.Empty && recipient.TenantUserId is null && recipient.CustomerId is null,
            _ => false
        };
    }

    private static string NormalizePriority(string? priority)
    {
        var normalized = string.IsNullOrWhiteSpace(priority)
            ? NotificationPriorities.Normal
            : priority.Trim().ToUpperInvariant();

        return normalized is NotificationPriorities.Low or NotificationPriorities.Normal or NotificationPriorities.High or NotificationPriorities.Urgent
            ? normalized
            : NotificationPriorities.Normal;
    }

    private static string NormalizeEventNumber(CreateNotificationEventRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.EventNumber))
            return request.EventNumber.Trim().ToUpperInvariant();

        var reference = request.SourceReferenceId?.ToString("N") ?? Guid.NewGuid().ToString("N");
        var compactCode = request.EventCode
            .Trim()
            .ToUpperInvariant()
            .Replace('.', '-')
            .Replace('_', '-');
        var value = $"NTF-{compactCode}-{reference}";
        return value.Length <= MaxEventNumberLength ? value : value[..MaxEventNumberLength];
    }

    private static bool HasLength(string? value, int min, int max)
    {
        var length = value?.Trim().Length ?? 0;
        return length >= min && length <= max;
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ApplicationError Error(string code, string message) => new(code, message);
}