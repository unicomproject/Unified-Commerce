using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public BrandsController(IBrandService brandService, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _brandService = brandService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] BrandCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("brand.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _brandService.CreateAsync(context, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(typeof(BrandListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("brand.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _brandService.ListAsync(context, pageNumber, pageSize, search, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("brand.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _brandService.GetByIdAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] BrandUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("brand.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _brandService.UpdateAsync(context, id, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("brand.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _brandService.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "brand.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "brand.not_found" => NotFound(CreateError(error)),
            "brand.duplicate_code" => Conflict(CreateError(error)),
            "brand.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new { code = error.Code, message = error.Message, details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow };
    }
}