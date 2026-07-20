using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/catalog/categories")]
public class StorefrontCategoriesController : ControllerBase
{
    private readonly IStorefrontCategoryService _storefrontCategoryService;

    public StorefrontCategoriesController(IStorefrontCategoryService storefrontCategoryService)
    {
        _storefrontCategoryService = storefrontCategoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromHeader(Name = "X-Tenant-Id")] Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });

        var categories = await _storefrontCategoryService.GetRootCategoriesAsync(tenantId, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{categoryId:guid}/children")]
    public async Task<IActionResult> GetChildCategories(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromRoute] Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });
        if (categoryId == Guid.Empty) return BadRequest(new { Error = "categoryId is required" });

        var categories = await _storefrontCategoryService.GetChildCategoriesAsync(tenantId, categoryId, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedCategories([FromHeader(Name = "X-Tenant-Id")] Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });

        var categories = await _storefrontCategoryService.GetFeaturedCategoriesAsync(tenantId, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetCategoryBySlug([FromHeader(Name = "X-Tenant-Id")] Guid tenantId, [FromRoute] string slug, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) return BadRequest(new { Error = "X-Tenant-Id header is required" });
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest(new { Error = "slug is required" });

        var category = await _storefrontCategoryService.GetCategoryBySlugAsync(tenantId, slug, cancellationToken);
        
        if (category == null)
            return NotFound(new { Error = "Category not found" });

        return Ok(category);
    }
}