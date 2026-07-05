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
[Route("api/v1/tax/classes")]
public class TaxClassesController : ControllerBase
{
    private readonly ITaxSetupService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TaxClassesController(ITaxSetupService service, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTaxClass([FromBody] TaxClassCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.CreateTaxClassAsync(context, request, cancellationToken);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetTaxClass), new { id = result.Value }, result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTaxClass(Guid id, [FromBody] TaxClassUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.UpdateTaxClassAsync(context, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTaxClass(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetTaxClassAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetTaxClasses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetTaxClassesAsync(context, pageNumber, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTaxClass(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.DeleteTaxClassAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pricing.tax_setup.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pricing.tax_setup.not_found" => NotFound(CreateError(error)),
            "pricing.tax_class.code_exists" => Conflict(CreateError(error)),
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
