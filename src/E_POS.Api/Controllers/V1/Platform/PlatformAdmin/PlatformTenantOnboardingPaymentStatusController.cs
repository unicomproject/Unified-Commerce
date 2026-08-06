using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform-admin/tenant-onboarding/tenants")]
public sealed class PlatformTenantOnboardingPaymentStatusController : ControllerBase
{
    private readonly IManualPaymentService _service;
    public PlatformTenantOnboardingPaymentStatusController(IManualPaymentService service) => _service = service;

    [HttpGet("{tenantId:guid}/payment-status")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken ct)
    {
        var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var userId)) return Unauthorized();
        var result = await _service.GetTenantPaymentStatusAsync(tenantId, userId, ct);
        if (result.IsSuccess && result.Value is not null)
            return Ok(LegacyApiResponse<ManualPaymentDetailResponse>.Ok("Tenant payment status loaded.", result.Value));
        var body = new { success = false, message = result.Error.Message, errorCode = result.Error.Code,
            errors = Array.Empty<object>(), traceId = HttpContext.TraceIdentifier };
        return result.Error.Code.EndsWith("access_denied", StringComparison.Ordinal) ? StatusCode(403, body) : NotFound(body);
    }
}
