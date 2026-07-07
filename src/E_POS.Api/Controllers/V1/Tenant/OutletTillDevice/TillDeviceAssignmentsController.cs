using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tills/{tillId:guid}/devices")]
public sealed class TillDeviceAssignmentsController : ControllerBase
{
    private readonly ITillDeviceAssignmentService _assignmentService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TillDeviceAssignmentsController(ITillDeviceAssignmentService assignmentService, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _assignmentService = assignmentService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost("{deviceId:guid}")]
    [ProducesResponseType(typeof(TillDeviceAssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(Guid tillId, Guid deviceId, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("till_device_assignment.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _assignmentService.AssignAsync(context, tillId, deviceId, cancellationToken);
        return result.IsSuccess && result.Value is not null ? CreatedAtAction(nameof(ListByTill), new { tillId }, result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(typeof(TillDeviceAssignmentListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByTill(Guid tillId, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("till_device_assignment.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _assignmentService.ListByTillAsync(context, tillId, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid tillId, Guid deviceId, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("till_device_assignment.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _assignmentService.RemoveAsync(context, tillId, deviceId, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "till_device_assignment.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "till_device_assignment.till_not_found" or "till_device_assignment.device_not_found" or "till_device_assignment.not_found" => NotFound(CreateError(error)),
            "till_device_assignment.duplicate" or "till_device_assignment.device_already_assigned" => Conflict(CreateError(error)),
            "till_device_assignment.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new { code = error.Code, message = error.Message, details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow };
    }
}

