using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.CatalogProduct;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin/catalog/products/{productId:guid}/variants/{variantId:guid}/barcodes")]
public sealed class ProductBarcodesController : ControllerBase
{
    private readonly ITenantAdminProductService _tenantAdminProductService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public ProductBarcodesController(ITenantAdminProductService tenantAdminProductService, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminProductService = tenantAdminProductService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddBarcode(
        Guid productId,
        Guid variantId,
        [FromBody] TenantAdminProductBarcodeAddRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _tenantAdminProductService.AddBarcodeAsync(context, productId, variantId, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }

        return ToErrorResult(result.Error);
    }

    [HttpDelete("{barcodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBarcode(
        Guid productId,
        Guid variantId,
        Guid barcodeId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _tenantAdminProductService.DeleteBarcodeAsync(context, productId, variantId, barcodeId, cancellationToken);
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
            "product.duplicate_barcode" => Conflict(CreateError(error)),
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
