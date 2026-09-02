using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICategoryRepository
{
    Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken);
    Task<bool> CategoryNameExistsAsync(Guid tenantId, string categoryName, Guid? excludeCategoryId, CancellationToken cancellationToken);
    Task<bool> CategorySlugExistsAsync(Guid tenantId, string categorySlug, Guid? excludeCategoryId, CancellationToken cancellationToken);
    Task<CategoryParentInfo?> GetParentInfoAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken);
    Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken);
    Task<int> GetSubtreeRelativeDepthAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<CategoryListResponse> ListAsync(Guid tenantId, CategoryListQuery query, CancellationToken cancellationToken);
    Task<CategoryTreeResponse> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
    Task MarkMediaAssetInactiveAsync(Guid tenantId, Guid mediaAssetId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
