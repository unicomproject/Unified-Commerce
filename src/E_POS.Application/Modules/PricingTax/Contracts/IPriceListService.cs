using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IPriceListService
{
    Task<ApplicationResult<PriceListResponse>> CreateAsync(TenantRequestContext context, PriceListCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<PriceListListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<PriceListResponse>> GetByIdAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<PriceListResponse>> UpdateAsync(TenantRequestContext context, Guid id, PriceListUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<Guid>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}
