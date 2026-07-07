using System.Security.Claims;
using E_POS.Application.Modules.TenantAdministration.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.TenantAdministration;

[ApiController]
[Route("api/v1/tenant-admin")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminContextController : ControllerBase
{
    private readonly ITenantAdminContextService _contextService;

    public TenantAdminContextController(ITenantAdminContextService contextService)
    {
        _contextService = contextService;
    }

    [HttpGet("context")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContext(CancellationToken cancellationToken)
    {
        var tenantUserIdValue = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = User.FindFirstValue("tenant_id");

        if (!Guid.TryParse(tenantUserIdValue, out var tenantUserId)
            || !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized(new
            {
                code = "tenant_admin_context.unauthorized",
                message = "Invalid tenant identity claims.",
                traceId = HttpContext.TraceIdentifier,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        var result = await _contextService.GetContextAsync(tenantUserId, tenantId, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            // Wrap in { data: ... } envelope as expected by the Flutter client
            return Ok(new { data = result.Value });
        }

        return NotFound(new
        {
            code = result.Error.Code,
            message = result.Error.Message,
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
