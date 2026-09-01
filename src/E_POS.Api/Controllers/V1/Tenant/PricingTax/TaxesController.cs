using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.PricingTax;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tax")]
public class TaxesController : ControllerBase
{
    private readonly ITaxAggregateService _taxAggregateService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TaxesController(
        ITenantRequestContextFactory tenantRequestContextFactory,
        ITaxAggregateService taxAggregateService)
    {
        _tenantRequestContextFactory = tenantRequestContextFactory;
        _taxAggregateService = taxAggregateService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTax([FromBody] TaxAggregateCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.CreateTaxAsync(context, request, cancellationToken);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetTax), new { id = result.Value }, result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTax(Guid id, [FromBody] TaxAggregateUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.UpdateTaxAsync(context, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaxAggregateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTax(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.GetTaxAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(typeof(TaxAggregateListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.GetTaxesAsync(context, pageNumber, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTax(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.DeleteTaxAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pricing.tax_aggregate.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pricing.tax_aggregate.not_found" => NotFound(CreateError(error)),
            "pricing.tax_aggregate.code_exists" => Conflict(CreateError(error)),
            "pricing.tax_aggregate.rate_exists" => Conflict(CreateError(error)),
            "pricing.tax_aggregate.invalid_tenant_context" => Unauthorized(CreateError(error)),
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
