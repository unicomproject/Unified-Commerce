using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceContextService _deviceContextService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public DevicesController(
        IDeviceContextService deviceContextService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _deviceContextService = deviceContextService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentDeviceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentDevice(
        [FromQuery] string? deviceFingerprint,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("device_context.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _deviceContextService.GetCurrentDeviceAsync(
            context,
            deviceFingerprint,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("activate")]
    [ProducesResponseType(typeof(CurrentDeviceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateDevice(
        [FromBody] ActivateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("device_context.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _deviceContextService.ActivateDeviceAsync(
            context,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "device_context.not_found" => NotFound(CreateError(error)),
            "device_context.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "device_context.fingerprint_already_paired" => Conflict(CreateError(error)),
            "device_context.activation_code_used" => Conflict(CreateError(error)),
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
