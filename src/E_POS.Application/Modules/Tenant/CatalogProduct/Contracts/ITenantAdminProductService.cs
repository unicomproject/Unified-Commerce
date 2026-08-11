using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductService
{
    Task<ApplicationResult<TenantAdminProductListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        Guid? categoryId,
        Guid? brandId,
        string? productStatus,
        string? stockStatus,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductSummaryCardsResponse>> GetSummaryAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductCreateOptionsResponse>> GetCreateOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductFilterOptionsResponse>> GetFilterOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductCreateResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid productId,
        TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductStatusUpdateResponse>> UpdateStatusAsync(
        TenantRequestContext context,
        Guid productId,
        TenantAdminProductStatusUpdateRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductDeleteResponse>> DeleteAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductDashboardResponse>> GetDashboardAsync(
        TenantRequestContext context,
        TenantAdminProductDashboardQuery query,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductDraftResponse>> SaveDraftAsync(
        TenantRequestContext context,
        SaveProductDraftRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductDraftResponse>> UpdateDraftAsync(
        TenantRequestContext context,
        Guid productId,
        SaveProductDraftRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductSetupWizardDto>> GetSetupAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken);
}
