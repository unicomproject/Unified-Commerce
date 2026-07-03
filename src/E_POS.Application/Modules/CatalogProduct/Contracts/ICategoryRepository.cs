using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface ICategoryRepository
{
    Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken);
    Task<bool> ParentCategoryExistsAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken);
    Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken);
    Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<CategoryListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}