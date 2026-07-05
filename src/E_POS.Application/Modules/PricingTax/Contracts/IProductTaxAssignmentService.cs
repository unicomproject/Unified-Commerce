using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IProductTaxAssignmentService
{
    Task<ApplicationResult<Guid>> CreateAsync(TenantRequestContext context, ProductTaxAssignmentCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> UpdateAsync(TenantRequestContext context, Guid id, ProductTaxAssignmentUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductTaxAssignmentResponse>> GetAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<ProductTaxAssignmentListResponse>> GetByProductAsync(TenantRequestContext context, Guid productId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}
