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
    public async Task<IActionResult> GetTaxes(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var effectivePage = page > 0 ? page : pageNumber;
        var result = await _taxAggregateService.GetTaxesAsync(context, search, status, effectivePage, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpPost("{id:guid}/rates")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> ScheduleRate(Guid id, [FromBody] TaxScheduleRateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.ScheduleRateAsync(context, id, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}/rates/{rateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateFutureRate(Guid id, Guid rateId, [FromBody] TaxFutureRateUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.UpdateFutureRateAsync(context, id, rateId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}/rates/{rateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFutureRate(Guid id, Guid rateId, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.DeleteFutureRateAsync(context, id, rateId, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(TaxStatusChangeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.ActivateAsync(context, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(TaxStatusChangeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(Guid id, [FromBody] TaxStatusChangeRequest? request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.DeactivateAsync(context, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}/products")]
    [ProducesResponseType(typeof(TaxProductsUsingListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsUsing(
        Guid id,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_aggregate.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _taxAggregateService.GetProductsUsingAsync(context, id, search, pageNumber, pageSize, cancellationToken);
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
            "pricing.tax_aggregate.not_found" or "pricing.tax_aggregate.rate_not_found" => NotFound(CreateError(error)),
            "pricing.tax_aggregate.code_exists" or "pricing.tax_aggregate.rate_exists"
                or "pricing.tax_aggregate.duplicate_effective_from" or "pricing.tax_aggregate.delete_restricted"
                or "pricing.tax_aggregate.treatment_locked" or "pricing.tax_aggregate.code_locked"
                or "pricing.tax_aggregate.rate_immutable" => Conflict(CreateError(error)),
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
