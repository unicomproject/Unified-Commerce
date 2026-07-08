using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IDepartmentRepository
{
    Task<bool> DepartmentCodeExistsAsync(Guid tenantId, string departmentCode, Guid? excludeDepartmentId, CancellationToken cancellationToken);
    Task<DepartmentListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<DepartmentResponse?> GetByIdAsync(Guid tenantId, Guid departmentId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Department?> GetEditableAsync(Guid tenantId, Guid departmentId, CancellationToken cancellationToken);
    Task AddAsync(Department department, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

