using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin")]
public sealed class CatalogMediaController : ControllerBase
{
    private readonly ICatalogMediaService _catalogMediaService;
    private readonly IBrandService _brandService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public CatalogMediaController(
        ICatalogMediaService catalogMediaService,
        IBrandService brandService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _catalogMediaService = catalogMediaService;
        _brandService = brandService;
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

    [HttpPut("products/{productId:guid}/images/reorder")]
    [ProducesResponseType(typeof(ProductImagesMutationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderProductImages(
        Guid productId,
        [FromBody] ReorderProductImagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "media.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _catalogMediaService.ReorderProductImagesAsync(
            context,
            productId,
            request,
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpDelete("products/{productId:guid}/images/{productImageId:guid}")]
    [ProducesResponseType(typeof(ProductImagesMutationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteProductImage(
        Guid productId,
        Guid productImageId,
        [FromQuery] long? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "media.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _catalogMediaService.DeleteProductImageAsync(
            context,
            productId,
            productImageId,
            expectedRowVersion,
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPost("products/{productId:guid}/images/replace")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImagesMutationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceProductImages(
        Guid productId,
        [FromForm] long expectedRowVersion,
        [FromForm] List<Guid>? stagedMediaAssetIds = null,
        List<IFormFile>? files = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "media.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var uploadFiles = new List<MediaUploadFile>();
        var streams = new List<Stream>();
        try
        {
            if (files is { Count: > 0 })
            {
                foreach (var file in files)
                {
                    var fileError = ValidateFile(file);
                    if (fileError is not null)
                    {
                        return BadRequest(CreateError(fileError));
                    }

                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    uploadFiles.Add(new MediaUploadFile(stream, file.FileName, file.ContentType, file.Length));
                }
            }

            var result = await _catalogMediaService.ReplaceProductImagesAsync(
                context,
                productId,
                expectedRowVersion,
                uploadFiles.Count == 0 ? null : uploadFiles,
                stagedMediaAssetIds,
                cancellationToken);

            return result.IsSuccess && result.Value is not null
                ? Ok(new { data = result.Value })
                : ToErrorResult(result.Error);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
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

    /// <summary>
    /// Uploads/replaces Brand logo and returns the refreshed Brand contract
    /// expected by Tenant Admin Flutter (<c>BrandDto</c>).
    /// </summary>
    [HttpPost("brands/{brandId:guid}/logo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status200OK)]
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
        var uploadResult = await _catalogMediaService.UploadBrandLogoAsync(
            context,
            brandId,
            new MediaUploadFile(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            return ToErrorResult(uploadResult.Error);
        }

        var brandResult = await _brandService.GetByIdAsync(context, brandId, cancellationToken);
        return brandResult.IsSuccess && brandResult.Value is not null
            ? Ok(new { data = brandResult.Value })
            : ToErrorResult(brandResult.Error ?? uploadResult.Error);
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
            "media.permission_denied" or "brand.permission_denied" =>
                StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "media.invalid_tenant_context" or "brand.invalid_tenant_context" =>
                Unauthorized(CreateError(error)),
            "media.product_not_found" or
                "media.variant_not_found" or
                "media.category_not_found" or
                "media.brand_not_found" or
                "brand.not_found" => NotFound(CreateError(error)),
            "media.concurrency_conflict" =>
                StatusCode(StatusCodes.Status409Conflict, CreateError(error)),
            "media.file_size_exceeded" =>
                StatusCode(StatusCodes.Status413PayloadTooLarge, CreateError(error)),
            "media.max_images_exceeded" => BadRequest(CreateError(error)),
            "media.storage_not_configured" or "media.storage_unavailable" =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, CreateError(error)),
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
