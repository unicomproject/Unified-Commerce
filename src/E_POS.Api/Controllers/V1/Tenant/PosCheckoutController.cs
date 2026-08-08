using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/checkout")]
public sealed class PosCheckoutController : ControllerBase
{
    private readonly IPosCheckoutService _posCheckoutService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;
    private readonly ILogger<PosCheckoutController> _logger;

    public PosCheckoutController(
        IPosCheckoutService posCheckoutService,
        ITenantRequestContextFactory tenantRequestContextFactory,
        ILogger<PosCheckoutController>? logger = null)
    {
        _posCheckoutService = posCheckoutService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
        _logger = logger ?? NullLogger<PosCheckoutController>.Instance;
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
        var stopwatch = Stopwatch.StartNew();
        var correlation = PosPaymentCorrelation.FromIdempotencyKey(
            request.IdempotencyKey,
            HttpContext.TraceIdentifier);
        Response.Headers["X-POS-Correlation-Id"] = correlation;
        _logger.LogInformation(
            "event={Event} correlation={Correlation} device={Device} paymentMethod={PaymentMethod} lineCount={LineCount} tenderAmount={TenderAmount}",
            "pos_checkout_start_payment_received", correlation,
            Mask(request.DeviceId), request.PaymentMethod,
            request.Lines?.Count ?? 0, request.CashReceived);

        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            _logger.LogWarning(
                "event={Event} correlation={Correlation} outcome={Outcome} errorCode={ErrorCode}",
                "pos_checkout_context_validation", correlation, "rejected",
                "pos_checkout.invalid_tenant_context");
            return Unauthorized(CreateError(
                new ApplicationError("pos_checkout.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posCheckoutService.StartPaymentAsync(
            context,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            var errorResult = ToStartPaymentErrorResult(result.Error);
            _logger.LogWarning(
                "event={Event} correlation={Correlation} outcome={Outcome} errorCode={ErrorCode} httpStatus={HttpStatus} elapsedMs={ElapsedMs}",
                "pos_checkout_start_payment_completed", correlation, "rejected",
                result.Error.Code, StatusCodeOf(errorResult), stopwatch.ElapsedMilliseconds);
            return errorResult;
        }

        _logger.LogInformation(
            "event={Event} correlation={Correlation} outcome={Outcome} httpStatus={HttpStatus} elapsedMs={ElapsedMs}",
            "pos_checkout_start_payment_completed", correlation, "success",
            StatusCodes.Status200OK, stopwatch.ElapsedMilliseconds);
        return Ok(new { data = result.Value });
    }

    private static string Mask(Guid value) => value.ToString("N")[^8..];

    private static int StatusCodeOf(IActionResult result) => result switch
    {
        ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
        StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
        _ => StatusCodes.Status200OK
    };

    private IActionResult ToStartPaymentErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pos_checkout.permission_denied" or "pos_checkout.payment_permission_denied"
                => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_checkout.device_not_found" or "pos_checkout.customer_not_found" or "pos_checkout.variant_not_found"
                => NotFound(CreateError(error)),
            "pos_checkout.customer_inactive" or "pos_checkout.customer_blocked" or
            "pos_checkout.customer_deleted" or "pos_checkout.customer_not_eligible"
                => UnprocessableEntity(CreateError(error)),
            "pos_checkout.discount_application_not_found" => NotFound(CreateError(error)),
            "pos_checkout.discount_approval_required" or
            "pos_checkout.discount_application_expired" or
            "pos_checkout.discount_application_invalid" or
            "pos_checkout.discount_context_mismatch" or
            "pos_checkout.discount_policy_inactive" or
            "pos_checkout.discount_cart_changed"
                => Conflict(CreateError(error)),
            "pos_checkout.idempotency_conflict" or "pos_checkout.stock_conflict"
                => Conflict(CreateError(error)),
            "pos_checkout.persistence_failed"
                => StatusCode(StatusCodes.Status500InternalServerError, CreateError(error)),
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
            "pos_checkout.customer_inactive" or "pos_checkout.customer_blocked" or
            "pos_checkout.customer_deleted" or "pos_checkout.customer_not_eligible"
                => UnprocessableEntity(CreateError(error)),
            "pos_checkout.discount_application_not_found" => NotFound(CreateError(error)),
            "pos_checkout.discount_approval_required" or
            "pos_checkout.discount_application_expired" or
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
