using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;

namespace E_POS.Application.Modules.Tenant.PricingTax.Contracts;

public interface ITaxAggregateService
{
    Task<ApplicationResult<Guid>> CreateTaxAsync(TenantRequestContext context, TaxAggregateCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> UpdateTaxAsync(TenantRequestContext context, Guid id, TaxAggregateUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxAggregateResponse>> GetTaxAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxAggregateListResponse>> GetTaxesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteTaxAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}
