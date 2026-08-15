using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/hardware/drawer")]
public sealed class PosDrawerController : ControllerBase
{
    private readonly IPosDrawerService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosDrawerController(
        IPosDrawerService service,
        ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpPost("operations")]
    public async Task<IActionResult> RegisterOperation(
        [FromBody] RegisterDrawerOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_drawer.invalid_context", "Invalid tenant context.")));

        var result = await _service.RegisterOperationAsync(context, request, cancellationToken);
        return Result(result);
    }

    [HttpPut("operations/{operationId:guid}/finalize")]
    public async Task<IActionResult> FinalizeOperation(
        Guid operationId,
        [FromBody] FinalizeDrawerOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_drawer.invalid_context", "Invalid tenant context.")));

        var result = await _service.FinalizeOperationAsync(context, operationId, request, cancellationToken);
        return Result(result);
    }

    [HttpPost("operations/manual-open")]
    public async Task<IActionResult> ManualOpen(
        [FromBody] ManualOpenDrawerRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_drawer.invalid_context", "Invalid tenant context.")));

        var result = await _service.ManualOpenDrawerAsync(context, request, cancellationToken);
        return Result(result);
    }

    [HttpGet("operations/history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid posDeviceId,
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_drawer.invalid_context", "Invalid tenant context.")));

        var result = await _service.GetHistoryAsync(context, posDeviceId, take, cancellationToken);
        return Result(result);
    }

    [HttpGet("operations/{operationId:guid}")]
    public async Task<IActionResult> GetOperation(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_drawer.invalid_context", "Invalid tenant context.")));

        var result = await _service.GetOperationStatusAsync(context, operationId, cancellationToken);
        return Result(result);
    }

    [HttpGet("operations/by-request/{requestId:guid}")]
    public async Task<IActionResult> GetOperationByRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error(new ApplicationError("pos_drawer.invalid_context", "Invalid tenant context.")));

        var result = await _service.GetOperationStatusByRequestIdAsync(context, requestId, cancellationToken);
        return Result(result);
    }

    private IActionResult Result<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { data = result.Value });

        var error = result.Error;
        return error.Code switch
        {
            "pos_drawer.permission_denied" or "pos_drawer.approver_permission_denied" => 
                StatusCode(StatusCodes.Status403Forbidden, Error(error)),
            "pos_drawer.configuration_missing" or "pos_drawer.operation_not_found" => 
                NotFound(Error(error)),
            "pos_drawer.idempotency_conflict" or "pos_drawer.already_finalized" => 
                Conflict(Error(error)),
            "pos_drawer.till_session_not_open" or "pos_drawer.approval_required" or 
            "pos_drawer.invalid_approver_credentials" or "pos_drawer.manual_open_disabled" or
            "pos_drawer.purpose_disabled" or "pos_drawer.configuration_invalid" => 
                UnprocessableEntity(Error(error)),
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

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/cash-drawer")]
public sealed class PosCashDrawerController : ControllerBase
{
    private readonly IPosDrawerService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosCashDrawerController(IPosDrawerService service, ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] Guid deviceId, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("cash_drawer.invalid_context", "Invalid tenant context."));
        return Result(await _service.GetFinancialSummaryAsync(context, deviceId, cancellationToken));
    }

    [HttpGet("movements")]
    public async Task<IActionResult> Movements([FromQuery] Guid deviceId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("cash_drawer.invalid_context", "Invalid tenant context."));
        return Result(await _service.GetFinancialMovementsAsync(context, deviceId, page, pageSize, cancellationToken));
    }

    [HttpPost("movements")]
    public async Task<IActionResult> CreateMovement([FromBody] CreatePosCashMovementRequest request, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("cash_drawer.invalid_context", "Invalid tenant context."));
        return Result(await _service.CreateFinancialMovementAsync(context, request, cancellationToken));
    }

    private IActionResult Result<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null) return Ok(new { data = result.Value });
        var error = result.Error;
        var payload = Error(error.Code, error.Message);
        return error.Code switch
        {
            "cash_drawer.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, payload),
            "till_session.device_not_trusted" or "till_session.till_mismatch" => StatusCode(StatusCodes.Status403Forbidden, payload),
            "till_session.device_not_found" or "till_session.till_not_assigned" or "till_session.not_found" or "cash_drawer.till_session_not_open" => NotFound(payload),
            "cash_drawer.idempotency_conflict" => Conflict(payload),
            "cash_drawer.insufficient_expected_cash" => UnprocessableEntity(payload),
            _ => BadRequest(payload)
        };
    }

    private object Error(string code, string message) => new
    {
        code, message, details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow
    };
}
