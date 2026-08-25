using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.CatalogProduct;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin/catalog/products/{productId:guid}/variants")]
public sealed class ProductVariantsController : ControllerBase
{
    private readonly ITenantAdminProductService _tenantAdminProductService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public ProductVariantsController(ITenantAdminProductService tenantAdminProductService, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminProductService = tenantAdminProductService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPut("{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVariant(
        Guid productId,
        Guid variantId,
        [FromBody] TenantAdminProductVariantUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _tenantAdminProductService.UpdateVariantAsync(context, productId, variantId, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "product.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "product.tenant_inactive" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "product.not_found" => NotFound(CreateError(error)),
            "product.invalid_tenant_context" => Unauthorized(CreateError(error)),
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
            timestamp = DateTimeOffset.UtcNow
        };
    }
}
