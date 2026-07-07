using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IReturnPolicyService
{
    Task<ApplicationResult<ReturnPolicyResponse>> CreateAsync(TenantRequestContext context, ReturnPolicyCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyResponse>> GetByIdAsync(TenantRequestContext context, Guid policyId, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyResponse>> UpdateAsync(TenantRequestContext context, Guid policyId, ReturnPolicyUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid policyId, CancellationToken cancellationToken);
}
