using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tills")]
public sealed class PosTillsController : ControllerBase
{
    private readonly IPosTillSessionService _posTillSessionService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosTillsController(
        IPosTillSessionService posTillSessionService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _posTillSessionService = posTillSessionService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("current-session")]
    [ProducesResponseType(typeof(CurrentTillSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentSession(
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("till_session.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posTillSessionService.GetCurrentSessionAsync(
            context,
            deviceId,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("open")]
    [ProducesResponseType(typeof(CurrentTillSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OpenTill(
        [FromBody] OpenTillRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("till_session.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posTillSessionService.OpenTillAsync(
            context,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("close")]
    [ProducesResponseType(typeof(CloseTillResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseTill(
        [FromBody] CloseTillRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("till_session.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posTillSessionService.CloseTillAsync(
            context,
            request,
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
            "till_session.not_found" or "till_session.device_not_found" or "till_session.till_not_assigned" or "till_session.till_not_found" or "till_session.not_open" => NotFound(CreateError(error)),
            "till_session.permission_denied" or "till_session.device_not_trusted" or "till_session.till_mismatch" or "till_session.till_inactive" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "till_session.already_open" => Conflict(CreateError(error)),
            "till_session.mismatch_reason_required" or "till_session.invalid_counted_cash" or "till_session.invalid_expected_cash" => BadRequest(CreateError(error)),
            "till_session.invalid_tenant_context" => Unauthorized(CreateError(error)),
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
