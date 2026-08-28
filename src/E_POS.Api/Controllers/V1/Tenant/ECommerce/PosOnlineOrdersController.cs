using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.ECommerce;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant/ecommerce/click-collect/orders")]
public sealed class PosOnlineOrdersController : ControllerBase
{
    private readonly IPosOnlineOrderService _service;
    private readonly ITenantRequestContextFactory _contextFactory;

    public PosOnlineOrdersController(IPosOnlineOrderService service, ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    [HttpGet]
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
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));

        var result = await _service.ListAsync(
            context,
            new(outletId, search, status, sortBy, sortDirection, page, pageSize),
            cancellationToken);
        if (!result.IsSuccess) return Failure(result.Error);
        var value = result.Value!;
        return Ok(new
        {
            data = value.Items,
            summary = value.Summary,
            pagination = new { value.Page, value.PageSize, value.TotalCount, value.TotalPages },
            value.ServerTime
        });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> Get(
        Guid orderId,
        [FromQuery] Guid outletId,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.GetAsync(context, outletId, orderId, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    [HttpPost("{orderId:guid}/fulfilment/start")]
    public async Task<IActionResult> StartFulfillment(
        Guid orderId,
        [FromQuery] Guid outletId,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.StartFulfillmentAsync(context, outletId, orderId, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    [HttpGet("{orderId:guid}/picking")]
    public async Task<IActionResult> GetPicking(
        Guid orderId,
        [FromQuery] Guid outletId,
        CancellationToken cancellationToken = default)
    {
        if (!_contextFactory.TryCreate(User, out var context))
            return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.GetPickingAsync(context, outletId, orderId, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    [HttpPost("{orderId:guid}/picking/lines/{lineId:guid}/pick")]
    public async Task<IActionResult> PickLine(Guid orderId, Guid lineId, [FromQuery] Guid outletId, [FromBody] PosPickLineRequest request, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.PickLineAsync(context, outletId, orderId, lineId, request, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    [HttpPost("{orderId:guid}/picking/lines/{lineId:guid}/issues")]
    public async Task<IActionResult> ReportIssue(Guid orderId, Guid lineId, [FromQuery] Guid outletId, [FromBody] PosReportPickingIssueRequest request, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.ReportIssueAsync(context, outletId, orderId, lineId, request, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    [HttpPost("{orderId:guid}/pack")]
    public async Task<IActionResult> Pack(Guid orderId, [FromQuery] Guid outletId, [FromBody] PosPackOrderRequest request, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.PackAsync(context, outletId, orderId, request, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    [HttpPost("{orderId:guid}/ready")]
    public async Task<IActionResult> Ready(Guid orderId, [FromQuery] Guid outletId, CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized(Error("online_orders.invalid_tenant_context", "Invalid tenant context."));
        var result = await _service.MarkReadyAsync(context, outletId, orderId, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Failure(result.Error);
    }

    private IActionResult Failure(ApplicationError error) => error.Code switch
    {
        "online_orders.invalid_tenant_context" => Unauthorized(Error(error.Code, error.Message)),
        "online_orders.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(error.Code, error.Message)),
        "online_orders.feature_not_entitled" => StatusCode(StatusCodes.Status403Forbidden, Error(error.Code, error.Message)),
        "online_orders.outlet_access_denied" => StatusCode(StatusCodes.Status403Forbidden, Error(error.Code, error.Message)),
        "online_orders.not_found" => NotFound(Error(error.Code, error.Message)),
        "online_orders.picking_not_found" => NotFound(Error(error.Code, error.Message)),
        "online_orders.fulfilment_conflict" => Conflict(Error(error.Code, error.Message)),
        _ => BadRequest(Error(error.Code, error.Message))
    };

    private object Error(string code, string message) => new
    {
        code,
        message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
