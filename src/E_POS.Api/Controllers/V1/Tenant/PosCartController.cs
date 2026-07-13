using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/cart")]
public sealed class PosCartController : ControllerBase
{
    private readonly IPosCheckoutService _posCheckoutService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosCartController(
        IPosCheckoutService posCheckoutService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _posCheckoutService = posCheckoutService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(PosCheckoutSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calculate(
        [FromBody] PosCheckoutSummaryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_cart.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posCheckoutService.CalculateCartAsync(
            context,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error) =>
        error.Code switch
        {
            "pos_cart.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_checkout.device_not_found" or
            "pos_checkout.customer_not_found" or
            "pos_checkout.variant_not_found" => NotFound(CreateError(error)),
            "pos_cart.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };

    private object CreateError(ApplicationError error) =>
        new
        {
            code = error.Code,
            message = error.Message,
            details = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };
}
