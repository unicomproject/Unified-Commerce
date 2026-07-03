using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IReturnPolicyRepository
{
    Task<bool> PolicyCodeExistsAsync(Guid tenantId, string policyCode, Guid? excludePolicyId, CancellationToken cancellationToken);
    Task<ReturnPolicyListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ReturnPolicyResponse?> GetByIdAsync(Guid tenantId, Guid policyId, bool includeDeleted, CancellationToken cancellationToken);
    Task<ReturnPolicy?> GetEditableAsync(Guid tenantId, Guid policyId, CancellationToken cancellationToken);
    Task AddAsync(ReturnPolicy policy, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}