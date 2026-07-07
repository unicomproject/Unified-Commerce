using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICollectionRepository
{
    Task<bool> CollectionCodeExistsAsync(Guid tenantId, string collectionCode, Guid? excludeCollectionId, CancellationToken cancellationToken);
    Task<bool> HasProductLinksAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken);
    Task<CollectionListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<CollectionResponse?> GetByIdAsync(Guid tenantId, Guid collectionId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Collection?> GetEditableAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken);
    Task AddAsync(Collection collection, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

