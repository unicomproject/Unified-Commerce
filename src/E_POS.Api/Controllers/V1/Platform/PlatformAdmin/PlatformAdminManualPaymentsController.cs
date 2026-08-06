using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform-admin/billing/manual-payments")]
public sealed class PlatformAdminManualPaymentsController : ControllerBase
{
    private readonly IManualPaymentService _service;
    public PlatformAdminManualPaymentsController(IManualPaymentService service) => _service = service;

    [HttpGet]
    public Task<IActionResult> Queue([FromQuery] ManualPaymentQueueQuery query, CancellationToken ct) =>
        WithUser(id => _service.GetQueueAsync(query, id, ct), "Manual payment queue loaded.");

    [HttpGet("{paymentId:guid}")]
    public Task<IActionResult> Detail(Guid paymentId, CancellationToken ct) =>
        WithUser(id => _service.GetDetailAsync(paymentId, id, ct), "Manual payment loaded.");

    [HttpGet("{paymentId:guid}/proof/{evidenceId:guid}")]
    public async Task<IActionResult> Proof(Guid paymentId, Guid evidenceId, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized(Error("platform_auth.invalid_session", "Invalid platform session."));
        var result = await _service.OpenProofAsync(paymentId, evidenceId, userId, ct);
        if (result.IsSuccess && result.Value is not null)
        {
            Response.Headers.CacheControl = "no-store, private";
            Response.Headers.Pragma = "no-cache";
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: false);
        }
        return MapError(result.Error);
    }

    [HttpPost("{paymentId:guid}/review")]
    public Task<IActionResult> Review(Guid paymentId, [FromBody] ManualPaymentReviewRequest request, CancellationToken ct) =>
        WithUser(id => _service.ReviewAsync(paymentId, request, Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(), id, ct), "Manual payment review completed.");

    [HttpGet("{paymentId:guid}/history")]
    public Task<IActionResult> History(Guid paymentId, CancellationToken ct) =>
        WithUser(id => _service.GetAdminHistoryAsync(paymentId, id, ct), "Manual payment history loaded.");

    [HttpPost("{paymentId:guid}/notification/resend")]
    public Task<IActionResult> Resend(Guid paymentId, [FromBody] ResendPaymentNotificationRequest request, CancellationToken ct) =>
        WithUser(id => _service.ResendNotificationAsync(paymentId, request,
            Request.Headers["Idempotency-Key"].ToString(), CorrelationId(), id, ct), "Payment notification queued.");

    private async Task<IActionResult> WithUser<T>(Func<Guid, Task<ApplicationResult<T>>> action, string message)
    {
        if (!TryUser(out var userId)) return Unauthorized(Error("platform_auth.invalid_session", "Invalid platform session."));
        var result = await action(userId);
        if (result.IsSuccess && result.Value is not null) return Ok(LegacyApiResponse<T>.Ok(message, result.Value));
        return MapError(result.Error);
    }

    private IActionResult MapError(ApplicationError error)
    {
        var body = Error(error.Code, error.Message);
        if (error.Code.EndsWith("access_denied", StringComparison.Ordinal) || error.Code.EndsWith("proof_access_denied", StringComparison.Ordinal)) return StatusCode(403, body);
        if (error.Code.EndsWith("not_found", StringComparison.Ordinal)) return NotFound(body);
        if (error.Code.EndsWith("rate_limited", StringComparison.Ordinal)) return StatusCode(429, body);
        if (error.Code.EndsWith("storage_unavailable", StringComparison.Ordinal)) return StatusCode(503, body);
        if (error.Code.EndsWith("concurrency_conflict", StringComparison.Ordinal) || error.Code.EndsWith("idempotency_conflict", StringComparison.Ordinal) || error.Code.EndsWith("invalid_transition", StringComparison.Ordinal)) return Conflict(body);
        return BadRequest(body);
    }

    private bool TryUser(out Guid id)
    {
        var value = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out id);
    }
    private Guid CorrelationId() => Guid.TryParse(HttpContext.TraceIdentifier, out var id) ? id : Guid.NewGuid();
    private object Error(string code, string message) => new { success = false, message, errorCode = code, errors = Array.Empty<object>(), traceId = HttpContext.TraceIdentifier };
}
