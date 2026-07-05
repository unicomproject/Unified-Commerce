using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface ITaxSetupService
{
    // Tax Class
    Task<ApplicationResult<Guid>> CreateTaxClassAsync(TenantRequestContext context, TaxClassCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> UpdateTaxClassAsync(TenantRequestContext context, Guid id, TaxClassUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxClassResponse>> GetTaxClassAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxClassListResponse>> GetTaxClassesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteTaxClassAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);

    // Tax Rate
    Task<ApplicationResult<Guid>> CreateTaxRateAsync(TenantRequestContext context, TaxRateCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> UpdateTaxRateAsync(TenantRequestContext context, Guid id, TaxRateUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxRateResponse>> GetTaxRateAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxRateListResponse>> GetTaxRatesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteTaxRateAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}
