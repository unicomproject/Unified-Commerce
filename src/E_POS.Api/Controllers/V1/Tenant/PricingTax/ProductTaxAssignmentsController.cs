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
[Route("api/v1/pricing/product-tax-assignments")]
public class ProductTaxAssignmentsController : ControllerBase
{
    private readonly IProductTaxAssignmentService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public ProductTaxAssignmentsController(IProductTaxAssignmentService service, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductTaxAssignmentCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.product_tax_assignment.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.CreateAsync(context, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value }, result.Value)
            : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductTaxAssignmentUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.product_tax_assignment.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.UpdateAsync(context, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.product_tax_assignment.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.product_tax_assignment.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetByProductAsync(context, productId, pageNumber, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.product_tax_assignment.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pricing.product_tax_assignment.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pricing.product_tax_assignment.not_found" => NotFound(CreateError(error)),
            "pricing.product_tax_assignment.product_not_found" => BadRequest(CreateError(error)),
            "pricing.product_tax_assignment.tax_class_not_found" => BadRequest(CreateError(error)),
            "pricing.product_tax_assignment.overlap" => Conflict(CreateError(error)),
            "pricing.product_tax_assignment.invalid_tenant_context" => Unauthorized(CreateError(error)),
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


