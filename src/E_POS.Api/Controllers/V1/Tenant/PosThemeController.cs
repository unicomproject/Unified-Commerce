using E_POS.Api.Common;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/theme")]
public sealed class PosThemeController : ControllerBase
{
    private readonly IPosThemeService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosThemeController(
        IPosThemeService service,
        ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PosThemeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized();

        var result = await _service.GetAsync(context, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(result.Value)
            : NotFound(new
            {
                code = result.Error.Code,
                message = result.Error.Message,
                traceId = HttpContext.TraceIdentifier,
                timestamp = DateTimeOffset.UtcNow
            });
    }
}
