using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/hardware")]
public sealed class PosHardwareController : ControllerBase
{
    private readonly IPosHardwareService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosHardwareController(
        IPosHardwareService service,
        ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpGet("configurations")]
    public async Task<IActionResult> GetConfigurations(
        [FromQuery] Guid posDeviceId,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_hardware.invalid_context", "Invalid tenant context.")));
        return Result(await _service.GetConfigurationsAsync(context, posDeviceId, cancellationToken));
    }

    [HttpPut("configurations")]
    public async Task<IActionResult> SaveConfiguration(
        [FromBody] SavePosHardwareConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_hardware.invalid_context", "Invalid tenant context.")));
        return Result(await _service.SaveConfigurationAsync(context, request, cancellationToken));
    }

    [HttpPost("tests")]
    public async Task<IActionResult> CreateTest(
        [FromBody] CreateHardwareTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_hardware.invalid_context", "Invalid tenant context.")));
        return Result(await _service.CreateTestAsync(context, request, cancellationToken));
    }

    [HttpPut("tests/{testId:guid}/result")]
    public async Task<IActionResult> CompleteTest(
        Guid testId,
        [FromBody] CompleteHardwareTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_hardware.invalid_context", "Invalid tenant context.")));
        return Result(await _service.CompleteTestAsync(context, testId, request, cancellationToken));
    }

    [HttpGet("tests")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid posDeviceId,
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_hardware.invalid_context", "Invalid tenant context.")));
        return Result(await _service.GetTestHistoryAsync(context, posDeviceId, take, cancellationToken));
    }

    private IActionResult Result<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { data = result.Value });

        var error = result.Error;
        return error.Code switch
        {
            "pos_hardware.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(error)),
            "pos_hardware.device_not_trusted" or "pos_hardware.configuration_not_found" or
            "pos_hardware.test_not_found" => NotFound(Error(error)),
            "pos_hardware.version_conflict" or "pos_hardware.request_id_conflict" or
            "pos_hardware.result_conflict" => Conflict(Error(error)),
            "pos_hardware.active_shift_reason_required" => UnprocessableEntity(Error(error)),
            "pos_hardware.scanner_disabled" => UnprocessableEntity(Error(error)),
            "pos_hardware.unsupported_scanner_mode" or
            "pos_hardware.invalid_test_result" => UnprocessableEntity(Error(error)),
            _ => BadRequest(Error(error))
        };
    }

    private object Error(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
