using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/checkout")]
public sealed class PosCheckoutController : ControllerBase
{
    private readonly IPosCheckoutService _posCheckoutService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosCheckoutController(
        IPosCheckoutService posCheckoutService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _posCheckoutService = posCheckoutService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost("summary")]
    [ProducesResponseType(typeof(PosCheckoutSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(
        [FromBody] PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_checkout.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posCheckoutService.GetSummaryAsync(
            context,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("start-payment")]
    [ProducesResponseType(typeof(PosCheckoutStartPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartPayment(
        [FromBody] PosCheckoutStartPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_checkout.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posCheckoutService.StartPaymentAsync(
            context,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToStartPaymentErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToStartPaymentErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pos_checkout.permission_denied" or "pos_checkout.payment_permission_denied"
                => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_checkout.device_not_found" or "pos_checkout.customer_not_found" or "pos_checkout.variant_not_found"
                => NotFound(CreateError(error)),
            "pos_checkout.discount_application_not_found" => NotFound(CreateError(error)),
            "pos_checkout.discount_approval_required" or
            "pos_checkout.discount_application_invalid" or
            "pos_checkout.discount_context_mismatch" or
            "pos_checkout.discount_policy_inactive" or
            "pos_checkout.discount_cart_changed"
                => Conflict(CreateError(error)),
            "pos_checkout.idempotency_conflict" or "pos_checkout.stock_conflict"
                => Conflict(CreateError(error)),
            "pos_checkout.till_session_not_open" or
            "pos_checkout.invalid_payment_method" or
            "pos_checkout.cash_received_required" or
            "pos_checkout.insufficient_cash" or
            "pos_checkout.insufficient_stock" or
            "pos_checkout.price_not_configured" or
            "pos_checkout.invalid_lines"
                => BadRequest(CreateError(error)),
            "pos_checkout.invalid_idempotency_key" or "pos_checkout.payment_provider_required"
                => BadRequest(CreateError(error)),
            "pos_checkout.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pos_checkout.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_checkout.device_not_found" or "pos_checkout.customer_not_found" or "pos_checkout.variant_not_found"
                => NotFound(CreateError(error)),
            "pos_checkout.discount_application_not_found" => NotFound(CreateError(error)),
            "pos_checkout.discount_approval_required" or
            "pos_checkout.discount_application_invalid" or
            "pos_checkout.discount_context_mismatch" or
            "pos_checkout.discount_policy_inactive" or
            "pos_checkout.discount_cart_changed"
                => Conflict(CreateError(error)),
            "pos_checkout.till_session_not_open" => BadRequest(CreateError(error)),
            "pos_checkout.invalid_tenant_context" => Unauthorized(CreateError(error)),
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
