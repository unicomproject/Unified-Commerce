using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontCategoryRepository : IStorefrontCategoryRepository
{
    private const string ActiveStatus = "ACTIVE";
    private readonly EPosDbContext _dbContext;

    public StorefrontCategoryRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Category>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Category>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == CategoryConstants.ActiveStatus)
            .OrderBy(c => c.SortOrder)
            .Take(10)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StorefrontCategoryListReadModel>> GetRootCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await GetCategoriesByParentAsync(tenantId, null, cancellationToken);
    }

    public async Task<IEnumerable<StorefrontCategoryListReadModel>> GetChildCategoriesAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return await GetCategoriesByParentAsync(tenantId, parentCategoryId, cancellationToken);
    }

    public async Task<StorefrontCategoryListReadModel?> GetCategoryBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Set<Category>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CategorySlug == slug && c.Status == CategoryConstants.ActiveStatus, cancellationToken);

        if (category == null) return null;

        var itemCount = await GetItemCountForCategoryAsync(tenantId, category.Id, cancellationToken);
        return category.ToListReadModel(itemCount);
    }

    private async Task<int> GetItemCountForCategoryAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        var productCount = await (
                from productCategory in _dbContext.Set<ProductCategory>().AsNoTracking()
                join product in _dbContext.Set<Product>().AsNoTracking()
                    on new { productCategory.TenantId, productCategory.ProductId }
                    equals new { product.TenantId, ProductId = product.Id }
                where productCategory.TenantId == tenantId &&
                      productCategory.CategoryId == categoryId &&
                      product.Status == ActiveStatus &&
                      product.IsSellable
                select product.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return productCount;
    }

    private async Task<IEnumerable<StorefrontCategoryListReadModel>> GetCategoriesByParentAsync(Guid tenantId, Guid? parentCategoryId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Category>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == CategoryConstants.ActiveStatus);

        query = parentCategoryId.HasValue
            ? query.Where(c => c.ParentCategoryId == parentCategoryId.Value)
            : query.Where(c => c.ParentCategoryId == null);

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            return [];
        }

        var categoryIds = categories.Select(c => c.Id).ToList();
        var productCategoryRows = await (
                from productCategory in _dbContext.Set<ProductCategory>().AsNoTracking()
                join product in _dbContext.Set<Product>().AsNoTracking()
                    on new { productCategory.TenantId, productCategory.ProductId }
                    equals new { product.TenantId, ProductId = product.Id }
                where productCategory.TenantId == tenantId &&
                      categoryIds.Contains(productCategory.CategoryId) &&
                      product.Status == ActiveStatus &&
                      product.IsSellable
                select new { productCategory.CategoryId, product.Id })
            .Distinct()
            .ToListAsync(cancellationToken);

        var itemCounts = productCategoryRows
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        return categories.Select(category => category.ToListReadModel(itemCounts.TryGetValue(category.Id, out var count) ? count : 0));
    }
}