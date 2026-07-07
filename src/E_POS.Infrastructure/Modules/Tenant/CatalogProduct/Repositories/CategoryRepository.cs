using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly EPosDbContext _dbContext;

    public CategoryRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CategoryCodeExistsAsync(Guid tenantId, string categoryCode, Guid? excludeCategoryId, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.CategoryCode == categoryCode &&
                     (!excludeCategoryId.HasValue || x.Id != excludeCategoryId.Value),
                cancellationToken);
    }

    public Task<bool> ParentCategoryExistsAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == parentCategoryId && x.Status != CategoryConstants.DeletedStatus, cancellationToken);
    }

    public async Task<bool> WouldCreateParentCycleAsync(Guid tenantId, Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken)
    {
        var currentParentId = (Guid?)parentCategoryId;
        var visited = new HashSet<Guid>();

        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == categoryId) return true;
            if (!visited.Add(currentParentId.Value)) return true;

            currentParentId = await _dbContext.Categories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == currentParentId.Value && x.Status != CategoryConstants.DeletedStatus)
                .Select(x => x.ParentCategoryId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    public Task<bool> HasChildCategoriesAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.ParentCategoryId == categoryId && x.Status != CategoryConstants.DeletedStatus, cancellationToken);
    }

    public Task<bool> HasProductLinksAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        return (from productCategory in _dbContext.ProductCategories.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking() on productCategory.ProductId equals product.Id
                where product.TenantId == tenantId &&
                      productCategory.CategoryId == categoryId &&
                      product.Status != "DELETED"
                select productCategory.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<CategoryListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var categories = _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != CategoryConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                categories = categories.Where(x => EF.Functions.ILike(x.CategoryName, pattern) || EF.Functions.ILike(x.CategoryCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                categories = categories.Where(x => x.CategoryName.ToUpper().Contains(normalizedTerm) || x.CategoryCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var totalCount = await categories.CountAsync(cancellationToken);
        var rows = await (from category in categories
                          join parent in _dbContext.Categories.AsNoTracking()
                              on category.ParentCategoryId equals parent.Id into parentJoin
                          from parent in parentJoin.DefaultIfEmpty()
                          select new { Category = category, Parent = parent })
            .OrderBy(x => x.Category.ParentCategoryId.HasValue)
            .ThenBy(x => x.Category.SortOrder)
            .ThenBy(x => x.Category.CategoryCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CategorySummaryResponse(
                x.Category.Id,
                x.Category.CategoryCode,
                x.Category.CategoryName,
                x.Category.Status,
                x.Category.ParentCategoryId,
                x.Parent == null ? null : x.Parent.CategoryCode,
                x.Parent == null ? null : x.Parent.CategoryName,
                x.Category.SortOrder,
                x.Category.CreatedAt,
                x.Category.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new CategoryListResponse(rows, pageNumber, pageSize, totalCount);
    }

    public Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var categories = _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == categoryId && (includeDeleted || x.Status != CategoryConstants.DeletedStatus));

        return (from category in categories
                join parent in _dbContext.Categories.AsNoTracking()
                    on category.ParentCategoryId equals parent.Id into parentJoin
                from parent in parentJoin.DefaultIfEmpty()
                select new { Category = category, Parent = parent })
            .Select(x => new CategoryResponse(
                x.Category.Id,
                x.Category.CategoryCode,
                x.Category.CategoryName,
                x.Category.Status,
                x.Category.ParentCategoryId,
                x.Parent == null ? null : x.Parent.CategoryCode,
                x.Parent == null ? null : x.Parent.CategoryName,
                x.Category.SortOrder,
                x.Category.CreatedAt,
                x.Category.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == categoryId && x.Status != CategoryConstants.DeletedStatus, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}


