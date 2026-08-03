using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers.V1.Tenant.HardwareCash;

[ApiController]
[Route("api/v1/tenant-admin")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminHardwareDevicesController : ControllerBase
{
    private readonly ITenantAdminHardwareService _hardwareService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminHardwareDevicesController(
        ITenantAdminHardwareService hardwareService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _hardwareService = hardwareService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("hardware-devices")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? outletId = null,
        [FromQuery] string? hardwareType = null,
        [FromQuery] string? lifecycleStatus = null,
        [FromQuery] string? assignmentStatus = null,
        [FromQuery] bool? availableOnly = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.ListAsync(
            context,
            outletId,
            hardwareType,
            lifecycleStatus,
            assignmentStatus,
            availableOnly,
            search,
            page,
            pageSize,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("hardware-devices/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.GetByIdAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("hardware-devices")]
    public async Task<IActionResult> Create(
        [FromBody] TenantAdminHardwareDeviceCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.CreateAsync(context, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value.HardwareDeviceId },
                new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpPost("tills/{tillId:guid}/hardware-assignments")]
    public async Task<IActionResult> AssignToTill(
        Guid tillId,
        [FromBody] TenantAdminHardwareAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.AssignToTillAsync(context, tillId, request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("pos-devices/{posDeviceId:guid}/hardware-assignments")]
    public async Task<IActionResult> AssignToPosDevice(
        Guid posDeviceId,
        [FromBody] TenantAdminHardwareAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.AssignToPosDeviceAsync(context, posDeviceId, request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("hardware-assignments/{assignmentId:guid}/release")]
    public async Task<IActionResult> Release(
        Guid assignmentId,
        [FromBody] TenantAdminHardwareAssignmentReleaseRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.ReleaseAssignmentAsync(
            context,
            assignmentId,
            request ?? new TenantAdminHardwareAssignmentReleaseRequest(),
            cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            if (successStatusCode == StatusCodes.Status201Created)
            {
                return StatusCode(StatusCodes.Status201Created, new { data = result.Value });
            }

            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "hardware.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "hardware.not_found" or "hardware.till_not_found" or "hardware.pos_device_not_found"
                or "hardware.outlet_not_found" or "hardware.assignment_not_found"
                => NotFound(CreateError(error)),
            "hardware.duplicate_code" or "hardware.assignment_conflict"
                => Conflict(CreateError(error)),
            "hardware.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error)),
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
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}

[ApiController]
[Route("api/v1/pos")]
[Authorize(Policy = "TenantOnly")]
public sealed class PosHardwareTelemetryController : ControllerBase
{
    private readonly ITenantAdminHardwareService _hardwareService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosHardwareTelemetryController(
        ITenantAdminHardwareService hardwareService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _hardwareService = hardwareService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost("devices/{posDeviceId:guid}/hardware-heartbeat")]
    [EnableRateLimiting("hardware-heartbeat")]
    public async Task<IActionResult> HardwareHeartbeat(
        Guid posDeviceId,
        [FromBody] PosHardwareHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.RecordHardwareHeartbeatAsync(
            context,
            posDeviceId,
            request,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("hardware-tests")]
    public async Task<IActionResult> ReportTest(
        [FromBody] PosHardwareTestResultRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "hardware.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _hardwareService.ReportHardwareTestAsync(context, request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status201Created);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            if (successStatusCode == StatusCodes.Status201Created)
            {
                return StatusCode(StatusCodes.Status201Created, new { data = result.Value });
            }

            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "hardware.permission_denied" or "hardware.pos_device_untrusted" or "hardware.unrelated_device"
                => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "hardware.not_found" or "hardware.pos_device_not_found"
                => NotFound(CreateError(error)),
            "hardware.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error)),
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
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
