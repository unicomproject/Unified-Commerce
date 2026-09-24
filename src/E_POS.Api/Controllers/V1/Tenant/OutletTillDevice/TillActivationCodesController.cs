using E_POS.Api.Common;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.OutletTillDevice;

[ApiController, Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin/tills/{tillId:guid}/activation-codes")]
public sealed class TillActivationCodesController(ITenantRequestContextFactory contexts, ITillActivationCodeService service) : ControllerBase
{
    [HttpPost, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Issue(Guid tillId, CancellationToken ct)
    {
        if (!contexts.TryCreate(User, out var actor)) return Unauthorized();
        try { return Ok(new { data = await service.IssueAsync(actor, tillId, ct) }); }
        catch (UnauthorizedAccessException) { return StatusCode(403, new { code = "till.access_denied", message = "Till access denied." }); }
        catch (InvalidOperationException error) { return Conflict(new { code = "till.activation_unavailable", message = error.Message }); }
    }
}
