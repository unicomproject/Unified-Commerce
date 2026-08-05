using System.Security.Claims;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Notifications;

[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v1/ecommerce/storefront/notifications")]
public sealed class StorefrontNotificationsController : ControllerBase
{
    private readonly INotificationInboxService _service;

    public StorefrontNotificationsController(INotificationInboxService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.GetCustomerInboxAsync(
                tenantId,
                customerId,
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
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.GetCustomerUnreadCountAsync(
                tenantId,
                customerId,
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
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.MarkCustomerInboxItemReadAsync(
                tenantId,
                customerId,
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
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.MarkAllCustomerInboxItemsReadAsync(
                tenantId,
                customerId,
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
            "notifications.invalid_customer_context" => Unauthorized(error),
            "notifications.not_found" => NotFound(error),
            _ => BadRequest(error)
        };
    }

    private bool TryGetCustomerContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    private IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "notifications.invalid_customer_context",
            "A valid customer session is required.")));

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}