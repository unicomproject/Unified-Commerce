using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/catalog/products")]
public class StorefrontProductsController : ControllerBase
{
    private readonly IStorefrontProductService _storefrontProductService;

    public StorefrontProductsController(IStorefrontProductService storefrontProductService)
    {
        _storefrontProductService = storefrontProductService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromQuery] Guid categoryId,
        [FromQuery] string? sort,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });
        if (categoryId == Guid.Empty) return BadRequest(new { Error = "categoryId is required" });

        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 50);
        var products = await _storefrontProductService.GetProductsAsync(tenantId, categoryId, sort, normalizedPage, normalizedPageSize, cancellationToken);
        return Ok(products);
    }

    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellers([FromHeader(Name = "X-Tenant-Id")] Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });

        var products = await _storefrontProductService.GetBestSellersAsync(tenantId, cancellationToken);
        return Ok(products);
    }

    [HttpGet("/api/v1/ecommerce/storefront/catalog/search")]
    public async Task<IActionResult> Search([FromHeader(Name = "X-Tenant-Id")] Guid tenantId, [FromQuery] StorefrontSearchRequest request, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });
        if (request.MinPrice < 0 || request.MaxPrice < 0) return BadRequest(new { Error = "Price range cannot be negative" });
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
            return BadRequest(new { Error = "minPrice cannot be greater than maxPrice" });
        request.Page = request.Page < 1 ? 1 : request.Page;
        request.PageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 50);
        return Ok(await _storefrontProductService.SearchAsync(tenantId, request, cancellationToken));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetProductDetail(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest(new { Error = "slug is required" });

        var product = await _storefrontProductService.GetProductDetailAsync(tenantId, slug, cancellationToken);
        if (product is null) return NotFound(new { Error = "Product not found" });

        return Ok(product);
    }
}
