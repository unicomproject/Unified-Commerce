using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[ApiController]
[Route("api/v1/tenant-admin/inventory/[controller]")]
[Authorize(Policy = "TenantOnly")]
[Tags("Tenant Admin - Inventory")]
public abstract class InventoryBaseController : ControllerBase
{
    protected IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }
        return ToErrorResult(result.Error);
    }

    protected IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "inventory.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "inventory.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "inventory.validation_failed" => BadRequest(CreateError(error)),
            "inventory.duplicate_request" => Conflict(CreateError(error)),
            "inventory.not_found" => NotFound(CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private object CreateError(ApplicationError error)
    {
        var fieldErrors = error.FieldErrors?
            .Select(item => new { field = item.Field, message = item.Message })
            .ToArray<object>() ?? Array.Empty<object>();

        return new
        {
            code = error.Code,
            message = error.Message,
            details = fieldErrors,
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
