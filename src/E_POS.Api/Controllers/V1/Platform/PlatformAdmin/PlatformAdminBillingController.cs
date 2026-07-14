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
[Route("api/v1/platform-admin/billing")]
public sealed class PlatformAdminBillingController : ControllerBase
{
    private readonly IPlatformBillingService _service;
    public PlatformAdminBillingController(IPlatformBillingService service) => _service = service;

    [HttpGet("summary")]
    public Task<IActionResult> Summary([FromQuery] PlatformBillingQuery query, CancellationToken ct)
        => WithUser(id => _service.GetSummaryAsync(query, id, ct), "Billing summary loaded successfully.");

    [HttpGet("invoices")]
    public Task<IActionResult> Invoices([FromQuery] PlatformBillingQuery query, CancellationToken ct)
        => WithUser(id => _service.GetInvoicesAsync(query, id, ct), "Invoices loaded successfully.");

    [HttpGet("invoices/{invoiceId:guid}")]
    public Task<IActionResult> Invoice(Guid invoiceId, CancellationToken ct)
        => WithUser(id => _service.GetInvoiceAsync(invoiceId, id, ct), "Invoice loaded successfully.");

    [HttpGet("invoices/{invoiceId:guid}/payments")]
    public Task<IActionResult> Payments(Guid invoiceId, CancellationToken ct)
        => WithUser(id => _service.GetPaymentsAsync(invoiceId, id, ct), "Payment history loaded successfully.");

    [HttpGet("filter-options")]
    public Task<IActionResult> FilterOptions(CancellationToken ct)
        => WithUser(id => _service.GetFilterOptionsAsync(id, ct), "Billing filters loaded successfully.");

    [HttpPost("invoices/{invoiceId:guid}/issue")]
    public Task<IActionResult> Issue(Guid invoiceId, [FromBody] PlatformBillingTransitionRequest request, CancellationToken ct)
        => WithUser(id => _service.IssueAsync(invoiceId, request, id, ct), "Invoice issued successfully.");

    [HttpPost("invoices/{invoiceId:guid}/mark-paid")]
    public Task<IActionResult> MarkPaid(Guid invoiceId, [FromBody] PlatformBillingMarkPaidRequest request, CancellationToken ct)
        => WithUser(id => _service.MarkPaidAsync(invoiceId, request, id, ct), "Invoice marked paid successfully.");

    private async Task<IActionResult> WithUser<T>(Func<Guid, Task<ApplicationResult<T>>> action, string message)
    {
        var value = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId)) return Unauthorized(Error("platform_auth.invalid_session", "Invalid platform session."));
        var result = await action(userId);
        if (result.IsSuccess && result.Value is not null) return Ok(LegacyApiResponse<T>.Ok(message, result.Value));
        return result.Error.Code switch
        {
            "platform_billing.validation_failed" => BadRequest(Error(result.Error.Code, result.Error.Message)),
            "platform_billing.invoice_not_found" => NotFound(Error(result.Error.Code, result.Error.Message)),
            "platform_billing.invalid_transition" => Conflict(Error(result.Error.Code, result.Error.Message)),
            "platform_billing.concurrency_conflict" => Conflict(Error(result.Error.Code, result.Error.Message)),
            _ => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error.Code, result.Error.Message))
        };
    }

    private object Error(string code, string message) => new { success = false, message, errorCode = code, errors = Array.Empty<object>(), traceId = HttpContext.TraceIdentifier };
}
