using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
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
    private readonly ICatalogMediaService _catalogMediaService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminProductsController(
        ITenantAdminProductService tenantAdminProductService,
        ITenantAdminInventoryService tenantAdminInventoryService,
        ICatalogMediaService catalogMediaService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminProductService = tenantAdminProductService;
        _tenantAdminInventoryService = tenantAdminInventoryService;
        _catalogMediaService = catalogMediaService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] string? productStatus = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var resolvedPageNumber = pageNumber ?? page ?? 1;

        var result = await _tenantAdminProductService.ListAsync(
            context,
            search,
            categoryId,
            brandId,
            productStatus,
            stockStatus,
            resolvedPageNumber,
            pageSize,
            sortBy,
            sortDirection,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("filter-options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.GetFilterOptionsAsync(context, cancellationToken);
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

    [HttpPost("draft")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveDraft(
        [FromBody] SaveProductDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.SaveDraftAsync(context, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return Created(
                $"/api/v1/tenant-admin/products/{result.Value.ProductId}/setup",
                new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}/draft")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft(
        Guid id,
        [FromBody] SaveProductDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.UpdateDraftAsync(context, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/setup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSetup(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminProductService.GetSetupAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("images/stage")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StageImage(
        IFormFile? file,
        [FromForm] Guid? uploadSessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "product.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(CreateError(new ApplicationError(
                "media.validation_failed",
                "Image validation failed.",
                [new ApplicationFieldError("file", "Image file is required.")])));
        }

        await using var stream = file.OpenReadStream();
        var result = await _catalogMediaService.StageProductImageAsync(
            context,
            new MediaUploadFile(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            uploadSessionId,
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToMediaErrorResult(result.Error);
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
            "product.permission_denied" or "product.entitlement_denied" or "product.tenant_blocked" => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateError(error)),
            "product.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "product.invalid_product_structure" or
            "product.track_inventory_required_for_batch" or
            "product.track_inventory_required_for_expiry" or
            "product.track_inventory_required_for_serial" or
            "product.batch_required_for_expiry" or
            "product.serial_incompatible_with_batch" or
            "product.serial_incompatible_with_expiry" or
            "product.row_version_required" or
            "product.unsafe_product_structure_transition" or
            "product.unsafe_tracking_change" or
            "product.validation_failed" or
            "product.delete_blocked" => BadRequest(CreateError(error)),
            "product.not_found" => NotFound(CreateError(error)),
            "product.concurrency_conflict" or "product.duplicate_sku" or "product.duplicate_barcode" => StatusCode(
                StatusCodes.Status409Conflict,
                CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private IActionResult ToMediaErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "media.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "media.file_size_exceeded" => StatusCode(StatusCodes.Status413PayloadTooLarge, CreateError(error)),
            "media.max_images_exceeded" => BadRequest(CreateError(error)),
            "media.storage_not_configured" or "media.storage_unavailable" =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, CreateError(error)),
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
