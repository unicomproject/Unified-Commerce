using System.Security.Claims;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;
using E_POS.Application.Modules.ECommerce.ProductReviews.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.ProductReviews;

[ApiController]
[Route("api/v1/ecommerce/storefront")]
public sealed class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _service;

    public ProductReviewsController(IProductReviewService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet("catalog/products/{productId:guid}/reviews")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviews(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromRoute] Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = "newest",
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAsync(
            tenantId, productId, page, pageSize, sort, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return Ok(Success("Product reviews retrieved successfully.", result.Value));

        return ErrorResult(result.Error, isPublicRequest: true);
    }

    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("catalog/products/{productId:guid}/reviews")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReview(
        [FromRoute] Guid productId,
        [FromBody] CreateProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _service.CreateAsync(
            tenantId, customerId, productId, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return StatusCode(
                StatusCodes.Status201Created,
                Success("Product review created successfully.", result.Value));

        return ErrorResult(result.Error, isPublicRequest: false);
    }

    [Authorize(Policy = "CustomerOnly")]
    [HttpPatch("reviews/{reviewId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReview(
        [FromRoute] Guid reviewId,
        [FromBody] UpdateProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _service.UpdateAsync(
            tenantId, customerId, reviewId, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return Ok(Success("Product review updated successfully.", result.Value));

        return ErrorResult(result.Error, isPublicRequest: false);
    }

    [Authorize(Policy = "CustomerOnly")]
    [HttpDelete("reviews/{reviewId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(
        [FromRoute] Guid reviewId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _service.DeleteAsync(
            tenantId, customerId, reviewId, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ErrorResult(result.Error, isPublicRequest: false);
    }

    private IActionResult ErrorResult(ApplicationError error, bool isPublicRequest)
    {
        var response = CreateError(error);
        return error.Code switch
        {
            "product_reviews.feature_disabled" or
            "product_reviews.purchase_required" => StatusCode(StatusCodes.Status403Forbidden, response),
            "product_reviews.tenant_unavailable" or
            "product_reviews.customer_not_found" or
            "product_reviews.product_not_found" or
            "product_reviews.review_not_found" => NotFound(response),
            "product_reviews.duplicate_review" => Conflict(response),
            "product_reviews.invalid_customer_context" when !isPublicRequest => Unauthorized(response),
            _ => BadRequest(response)
        };
    }

    private bool TryGetCustomerContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    private IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "product_reviews.invalid_customer_context",
            "A valid customer session is required.")));

    private static object Success(string message, object data) => new
    {
        success = true,
        message,
        data
    };

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
