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
[Route("api/v1/tax/rates")]
public class TaxRatesController : ControllerBase
{
    private readonly ITaxSetupService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TaxRatesController(ITaxSetupService service, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTaxRate([FromBody] TaxRateCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.CreateTaxRateAsync(context, request, cancellationToken);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetTaxRate), new { id = result.Value }, result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTaxRate(Guid id, [FromBody] TaxRateUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.UpdateTaxRateAsync(context, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTaxRate(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetTaxRateAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetTaxRates([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetTaxRatesAsync(context, pageNumber, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTaxRate(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.DeleteTaxRateAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pricing.tax_setup.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pricing.tax_setup.not_found" => NotFound(CreateError(error)),
            "pricing.tax_rate.code_exists" => Conflict(CreateError(error)),
            "pricing.tax_rate.jurisdiction_not_found" => BadRequest(CreateError(error)),
            "pricing.tax_setup.invalid_tenant_context" => Unauthorized(CreateError(error)),
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


