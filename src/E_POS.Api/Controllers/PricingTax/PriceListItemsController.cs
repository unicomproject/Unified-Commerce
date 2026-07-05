using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.PricingTax;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pricing/price-list-items")]
public sealed class PriceListItemsController : ControllerBase
{
    private readonly IPriceListItemsService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PriceListItemsController(
        IPriceListItemsService service,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PriceListItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] PriceListItemCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.price_list_item.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.CreateAsync(context, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PriceListItemListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] Guid priceListId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (priceListId == Guid.Empty)
            return BadRequest(CreateError(new ApplicationError("pricing.price_list_item.invalid_request", "Price list ID is required.")));

        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.price_list_item.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.ListAsync(context, priceListId, pageNumber, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PriceListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.price_list_item.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetByIdAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PriceListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] PriceListItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.price_list_item.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.UpdateAsync(context, id, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.price_list_item.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pricing.price_list_item.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pricing.price_list_item.not_found" => NotFound(CreateError(error)),
            "pricing.price_list_item.invalid_price_list" => BadRequest(CreateError(error)),
            "pricing.price_list_item.invalid_product" => BadRequest(CreateError(error)),
            "pricing.price_list_item.invalid_variant" => BadRequest(CreateError(error)),
            "pricing.price_list_item.invalid_uom" => BadRequest(CreateError(error)),
            "pricing.price_list_item.duplicate_entry" => Conflict(CreateError(error)),
            "pricing.price_list_item.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
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
            timestamp = DateTimeOffset.UtcNow
        };
    }
}
