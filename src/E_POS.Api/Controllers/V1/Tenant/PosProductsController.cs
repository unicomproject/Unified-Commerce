using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos")]
public sealed class PosProductsController : ControllerBase
{
    private readonly IPosProductCatalogService _posProductCatalogService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosProductsController(
        IPosProductCatalogService posProductCatalogService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _posProductCatalogService = posProductCatalogService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<PosProductSummaryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] Guid? deviceId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_products.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posProductCatalogService.ListProductsAsync(
            context,
            deviceId,
            categoryId,
            search,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value ?? Array.Empty<PosProductSummaryResponseDto>() });
    }

    [HttpGet("catalog/categories")]
    [ProducesResponseType(typeof(IReadOnlyList<PosCatalogCategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListCategories(
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_products.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posProductCatalogService.ListCategoriesAsync(
            context,
            deviceId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value ?? Array.Empty<PosCatalogCategoryResponseDto>() });
    }

    [HttpGet("products/{productId:guid}")]
    [ProducesResponseType(typeof(PosProductDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetail(
        Guid productId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_products.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posProductCatalogService.GetProductDetailAsync(
            context,
            deviceId,
            productId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("products/by-barcode/{barcode}")]
    [ProducesResponseType(typeof(PosBarcodeProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetProductByBarcode(
        string barcode,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_products.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posProductCatalogService.GetProductByBarcodeAsync(
            context,
            deviceId,
            barcode,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pos_products.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_products.device_not_found" => NotFound(CreateError(error)),
            "pos_products.product_not_found" => NotFound(CreateError(error)),
            "pos_products.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "pos_barcode.not_found" => NotFound(CreateError(error)),
            "pos_barcode.ambiguous" => Conflict(CreateError(error)),
            "pos_product.unavailable" or "pos_variant.unavailable" or "pos_price.unavailable" =>
                UnprocessableEntity(CreateError(error)),
            "pos_device.invalid" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
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
