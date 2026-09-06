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
}
