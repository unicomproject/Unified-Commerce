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
    private readonly IPosOnlineOrderDetailService _detailService;
    private readonly IPosOnlineOrderStartFulfillmentService _startFulfillmentService;
    private readonly IPosOnlineOrderPickingService _pickingService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public ClickCollectOrdersController(
        IClickCollectOrderStatusService service,
        IPosOnlineOrderDetailService detailService,
        IPosOnlineOrderStartFulfillmentService startFulfillmentService,
        IPosOnlineOrderPickingService pickingService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _detailService = detailService;
        _startFulfillmentService = startFulfillmentService;
        _pickingService = pickingService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("{orderId:guid}/picking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetPicking(
        [FromRoute] Guid orderId,
        [FromQuery] Guid outletId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _pickingService.GetAsync(context, outletId, orderId, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { success = true, message = "Picking details loaded successfully.", data = result.Value })
            : ToPickingErrorResult(result.Error);
    }

    [HttpPost("{orderId:guid}/picking/lines/{lineId:guid}/pick")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PickLine(
        [FromRoute] Guid orderId,
        [FromRoute] Guid lineId,
        [FromQuery] Guid outletId,
        [FromBody] PosOnlineOrderPickLineRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _pickingService.PickLineAsync(
            context, outletId, orderId, lineId, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { success = true, message = "Fulfilment line picked successfully.", data = result.Value })
            : ToPickingErrorResult(result.Error);
    }

    [HttpPost("{orderId:guid}/picking/lines/{lineId:guid}/issues")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReportPickingIssue(
        [FromRoute] Guid orderId,
        [FromRoute] Guid lineId,
        [FromQuery] Guid outletId,
        [FromBody] PosOnlineOrderPickingIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _pickingService.ReportIssueAsync(
            context, outletId, orderId, lineId, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { success = true, message = "Picking issue reported successfully.", data = result.Value })
            : ToPickingErrorResult(result.Error);
    }

    [HttpPost("{orderId:guid}/picking/notes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPickingNote(
        [FromRoute] Guid orderId,
        [FromQuery] Guid outletId,
        [FromBody] PosOnlineOrderPickingNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _pickingService.AddNoteAsync(
            context, outletId, orderId, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { success = true, message = "Picking note added successfully.", data = result.Value })
            : ToPickingErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] Guid outletId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _detailService.ListAsync(
            context,
            new PosOnlineOrderListQuery(
                outletId, search, status, sortBy, sortDirection, page, pageSize),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return ToDetailErrorResult(result.Error);

        var value = result.Value;
        return Ok(new
        {
            data = value.Items,
            summary = value.Summary,
            pagination = new
            {
                value.Page,
                value.PageSize,
                value.TotalCount,
                value.TotalPages
            },
            value.ServerTime
        });
    }

    [HttpPost("{orderId:guid}/fulfilment/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartFulfillment(
        [FromRoute] Guid orderId,
        [FromQuery] Guid outletId,
        [FromBody] PosOnlineOrderStartFulfillmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _startFulfillmentService.StartAsync(
            context, outletId, orderId, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                success = true,
                message = "Fulfilment started successfully.",
                data = result.Value
            });
        }

        return ToStartErrorResult(result.Error);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        [FromRoute] Guid orderId,
        [FromQuery] Guid outletId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "online_orders.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _detailService.GetAsync(
            context,
            outletId,
            orderId,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                success = true,
                message = "Online order details loaded successfully.",
                data = result.Value
            });
        }

        return ToDetailErrorResult(result.Error);
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

    private IActionResult ToDetailErrorResult(ApplicationError error) => error.Code switch
    {
        "online_orders.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "online_orders.permission_denied" or
        "online_orders.feature_not_entitled" or
        "online_orders.outlet_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "online_orders.not_found" => NotFound(CreateError(error)),
        _ => BadRequest(CreateError(error))
    };

    private IActionResult ToStartErrorResult(ApplicationError error) => error.Code switch
    {
        "online_orders.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "online_orders.permission_denied" or
        "online_orders.feature_not_entitled" or
        "online_orders.outlet_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "online_orders.not_found" => NotFound(CreateError(error)),
        "online_orders.concurrency_conflict" or
        "online_orders.invalid_state" or
        "online_orders.invalid_reservation" => Conflict(CreateError(error)),
        _ => BadRequest(CreateError(error))
    };

    private IActionResult ToPickingErrorResult(ApplicationError error) => error.Code switch
    {
        "online_orders.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "online_orders.permission_denied" or
        "online_orders.feature_not_entitled" or
        "online_orders.outlet_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "online_orders.not_found" => NotFound(CreateError(error)),
        "online_orders.concurrency_conflict" or
        "online_orders.invalid_state" => Conflict(CreateError(error)),
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
