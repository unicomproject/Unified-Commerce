using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Notifications;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant/notifications")]
public sealed class TenantNotificationsController : ControllerBase
{
    private readonly INotificationInboxService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantNotificationsController(
        INotificationInboxService service,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return InvalidSession();

        return ToActionResult(
            await _service.GetTenantUserInboxAsync(
                context.TenantId,
                context.UserId,
                page,
                pageSize,
                cancellationToken),
            "Notifications retrieved successfully.");
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return InvalidSession();

        return ToActionResult(
            await _service.GetTenantUserUnreadCountAsync(
                context.TenantId,
                context.UserId,
                cancellationToken),
            "Unread notification count retrieved successfully.");
    }

    [HttpPut("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return InvalidSession();

        return ToActionResult(
            await _service.MarkTenantUserInboxItemReadAsync(
                context.TenantId,
                context.UserId,
                notificationId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken),
            "Notification marked as read.");
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return InvalidSession();

        return ToActionResult(
            await _service.MarkAllTenantUserInboxItemsReadAsync(
                context.TenantId,
                context.UserId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken),
            "Notifications marked as read.");
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                success = true,
                message = successMessage,
                data = result.Value
            });
        }

        var error = CreateError(result.Error);
        return result.Error.Code switch
        {
            "notifications.invalid_tenant_user_context" => Unauthorized(error),
            "notifications.not_found" => NotFound(error),
            _ => BadRequest(error)
        };
    }

    private IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "notifications.invalid_tenant_user_context",
            "A valid staff session is required.")));

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
