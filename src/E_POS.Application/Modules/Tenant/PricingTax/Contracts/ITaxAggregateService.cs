using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;

namespace E_POS.Application.Modules.Tenant.PricingTax.Contracts;

public interface ITaxAggregateService
{
    Task<ApplicationResult<Guid>> CreateTaxAsync(TenantRequestContext context, TaxAggregateCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> UpdateTaxAsync(TenantRequestContext context, Guid id, TaxAggregateUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxAggregateResponse>> GetTaxAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxAggregateListResponse>> GetTaxesAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<ApplicationResult<Guid>> ScheduleRateAsync(TenantRequestContext context, Guid taxSetupId, TaxScheduleRateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> UpdateFutureRateAsync(TenantRequestContext context, Guid taxSetupId, Guid rateId, TaxFutureRateUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteFutureRateAsync(TenantRequestContext context, Guid taxSetupId, Guid rateId, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxStatusChangeResponse>> ActivateAsync(TenantRequestContext context, Guid taxSetupId, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxStatusChangeResponse>> DeactivateAsync(TenantRequestContext context, Guid taxSetupId, TaxStatusChangeRequest? request, CancellationToken cancellationToken);
    Task<ApplicationResult<TaxProductsUsingListResponse>> GetProductsUsingAsync(
        TenantRequestContext context,
        Guid taxSetupId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<ApplicationResult<bool>> DeleteTaxAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}
