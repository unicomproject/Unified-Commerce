using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/notifications")]
public sealed class PosNotificationsController : ControllerBase
{
    private readonly IPosNotificationService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosNotificationsController(
        IPosNotificationService service,
        ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(new { code = "pos_notifications.invalid_tenant_context" });

        var result = await _service.GetInboxAsync(context, page, pageSize, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { data = result.Value });

        return result.Error.Code == "pos_notifications.permission_denied"
            ? StatusCode(StatusCodes.Status403Forbidden, new { code = result.Error.Code, message = result.Error.Message })
            : BadRequest(new { code = result.Error.Code, message = result.Error.Message });
    }

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(new { code = "pos_notifications.invalid_tenant_context" });

        var result = await _service.MarkReadAsync(
            context,
            notificationId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { data = result.Value });

        return result.Error.Code switch
        {
            "pos_notifications.permission_denied" =>
                StatusCode(StatusCodes.Status403Forbidden, new { code = result.Error.Code, message = result.Error.Message }),
            "notifications.not_found" =>
                NotFound(new { code = result.Error.Code, message = result.Error.Message }),
            _ => BadRequest(new { code = result.Error.Code, message = result.Error.Message }),
        };
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(new { code = "pos_notifications.invalid_tenant_context" });

        var result = await _service.MarkAllReadAsync(
            context,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { data = result.Value });

        return result.Error.Code == "pos_notifications.permission_denied"
            ? StatusCode(StatusCodes.Status403Forbidden, new { code = result.Error.Code, message = result.Error.Message })
            : BadRequest(new { code = result.Error.Code, message = result.Error.Message });
    }

    [HttpPost("ticket")]
    public async Task<IActionResult> CreateTicket(
        [FromServices] IWebSocketNotificationTicketService ticketService,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(new { code = "pos_notifications.invalid_tenant_context" });

        var ticket = await ticketService.CreateTicketAsync(context, cancellationToken);
        return Ok(ticket);
    }
}
