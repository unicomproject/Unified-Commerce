using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.PricingTax;

/// <summary>
/// Controller for managing tax classes within the Pricing and Tax module.
/// </summary>
/// <remarks>
/// This controller handles CRUD operations for tax classes. 
/// It enforces tenant isolation by requiring a valid tenant context for all operations.
/// </remarks>
[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tax/classes")]
public class TaxClassesController : ControllerBase
{
    private readonly ITaxSetupService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaxClassesController"/> class.
    /// </summary>
    /// <param name="service">The tax setup service handling business logic.</param>
    /// <param name="tenantRequestContextFactory">Factory to create tenant context from the current user.</param>
    public TaxClassesController(ITaxSetupService service, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    /// <summary>
    /// Creates a new tax class for the tenant.
    /// </summary>
    /// <param name="request">The tax class creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tax class ID.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTaxClass([FromBody] TaxClassCreateRequest request, CancellationToken cancellationToken)
    {
        // Rationale: Ensure the request comes from a valid tenant before processing any business logic to prevent data leaks.
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.CreateTaxClassAsync(context, request, cancellationToken);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetTaxClass), new { id = result.Value }, result.Value)
            : ToErrorResult(result.Error);
    }

    /// <summary>
    /// Updates an existing tax class.
    /// </summary>
    /// <param name="id">The unique identifier of the tax class to update.</param>
    /// <param name="request">The updated tax class payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTaxClass(Guid id, [FromBody] TaxClassUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.UpdateTaxClassAsync(context, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    /// <summary>
    /// Retrieves a specific tax class by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the tax class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested tax class details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTaxClass(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetTaxClassAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    /// <summary>
    /// Retrieves a paginated list of tax classes for the tenant.
    /// </summary>
    /// <param name="pageNumber">The current page number (1-indexed).</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of tax classes.</returns>
    [HttpGet]
    public async Task<IActionResult> GetTaxClasses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.GetTaxClassesAsync(context, pageNumber, pageSize, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    /// <summary>
    /// Deletes a tax class.
    /// </summary>
    /// <param name="id">The unique identifier of the tax class to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTaxClass(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pricing.tax_setup.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _service.DeleteTaxClassAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    /// <summary>
    /// Maps internal application errors to appropriate HTTP status codes.
    /// </summary>
    /// <param name="error">The application error.</param>
    /// <returns>An action result containing the error response.</returns>
    private IActionResult ToErrorResult(ApplicationError error)
    {
        // Rationale: Centralized error mapping prevents leaking stack traces and maps domain logic to RESTful codes.
        return error.Code switch
        {
            "pricing.tax_setup.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pricing.tax_setup.not_found" => NotFound(CreateError(error)),
            "pricing.tax_class.code_exists" => Conflict(CreateError(error)),
            "pricing.tax_setup.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    /// <summary>
    /// Creates a standardized error response payload.
    /// </summary>
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


