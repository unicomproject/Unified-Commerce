using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Entities;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IPriceListRepository
{
    Task<bool> PriceListCodeExistsAsync(Guid tenantId, string code, Guid? excludePriceListId, CancellationToken cancellationToken);
    Task<PriceList?> GetEditableAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PriceListResponse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PriceListListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task AddAsync(PriceList priceList, CancellationToken cancellationToken);
    Task AddOutletAssignmentsAsync(IEnumerable<PriceListOutlet> assignments, CancellationToken cancellationToken);
    Task AddChannelAssignmentsAsync(IEnumerable<PriceListChannel> assignments, CancellationToken cancellationToken);
    Task ClearAssignmentsAsync(Guid priceListId, CancellationToken cancellationToken);
    Task ClearOtherDefaultsAsync(Guid tenantId, Guid defaultPriceListId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
