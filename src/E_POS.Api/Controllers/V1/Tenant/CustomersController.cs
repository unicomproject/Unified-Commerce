using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly IPosCustomerService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public CustomersController(
        IPosCustomerService service,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PosCustomerListItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromQuery] Guid? deviceId,
        [FromBody] PosCustomerCreateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.CreateAsync(
            context,
            deviceId,
            request,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return StatusCode(StatusCodes.Status201Created, new { data = result.Value });
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(PosCustomerSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Summary(
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetSummaryAsync(context, deviceId, cancellationToken);
        return result.IsSuccess
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PosCustomerListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.ListAsync(
            context,
            deviceId,
            search,
            status,
            source,
            page,
            pageSize,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        var response = result.Value!;
        return Ok(new
        {
            data = response.Items,
            pagination = new
            {
                page = response.Page,
                pageSize = response.PageSize,
                totalCount = response.TotalCount,
                totalPages = response.TotalPages
            }
        });
    }

    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(PosCustomerListItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid customerId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetByIdAsync(context, deviceId, customerId, cancellationToken);
        return result.IsSuccess
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpGet("{customerId:guid}/orders")]
    [ProducesResponseType(typeof(PosCustomerOrdersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrders(
        Guid customerId,
        [FromQuery] Guid? deviceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetOrdersAsync(
            context,
            deviceId,
            customerId,
            page,
            pageSize,
            fromDate,
            toDate,
            status,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        var response = result.Value!;
        return Ok(new
        {
            data = response.Items,
            pagination = new
            {
                page = response.Page,
                pageSize = response.PageSize,
                totalCount = response.TotalCount,
                totalPages = response.TotalPages
            }
        });
    }

    [HttpPost("{customerId:guid}/attach-to-sale")]
    [ProducesResponseType(typeof(PosCustomerAttachToSaleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AttachToSale(
        Guid customerId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosCustomerAttachToSaleRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.AttachToSaleAsync(
            context,
            deviceId,
            customerId,
            request ?? new PosCustomerAttachToSaleRequestDto(null),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPut("{customerId:guid}")]
    [ProducesResponseType(typeof(PosCustomerListItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid customerId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosCustomerUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_customers.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.UpdateAsync(
            context,
            deviceId,
            customerId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error) => error.Code switch
    {
        "pos_customers.permission_denied" or "pos_customers.create_permission_denied" or
            "pos_customers.update_permission_denied" or
            "pos_customers.attach_permission_denied" or
            "pos_customers.device_not_trusted" =>
            StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "pos_customers.device_not_found" or "pos_customers.till_not_assigned" or
            "pos_customers.open_till_required" or
            "pos_customers.customer_not_found" => NotFound(CreateError(error)),
        "pos_customers.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "pos_customers.duplicate_phone" or "pos_customers.duplicate_email" or
            "pos_customers.duplicate_contact" or
            "pos_customers.customer_update_conflict" or
            "pos_customers.sale_not_editable" => Conflict(CreateError(error)),
        "pos_customers.customer_inactive" or "pos_customers.customer_blocked" or
            "pos_customers.customer_deleted" or "pos_customers.customer_not_eligible" =>
            UnprocessableEntity(CreateError(error)),
        _ => BadRequest(CreateError(error))
    };

    private object CreateError(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
