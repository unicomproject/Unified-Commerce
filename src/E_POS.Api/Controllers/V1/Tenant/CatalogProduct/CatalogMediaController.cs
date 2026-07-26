using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1")]
public sealed class CatalogMediaController : ControllerBase
{
    private readonly ICatalogMediaService _catalogMediaService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public CatalogMediaController(
        ICatalogMediaService catalogMediaService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _catalogMediaService = catalogMediaService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost("products/{productId:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaAssetUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadProductImage(
        Guid productId,
        IFormFile? file,
        [FromForm] Guid? productVariantId = null,
        [FromForm] Guid? salesChannelId = null,
        [FromForm] string? altText = null,
        [FromForm] string? imagePurpose = null,
        [FromForm] int? sortOrder = null,
        [FromForm] bool? isPrimaryImage = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "media.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var fileError = ValidateFile(file);
        if (fileError is not null)
        {
            return BadRequest(CreateError(fileError));
        }

        await using var stream = file!.OpenReadStream();
        var result = await _catalogMediaService.UploadProductImageAsync(
            context,
            productId,
            new ProductImageUploadRequest(
                productVariantId,
                salesChannelId,
                altText,
                imagePurpose,
                sortOrder,
                isPrimaryImage),
            new MediaUploadFile(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPost("categories/{categoryId:guid}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaAssetUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadCategoryImage(
        Guid categoryId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "media.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var fileError = ValidateFile(file);
        if (fileError is not null)
        {
            return BadRequest(CreateError(fileError));
        }

        await using var stream = file!.OpenReadStream();
        var result = await _catalogMediaService.UploadCategoryImageAsync(
            context,
            categoryId,
            new MediaUploadFile(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPost("brands/{brandId:guid}/logo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaAssetUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadBrandLogo(
        Guid brandId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "media.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var fileError = ValidateFile(file);
        if (fileError is not null)
        {
            return BadRequest(CreateError(fileError));
        }

        await using var stream = file!.OpenReadStream();
        var result = await _catalogMediaService.UploadBrandLogoAsync(
            context,
            brandId,
            new MediaUploadFile(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    private static ApplicationError? ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return new ApplicationError(
                "media.validation_failed",
                "Image validation failed.",
                [new ApplicationFieldError("file", "Image file is required.")]);
        }

        return null;
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "media.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "media.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "media.product_not_found" or
                "media.variant_not_found" or
                "media.category_not_found" or
                "media.brand_not_found" => NotFound(CreateError(error)),
            "media.storage_not_configured" => StatusCode(StatusCodes.Status500InternalServerError, CreateError(error)),
            _ => BadRequest(CreateError(error))
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
