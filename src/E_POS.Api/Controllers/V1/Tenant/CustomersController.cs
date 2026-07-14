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

    [HttpGet]
    [ProducesResponseType(typeof(PosCustomerListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? search,
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

    private IActionResult ToErrorResult(ApplicationError error) => error.Code switch
    {
        "pos_customers.permission_denied" or "pos_customers.create_permission_denied" or
            "pos_customers.device_not_trusted" =>
            StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "pos_customers.device_not_found" or "pos_customers.till_not_assigned" or
            "pos_customers.open_till_required" => NotFound(CreateError(error)),
        "pos_customers.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "pos_customers.duplicate_phone" or "pos_customers.duplicate_email" or
            "pos_customers.duplicate_contact" => Conflict(CreateError(error)),
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
