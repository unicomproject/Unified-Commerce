using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class PosProductCatalogService : IPosProductCatalogService
{
    private static readonly ApplicationError InvalidDeviceId = new(
        "pos_products.invalid_device_id",
        "Device id is required.");

    private static readonly ApplicationError ViewPermissionDenied = new(
        "pos_products.permission_denied",
        "You do not have permission to view POS products.");

    private static readonly ApplicationError SearchPermissionDenied = new(
        "pos_products.permission_denied",
        "You do not have permission to search POS products.");

    private static readonly ApplicationError DeviceNotFound = new(
        "pos_products.device_not_found",
        "POS device could not be found.");

    private readonly IPosProductCatalogRepository _repository;

    public PosProductCatalogService(IPosProductCatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>> ListProductsAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken)
    {
        if (!HasViewPermission(context))
        {
            return ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(ViewPermissionDenied);
        }

        if (!string.IsNullOrWhiteSpace(search) && !HasSearchPermission(context))
        {
            return ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(SearchPermissionDenied);
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(InvalidDeviceId);
        }

        var result = await _repository.ListProductsAsync(
            context.TenantId,
            deviceId.Value,
            categoryId,
            search,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Failure(
                result.ErrorCode switch
                {
                    "pos_products.device_not_found" => DeviceNotFound,
                    _ => new ApplicationError(
                        result.ErrorCode ?? "pos_products.list_failed",
                        "POS products could not be loaded.")
                });
        }

        return ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>.Success(result.Products);
    }

    public async Task<ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>> ListCategoriesAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!HasViewPermission(context))
        {
            return ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>.Failure(ViewPermissionDenied);
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>.Failure(InvalidDeviceId);
        }

        var result = await _repository.ListCategoriesAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>.Failure(
                result.ErrorCode switch
                {
                    "pos_products.device_not_found" => DeviceNotFound,
                    _ => new ApplicationError(
                        result.ErrorCode ?? "pos_catalog.categories_failed",
                        "POS catalog categories could not be loaded.")
                });
        }

        return ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>.Success(result.Categories);
    }

    private static bool HasViewPermission(TenantRequestContext context) =>
        context.HasPermission(ProductPosPermissions.View) ||
        context.HasPermission(ProductConstants.ViewPermission);

    private static bool HasSearchPermission(TenantRequestContext context) =>
        context.HasPermission(ProductPosPermissions.Search) || HasViewPermission(context);
}
