using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IPriceListItemsService
{
    Task<ApplicationResult<PriceListItemResponse>> CreateAsync(TenantRequestContext context, PriceListItemCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<PriceListItemListResponse>> ListAsync(TenantRequestContext context, Guid priceListId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationResult<PriceListItemResponse>> GetByIdAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
    Task<ApplicationResult<PriceListItemResponse>> UpdateAsync(TenantRequestContext context, Guid id, PriceListItemUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<Guid>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken);
}
