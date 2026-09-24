using E_POS.Api.Common;
using E_POS.Api.Realtime;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers.V1.Tenant.HardwareCash;

[ApiController, Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin/tills/{tillId:guid}/hardware-scans")]
public sealed class TillDiagnosticsController(ITenantRequestContextFactory contexts,
    ITillDiagnosticService service, TillDiagnosticDispatcher dispatcher) : ControllerBase
{
    public sealed record StartRequest(Guid RequestId);
    [HttpGet("runtime")]
    public Task<IActionResult> Runtime(Guid tillId, CancellationToken ct) => Execute(async actor =>
        await dispatcher.StatusAsync(actor, tillId, ct));
    [HttpGet("{scanId:guid}")]
    public Task<IActionResult> Get(Guid tillId, Guid scanId, CancellationToken ct) => Execute(async actor =>
        await service.GetAsync(actor, tillId, scanId, ct));
    [HttpPost]
    public Task<IActionResult> Start(Guid tillId, StartRequest request, CancellationToken ct) => Execute(async actor =>
        await dispatcher.StartAsync(actor, tillId, request.RequestId, ct));
    private async Task<IActionResult> Execute(Func<E_POS.Application.Common.Models.TenantRequestContext, Task<object?>> action)
    {
        if (!contexts.TryCreate(User, out var actor)) return Unauthorized();
        try { var data = await action(actor); return data == null ? NotFound() : Ok(new { data }); }
        catch (UnauthorizedAccessException) { return StatusCode(403, new { code = "hardware.scan_access_denied" }); }
        catch (ArgumentException) { return BadRequest(new { code = "hardware.scan_invalid_request" }); }
        catch (InvalidOperationException) { return Conflict(new { code = "hardware.scan_conflict", message = "Check active scan, device assignment and configuration." }); }
    }
}
