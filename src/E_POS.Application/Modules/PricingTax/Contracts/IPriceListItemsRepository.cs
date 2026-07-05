using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Entities;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IPriceListItemsRepository
{
    Task<bool> ItemExistsAsync(
        Guid tenantId,
        Guid priceListId,
        Guid productId,
        Guid? variantId,
        Guid? uomId,
        decimal minQuantity,
        Guid? excludeItemId,
        CancellationToken cancellationToken);

    Task<PriceListItem?> GetEditableAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PriceListItemResponse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PriceListItemListResponse> ListAsync(Guid tenantId, Guid priceListId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(PriceListItem priceListItem, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
