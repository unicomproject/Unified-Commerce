using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/discounts")]
public sealed class PosDiscountsController : ControllerBase
{
    private readonly IPosDiscountService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosDiscountsController(IPosDiscountService service, ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PosDiscountCatalogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List([FromQuery] PosDiscountCatalogQueryDto query, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(Error(new ApplicationError("pos_discounts.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.ListAvailableAsync(context, query, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.Code switch
            {
                "pos_discounts.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.device_not_found" or "pos_discounts.till_not_assigned" => NotFound(Error(result.Error)),
                "pos_discounts.device_not_trusted" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                _ => BadRequest(Error(result.Error))
            };
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("{applicationId:guid}/cancel")]
    [ProducesResponseType(typeof(PosDiscountCancelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid applicationId, [FromBody] PosDiscountCancelRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_discounts.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.CancelAsync(context, applicationId, request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return result.Error.Code switch
            {
                "pos_discounts.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.application_not_found" => NotFound(Error(result.Error)),
                "pos_discounts.application_not_cancellable" or "pos_discounts.application_context_mismatch"
                    => Conflict(Error(result.Error)),
                _ => BadRequest(Error(result.Error))
            };
        }
        return Ok(new { data = result.Value });
    }

    [HttpPost("validate")]
    [ProducesResponseType(typeof(PosDiscountValidationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate(
        [FromBody] PosDiscountValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(Error(new ApplicationError("pos_discounts.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.ValidateAsync(context, request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return result.Error.Code switch
            {
                "pos_discounts.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.discount_not_found" or "pos_discounts.device_not_found" => NotFound(Error(result.Error)),
                "pos_discounts.device_not_trusted" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.manual_configuration_not_found" or "pos_discounts.policy_not_applicable"
                    or "pos_discounts.scope_mismatch" => UnprocessableEntity(Error(result.Error)),
                _ => BadRequest(Error(result.Error))
            };
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("apply")]
    [ProducesResponseType(typeof(PosDiscountApplyResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Apply(
        [FromBody] PosDiscountValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(Error(new ApplicationError("pos_discounts.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.ApplyAsync(context, request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return result.Error.Code switch
            {
                "pos_discounts.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.discount_not_found" or "pos_discounts.device_not_found" => NotFound(Error(result.Error)),
                "pos_discounts.device_not_trusted" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.idempotency_conflict" => Conflict(Error(result.Error)),
                "pos_discounts.manual_configuration_not_found" or "pos_discounts.policy_not_applicable"
                    or "pos_discounts.scope_mismatch" or "pos_discounts.target_required"
                    or "pos_discounts.target_not_in_cart" => UnprocessableEntity(Error(result.Error)),
                _ => BadRequest(Error(result.Error))
            };
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("{applicationId:guid}/approve")]
    [ProducesResponseType(typeof(PosDiscountDecisionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decide(
        Guid applicationId,
        [FromBody] PosDiscountDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(Error(new ApplicationError("pos_discounts.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _service.DecideAsync(context, applicationId, request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return result.Error.Code switch
            {
                "pos_discounts.approval_permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.self_approval_forbidden" => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error)),
                "pos_discounts.application_not_found" => NotFound(Error(result.Error)),
                "pos_discounts.application_not_pending" or "pos_discounts.application_expired" => Conflict(Error(result.Error)),
                _ => BadRequest(Error(result.Error))
            };
        }

        return Ok(new { data = result.Value });
    }

    private object Error(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
