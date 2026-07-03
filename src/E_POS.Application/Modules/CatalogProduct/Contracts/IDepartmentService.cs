using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IDepartmentService
{
    Task<ApplicationResult<DepartmentResponse>> CreateAsync(TenantRequestContext context, DepartmentCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<DepartmentListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<DepartmentResponse>> GetByIdAsync(TenantRequestContext context, Guid departmentId, CancellationToken cancellationToken);
    Task<ApplicationResult<DepartmentResponse>> UpdateAsync(TenantRequestContext context, Guid departmentId, DepartmentUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid departmentId, CancellationToken cancellationToken);
}