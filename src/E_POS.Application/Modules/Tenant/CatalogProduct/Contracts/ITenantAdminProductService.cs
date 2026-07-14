using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductService
{
    Task<ApplicationResult<TenantAdminProductListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductSummaryCardsResponse>> GetSummaryAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminProductCreateOptionsResponse>> GetCreateOptionsAsync(
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
}
