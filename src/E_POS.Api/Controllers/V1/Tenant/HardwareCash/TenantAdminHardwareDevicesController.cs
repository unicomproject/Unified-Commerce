using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;

namespace E_POS.Api.Controllers.V1.Tenant.HardwareCash;

[ApiController]
[Route("api/v1/tenant-admin")]
[Authorize(Policy = "TenantOnly")]
[TypeFilter(typeof(HardwareEntitlementFilter), Order = -1)]
[TypeFilter(typeof(HardwareScopeFilter), Order = 0)]
public sealed class TenantAdminHardwareDevicesController : ControllerBase
{
    private readonly ITenantAdminHardwareService _hardwareService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;
    private readonly HardwareQueryScope? _hardwareScope;

    public TenantAdminHardwareDevicesController(
        ITenantAdminHardwareService hardwareService,
        ITenantRequestContextFactory tenantRequestContextFactory,
        HardwareQueryScope? hardwareScope = null)
    {
        _hardwareService = hardwareService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
        _hardwareScope = hardwareScope;
    }

    /// <summary>Software compatibility profiles; certification is independent of runtime readiness.</summary>
    /// <remarks>Requires tenant.hardware.view or tenant.hardware.manage. No provider secrets or tenant records are returned.</remarks>
    [HttpGet("hardware-devices/create-options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateOptions([FromQuery] string? search = null,
        [FromQuery] string? deviceType = null, [FromQuery] string? connectionType = null,
        [FromQuery] string? supportLevel = null)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized();
        if (!context.HasAnyPermission("tenant.hardware.view", "tenant.hardware.manage"))
            return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(new { data = new {
            hardwareScope = new {
                restricted = _hardwareScope?.TillId is not null,
                outletIds = _hardwareScope?.OutletId is Guid outlet ? new[] { outlet } : Array.Empty<Guid>(),
                tillIds = _hardwareScope?.TillId is Guid till ? new[] { till } : Array.Empty<Guid>(),
            },
            compatibilityProfiles = HardwareCompatibilityCatalog.Search(search, deviceType, connectionType, supportLevel).ToArray(),
            deviceTypes = HardwareCompatibilityCatalog.Profiles.Select(p => p.DeviceType).Distinct().ToArray(),
            connectionTypes = HardwareCompatibilityCatalog.Profiles.Select(p => p.ConnectionType).Distinct().ToArray(),
            printerPaperWidths = new[] { 58, 80 },
            paymentProviders = Array.Empty<object>(),
            testAction = "TEST_ON_POS_REQUIRED",
            capabilitySource = "DECLARED",
        }});
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

        var result = await Mutate(context, "CREATE", null, request,
            ct => _hardwareService.CreateAsync(context, request, ct), cancellationToken);
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

        var result = await Mutate(context, "ASSIGN_TILL", request.HardwareDeviceId, new { tillId, request },
            ct => _hardwareService.AssignToTillAsync(context, tillId, request, ct), cancellationToken);
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

        var result = await Mutate(context, "ASSIGN_POS", request.HardwareDeviceId, new { posDeviceId, request },
            ct => _hardwareService.AssignToPosDeviceAsync(context, posDeviceId, request, ct), cancellationToken);
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

        var result = await Mutate(context, "RELEASE", assignmentId, new { assignmentId, request }, ct => _hardwareService.ReleaseAssignmentAsync(
            context,
            assignmentId,
            request ?? new TenantAdminHardwareAssignmentReleaseRequest(),
            ct), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("hardware-devices/{id:guid}/test-history")]
    public async Task<IActionResult> TestHistory(Guid id,
        [FromServices] ITenantAdminHardwareRepository repository, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized();
        var access = await _hardwareService.GetByIdAsync(context, id, cancellationToken);
        if (!access.IsSuccess) return ToActionResult(access);
        var items = await repository.GetTestHistoryAsync(context.TenantId, id, cancellationToken);
        return Ok(new { data = new { items, limit = 50 } });
    }

    [HttpGet("hardware-devices/{id:guid}/activity")]
    public async Task<IActionResult> Activity(Guid id,
        [FromServices] ITenantAdminHardwareRepository repository, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized();
        var access = await _hardwareService.GetByIdAsync(context, id, cancellationToken);
        if (!access.IsSuccess) return ToActionResult(access);
        var items = await repository.GetActivityAsync(context.TenantId, id, cancellationToken);
        return Ok(new { data = new { items, limit = 50 } });
    }

    /// <summary>Tenant-scoped aggregate readiness and a server-paged hardware list.</summary>
    [HttpGet("hardware-devices/dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] Guid? outletId = null, [FromQuery] string search = "",
        [FromQuery] string type = "", [FromQuery] string status = "", [FromQuery] string sort = "name",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 5, CancellationToken ct = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized();
        if (!context.HasAnyPermission("tenant.hardware.view", "tenant.hardware.manage")) return Forbid();
        if (sort is not ("name" or "name_desc" or "last_seen") || page < 1 || page > 100000 || pageSize is < 1 or > 100 || search.Length > 150)
            return BadRequest(CreateError(new("hardware.validation_failed", "Invalid dashboard query.")));
        var data = await HttpContext.RequestServices.GetRequiredService<IHardwareDashboardQuery>()
            .GetAsync(context.TenantId, outletId, search, type, status, sort, page, pageSize, ct);
        return Ok(new { data });
    }

    /// <summary>Update registry label/lifecycle using the current configuration version. Physical identity is immutable.</summary>
    [HttpPut("hardware-devices/{id:guid}")]
    [RequestSizeLimit(8192)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenantAdminHardwareUpdateRequest request, CancellationToken ct)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized();
        return ToActionResult(await Mutate(context, "UPDATE", id, new { id, request },
            token => _hardwareService.UpdateAsync(context, id, request, token), ct));
    }

    private Task<ApplicationResult<T>> Mutate<T>(TenantRequestContext context, string operation, Guid? id,
        object payload, Func<CancellationToken, Task<ApplicationResult<T>>> execute, CancellationToken ct) =>
        HttpContext.RequestServices.GetRequiredService<IHardwareMutationRunner>().ExecuteAsync(
            context, operation, id, Request.Headers["Idempotency-Key"].ToString(), payload, execute, ct);


    [HttpPost("tills/{tillId:guid}/hardware-test-all")]
    public async Task<IActionResult> StartTestAll(Guid tillId, [FromBody] HardwareTestAllRequest request, CancellationToken ct)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var actor)) return Unauthorized();
        if (!actor.HasPermission("tenant.hardware.manage")) return Forbid();
        try {
            var service = HttpContext.RequestServices.GetRequiredService<E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services.HardwareRemoteTestService>();
            return Ok(new { data = await service.StartAsync(actor, tillId, request.RequestId, ct) });
        } catch (InvalidOperationException e) { return BadRequest(new { code = "hardware.test_all_unavailable", message = e.Message }); }
    }
    [HttpGet("tills/{tillId:guid}/hardware-test-all/{id:guid}")]
    public async Task<IActionResult> GetTestAll(Guid tillId, Guid id, CancellationToken ct)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var actor)) return Unauthorized();
        if (!actor.HasPermission("tenant.hardware.manage")) return Forbid();
        var data = await HttpContext.RequestServices.GetRequiredService<E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services.HardwareRemoteTestService>().GetAsync(actor, tillId, id, ct);
        return data == null ? NotFound() : Ok(new { data });
    }
    public sealed record HardwareTestAllRequest(Guid RequestId);

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
            "hardware.duplicate_code" or "hardware.assignment_conflict" or "hardware.version_conflict" or "hardware.idempotency_conflict"
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
[TypeFilter(typeof(HardwareEntitlementFilter), Order = -1)]
[TypeFilter(typeof(PosDeviceProofFilter), Order = 0)]
[TypeFilter(typeof(HardwareScopeFilter), Order = 1)]
[TypeFilter(typeof(HardwareTelemetryTransactionFilter), Order = 2)]
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
    [RequestSizeLimit(32768)]
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
    [EnableRateLimiting("hardware-heartbeat")]
    [RequestSizeLimit(32768)]
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
            "hardware.idempotency_conflict" or "hardware.version_conflict"
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
