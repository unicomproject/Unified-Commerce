using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.CatalogProduct;

[ApiController]
[Route("api/v1/tenant-admin/products")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminProductsController : ControllerBase
{
    private readonly ITenantAdminProductService _tenantAdminProductService;
    private readonly ITenantAdminInventoryService _tenantAdminInventoryService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminProductsController(
        ITenantAdminProductService tenantAdminProductService,
        ITenantAdminInventoryService tenantAdminInventoryService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminProductService = tenantAdminProductService;
        _tenantAdminInventoryService = tenantAdminInventoryService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.ListAsync(
            context,
            search,
            page,
            pageSize,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.GetSummaryAsync(context, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] Guid? outletId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new TenantAdminProductDashboardQuery(
            outletId,
            dateFrom ?? today,
            dateTo ?? today);

        var result = await _tenantAdminProductService.GetDashboardAsync(context, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("create-options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreateOptions(CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.GetCreateOptionsAsync(context, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.CreateAsync(context, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return Created(
                $"/api/v1/tenant-admin/products/{result.Value.ProductId}",
                new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.GetByIdAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.UpdateAsync(context, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] TenantAdminProductStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.UpdateStatusAsync(context, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{productId:guid}/variants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariants(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminInventoryService.GetProductVariantsForStockInAsync(
            context,
            productId,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "inventory.permission_denied" => StatusCode(
                    StatusCodes.Status403Forbidden,
                    CreateError(result.Error)),
                "inventory.not_found" => NotFound(CreateError(result.Error)),
                _ => BadRequest(CreateError(result.Error)),
            };
        }

        return Ok(new { data = result.Value });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.DeleteAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "product.permission_denied" => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateError(error)),
            "product.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "product.validation_failed" => BadRequest(CreateError(error)),
            "product.delete_blocked" => BadRequest(CreateError(error)),
            "product.not_found" => NotFound(CreateError(error)),
            "product.duplicate_sku" or "product.duplicate_barcode" => StatusCode(
                StatusCodes.Status409Conflict,
                CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private object CreateError(ApplicationError error)
    {
        var fieldErrors = error.FieldErrors?
            .Select(item => new { field = item.Field, message = item.Message })
            .ToArray<object>() ?? Array.Empty<object>();

        return new
        {
            code = error.Code,
            message = error.Message,
            details = fieldErrors,
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
