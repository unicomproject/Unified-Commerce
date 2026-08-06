using E_POS.Api.Extensions;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingPolicies.PaymentAccess)]
[Route("api/v1/tenant-onboarding/payment-access/{accessToken}")]
public sealed class ManualPaymentAccessController : ControllerBase
{
    private readonly IManualPaymentService _service;
    public ManualPaymentAccessController(IManualPaymentService service) => _service = service;

    [HttpGet]
    public Task<IActionResult> Status(string accessToken, CancellationToken ct) =>
        Respond(() => _service.GetStatusAsync(accessToken, ct), "Manual payment status loaded.");

    [HttpGet("invoice")]
    public Task<IActionResult> Invoice(string accessToken, CancellationToken ct) =>
        Respond(() => _service.GetInvoiceAsync(accessToken, ct), "Invoice loaded.");

    [HttpPost("evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> Submit(string accessToken, [FromForm] ManualPaymentEvidenceForm form, CancellationToken ct)
    {
        if (form.Proof is null) return BadRequest(Error("manual_payment.proof_required", "Payment proof is required."));
        await using var stream = form.Proof.OpenReadStream();
        var request = new SubmitManualPaymentEvidenceRequest(form.PaymentMethod, form.BankOrTransactionReference,
            form.SubmittedAmount, form.CurrencyCode, form.PaymentDate, form.PayerNote, form.ExpectedVersion);
        var result = await _service.SubmitAsync(accessToken, request,
            new(stream, form.Proof.FileName, form.Proof.ContentType, form.Proof.Length),
            Request.Headers["Idempotency-Key"].ToString(), CorrelationId(), ct);
        return ToResult(result, "Manual payment evidence submitted.", StatusCodes.Status201Created);
    }

    [HttpPut("submissions/{paymentId:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> Update(string accessToken, Guid paymentId, [FromForm] ManualPaymentEvidenceForm form, CancellationToken ct)
    {
        if (form.Proof is null || form.ExpectedVersion is null)
            return BadRequest(Error("manual_payment.validation_failed", "Proof and expected version are required."));
        await using var stream = form.Proof.OpenReadStream();
        var request = new UpdateManualPaymentSubmissionRequest(form.PaymentMethod, form.BankOrTransactionReference,
            form.SubmittedAmount, form.CurrencyCode, form.PaymentDate, form.PayerNote, form.ExpectedVersion.Value);
        var result = await _service.UpdateAsync(accessToken, paymentId, request,
            new(stream, form.Proof.FileName, form.Proof.ContentType, form.Proof.Length),
            Request.Headers["Idempotency-Key"].ToString(), CorrelationId(), ct);
        return ToResult(result, "Manual payment submission updated.");
    }

    [HttpGet("history")]
    public Task<IActionResult> History(string accessToken, CancellationToken ct) =>
        Respond(() => _service.GetRecipientHistoryAsync(accessToken, ct), "Manual payment history loaded.");

    private async Task<IActionResult> Respond<T>(Func<Task<ApplicationResult<T>>> action, string message) =>
        ToResult(await action(), message);

    private IActionResult ToResult<T>(ApplicationResult<T> result, string message, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess && result.Value is not null)
            return StatusCode(successStatus, LegacyApiResponse<T>.Ok(message, result.Value));
        var code = result.Error.Code;
        var error = Error(code, result.Error.Message);
        if (code.EndsWith("access_invalid_or_expired", StringComparison.Ordinal)) return NotFound(error);
        if (code.EndsWith("not_found", StringComparison.Ordinal)) return NotFound(error);
        if (code.EndsWith("concurrency_conflict", StringComparison.Ordinal) ||
            code.EndsWith("idempotency_conflict", StringComparison.Ordinal) ||
            code.EndsWith("invalid_transition", StringComparison.Ordinal)) return Conflict(error);
        if (code.EndsWith("storage_unavailable", StringComparison.Ordinal)) return StatusCode(StatusCodes.Status503ServiceUnavailable, error);
        return BadRequest(error);
    }

    private Guid CorrelationId() => Guid.TryParse(HttpContext.TraceIdentifier, out var id) ? id : Guid.NewGuid();
    private object Error(string code, string message) => new { success = false, message, errorCode = code, errors = Array.Empty<object>(), traceId = HttpContext.TraceIdentifier };
}

public sealed class ManualPaymentEvidenceForm
{
    public string PaymentMethod { get; init; } = string.Empty;
    public string BankOrTransactionReference { get; init; } = string.Empty;
    public decimal SubmittedAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTimeOffset PaymentDate { get; init; }
    public string? PayerNote { get; init; }
    public long? ExpectedVersion { get; init; }
    public IFormFile? Proof { get; init; }
}
