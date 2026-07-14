using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos")]
public sealed class PosHomeController : ControllerBase
{
    private readonly IPosHomeDashboardService _posHomeDashboardService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosHomeController(
        IPosHomeDashboardService posHomeDashboardService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _posHomeDashboardService = posHomeDashboardService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("home")]
    [ProducesResponseType(typeof(PosHomeDashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPosHome(
        [FromQuery] Guid? outletId,
        [FromQuery] Guid? tillId,
        [FromQuery] Guid? deviceId,
        [FromQuery] string? deviceFingerprint,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_home.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posHomeDashboardService.GetPosHomeAsync(
            context,
            outletId,
            tillId,
            deviceId,
            deviceFingerprint,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pos_home_dashboard.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new
        {
            code = error.Code,
            message = error.Message,
            details = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };
    }
}
