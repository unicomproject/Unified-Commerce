using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontAutocompleteService
{
    StorefrontSearchReadModel GetSuggestions(Guid tenantId, string query, int limit = 10);
    Task LoadDataAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task LoadAllTenantsAsync(CancellationToken cancellationToken = default);
}
