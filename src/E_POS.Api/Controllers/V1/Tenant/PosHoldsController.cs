using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/holds")]
public sealed class PosHoldsController : ControllerBase
{
    private readonly IPosHoldService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosHoldsController(
        IPosHoldService service,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PosHoldListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHold(
        [FromBody] PosCreateHoldRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_holds.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.CreateHoldAsync(context, request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return StatusCode(StatusCodes.Status201Created, new { data = result.Value });
    }

    [HttpPost("{holdId:guid}/recall")]
    [ProducesResponseType(typeof(PosRecallHoldResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecallHold(
        Guid holdId,
        [FromBody] PosRecallHoldRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_holds.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.RecallHoldAsync(
            context, holdId, request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return ToErrorResult(result.Error);

        return Ok(new { data = result.Value });
    }

    [HttpDelete("{holdId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelHold(
        Guid holdId,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_holds.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.CancelHoldAsync(
            context, holdId, reason, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResult(result.Error);

        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PosHoldListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHolds(CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_holds.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.GetHoldsAsync(context, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error) => error.Code switch
    {
        "pos_holds.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "pos_checkout.device_not_found" or "pos_checkout.variant_not_found" or
        "pos_checkout.customer_not_found" => NotFound(CreateError(error)),
        "pos_holds.not_found" => NotFound(CreateError(error)),
        "pos_holds.idempotency_conflict" or "pos_holds.expired" or
        "pos_holds.not_recallable" or "pos_holds.not_cancellable" or
        "pos_holds.till_mismatch" => Conflict(CreateError(error)),
        "pos_holds.invalid_tenant_context" => Unauthorized(CreateError(error)),
        _ => BadRequest(CreateError(error))
    };

    private object CreateError(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
