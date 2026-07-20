using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.ECommerce;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant/ecommerce/click-collect/orders")]
public sealed class ClickCollectOrdersController : ControllerBase
{
    private readonly IClickCollectOrderStatusService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public ClickCollectOrdersController(
        IClickCollectOrderStatusService service,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPatch("{orderId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid orderId,
        [FromBody] ClickCollectOrderStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "click_collect_orders.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.UpdateStatusAsync(
            context,
            orderId,
            request,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                success = true,
                message = "Click & collect order status updated successfully.",
                data = result.Value
            });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error) => error.Code switch
    {
        "click_collect_orders.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "click_collect_orders.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "click_collect_orders.not_found" => NotFound(CreateError(error)),
        "click_collect_orders.invalid_transition" => StatusCode(StatusCodes.Status409Conflict, CreateError(error)),
        _ => BadRequest(CreateError(error))
    };

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
