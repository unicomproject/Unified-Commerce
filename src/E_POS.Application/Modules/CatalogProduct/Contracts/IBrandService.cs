using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IBrandService
{
    Task<ApplicationResult<BrandResponse>> CreateAsync(TenantRequestContext context, BrandCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<BrandListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<BrandResponse>> GetByIdAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken);
    Task<ApplicationResult<BrandResponse>> UpdateAsync(TenantRequestContext context, Guid brandId, BrandUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken);
}