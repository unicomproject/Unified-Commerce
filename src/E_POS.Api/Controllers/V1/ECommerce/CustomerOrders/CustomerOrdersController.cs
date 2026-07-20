using System.Security.Claims;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.CustomerOrders;

[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v1/ecommerce/storefront/orders")]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly ICustomerOrderService _service;

    public CustomerOrdersController(ICustomerOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.GetAsync(
                tenantId,
                customerId,
                status,
                page,
                pageSize,
                cancellationToken),
            "Orders retrieved successfully.");
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.GetDetailAsync(
                tenantId,
                customerId,
                orderId,
                cancellationToken),
            "Order retrieved successfully.");
    }

    [HttpPost("{orderId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid orderId,
        [FromBody] CustomerOrderCancelRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.CancelAsync(
                tenantId,
                customerId,
                orderId,
                request ?? new CustomerOrderCancelRequest(),
                cancellationToken),
            "Order cancelled successfully.");
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                success = true,
                message = successMessage,
                data = result.Value
            });
        }

        var error = CreateError(result.Error);
        return result.Error.Code switch
        {
            "customer_orders.invalid_customer_context" => Unauthorized(error),
            "customer_orders.not_found" => NotFound(error),
            "customer_orders.invalid_transition" => StatusCode(StatusCodes.Status409Conflict, error),
            _ => BadRequest(error)
        };
    }

    private bool TryGetCustomerContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    private IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "customer_orders.invalid_customer_context",
            "A valid customer session is required.")));

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
