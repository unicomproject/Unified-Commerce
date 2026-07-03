using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface ICollectionService
{
    Task<ApplicationResult<CollectionResponse>> CreateAsync(TenantRequestContext context, CollectionCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<CollectionListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<CollectionResponse>> GetByIdAsync(TenantRequestContext context, Guid collectionId, CancellationToken cancellationToken);
    Task<ApplicationResult<CollectionResponse>> UpdateAsync(TenantRequestContext context, Guid collectionId, CollectionUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid collectionId, CancellationToken cancellationToken);
}