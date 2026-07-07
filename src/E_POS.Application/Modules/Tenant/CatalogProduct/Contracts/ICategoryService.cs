using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICategoryService
{
    Task<ApplicationResult<CategoryResponse>> CreateAsync(TenantRequestContext context, CategoryCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<CategoryListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<CategoryResponse>> GetByIdAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken);
    Task<ApplicationResult<CategoryResponse>> UpdateAsync(TenantRequestContext context, Guid categoryId, CategoryUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken);
}
