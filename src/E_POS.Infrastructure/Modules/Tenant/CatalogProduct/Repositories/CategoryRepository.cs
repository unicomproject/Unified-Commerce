using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct;
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

    public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);
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

    public Task<bool> CategoryNameExistsAsync(Guid tenantId, string categoryName, Guid? excludeCategoryId, CancellationToken cancellationToken)
    {
        var normalized = CategoryConstants.NormalizeNameForComparison(categoryName);
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.CategoryName.Trim().ToLower() == normalized &&
                     (!excludeCategoryId.HasValue || x.Id != excludeCategoryId.Value),
                cancellationToken);
    }

    public Task<bool> CategorySlugExistsAsync(Guid tenantId, string categorySlug, Guid? excludeCategoryId, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.CategorySlug == categorySlug &&
                     (!excludeCategoryId.HasValue || x.Id != excludeCategoryId.Value),
                cancellationToken);
    }

    public async Task<CategoryParentInfo?> GetParentInfoAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken)
    {
        var parent = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == parentCategoryId && x.Status != CategoryConstants.DeletedStatus)
            .Select(x => new { x.Id, x.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (parent is null)
        {
            return null;
        }

        var parentMap = await LoadParentMapAsync(tenantId, includeDeleted: false, cancellationToken);
        var level = CategoryHierarchy.ComputeLevel(parent.Id, parentMap);
        return new CategoryParentInfo(parent.Id, parent.Status, level);
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

    public async Task<int> GetSubtreeRelativeDepthAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        var links = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != CategoryConstants.DeletedStatus)
            .Select(x => new { x.Id, x.ParentCategoryId })
            .ToListAsync(cancellationToken);

        var childrenByParent = links
            .Where(x => x.ParentCategoryId.HasValue)
            .GroupBy(x => x.ParentCategoryId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Guid>)g.Select(x => x.Id).ToList());

        return CategoryHierarchy.ComputeSubtreeRelativeDepth(categoryId, childrenByParent);
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

    public async Task<CategoryListResponse> ListAsync(Guid tenantId, CategoryListQuery query, CancellationToken cancellationToken)
    {
        var categories = _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != CategoryConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = CategoryConstants.NormalizeStatus(query.Status);
            categories = categories.Where(x => x.Status == status);
        }

        if (query.RootOnly)
        {
            categories = categories.Where(x => x.ParentCategoryId == null);
        }
        else if (query.ParentCategoryId.HasValue)
        {
            categories = categories.Where(x => x.ParentCategoryId == query.ParentCategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
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
        var pageRows = await categories
            .OrderBy(x => x.ParentCategoryId.HasValue)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryCode)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.Id,
                x.CategoryCode,
                x.CategoryName,
                x.CategorySlug,
                x.Description,
                x.ImageMediaAssetId,
                x.Status,
                x.ParentCategoryId,
                x.SortOrder,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var pageIds = pageRows.Select(x => x.Id).ToList();
        var hierarchy = await LoadHierarchyAsync(tenantId, includeDeleted: false, cancellationToken);
        var childCounts = await LoadChildCountsAsync(tenantId, pageIds, cancellationToken);
        var productCounts = await LoadProductCountsAsync(tenantId, pageIds, cancellationToken);
        var mediaById = await LoadMediaAsync(tenantId, pageRows.Select(x => x.ImageMediaAssetId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList(), cancellationToken);

        var items = pageRows.Select(row =>
        {
            var childCount = childCounts.GetValueOrDefault(row.Id);
            var productCount = productCounts.GetValueOrDefault(row.Id);
            var level = CategoryHierarchy.ComputeLevel(row.Id, hierarchy.ParentById);
            var path = CategoryHierarchy.ComputePath(row.Id, hierarchy.NameById, hierarchy.ParentById);
            var parentCode = row.ParentCategoryId.HasValue ? hierarchy.CodeById.GetValueOrDefault(row.ParentCategoryId.Value) : null;
            var parentName = row.ParentCategoryId.HasValue ? hierarchy.NameById.GetValueOrDefault(row.ParentCategoryId.Value) : null;
            var media = row.ImageMediaAssetId.HasValue ? mediaById.GetValueOrDefault(row.ImageMediaAssetId.Value) : null;
            var hasActiveMedia = media is not null && media.Status == "ACTIVE";

            return new CategorySummaryResponse(
                row.Id,
                row.CategoryCode,
                row.CategoryName,
                hasActiveMedia ? media!.PublicUrl : null,
                hasActiveMedia ? row.ImageMediaAssetId : null,
                row.Status,
                row.ParentCategoryId,
                parentCode,
                parentName,
                row.SortOrder,
                level,
                path,
                childCount,
                productCount,
                childCount > 0,
                row.CreatedAt,
                row.UpdatedAt);
        }).ToList();

        return new CategoryListResponse(items, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<CategoryTreeResponse> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != CategoryConstants.DeletedStatus);

        var rows = await query
            .Select(x => new CategoryHierarchyLink(
                x.Id,
                x.ParentCategoryId,
                x.CategoryCode,
                x.CategoryName,
                x.Status,
                x.SortOrder))
            .ToListAsync(cancellationToken);

        var ids = rows.Select(x => x.Id).ToList();
        var productCounts = await LoadProductCountsAsync(tenantId, ids, cancellationToken);
        var parentById = rows.ToDictionary(x => x.Id, x => x.ParentCategoryId);
        var nameById = rows.ToDictionary(x => x.Id, x => x.CategoryName);
        var childrenByParent = rows
            .Where(x => x.ParentCategoryId.HasValue)
            .GroupBy(x => x.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        CategoryTreeNodeResponse Map(CategoryHierarchyLink row, int depth)
        {
            var children = depth >= CategoryConstants.MaxHierarchyDepth
                ? []
                : childrenByParent.GetValueOrDefault(row.Id) ?? [];
            var orderedChildren = children
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CategoryCode)
                .Select(child => Map(child, depth + 1))
                .ToList();
            var childCount = children.Count;
            var productCount = productCounts.GetValueOrDefault(row.Id);
            var level = CategoryHierarchy.ComputeLevel(row.Id, parentById);

            return new CategoryTreeNodeResponse(
                row.Id,
                row.CategoryCode,
                row.CategoryName,
                row.Status,
                row.ParentCategoryId,
                row.SortOrder,
                level,
                CategoryHierarchy.ComputePath(row.Id, nameById, parentById),
                childCount,
                productCount,
                childCount > 0,
                orderedChildren);
        }

        var roots = rows
            .Where(x => x.ParentCategoryId is null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryCode)
            .Select(x => Map(x, 1))
            .ToList();

        return new CategoryTreeResponse(roots);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == categoryId && (includeDeleted || x.Status != CategoryConstants.DeletedStatus))
            .Select(x => new
            {
                x.Id,
                x.CategoryCode,
                x.CategoryName,
                x.CategorySlug,
                x.Description,
                x.ImageMediaAssetId,
                x.Status,
                x.ParentCategoryId,
                x.SortOrder,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return null;
        }

        var hierarchy = await LoadHierarchyAsync(tenantId, includeDeleted, cancellationToken);
        var childCounts = await LoadChildCountsAsync(tenantId, [category.Id], cancellationToken);
        var productCounts = await LoadProductCountsAsync(tenantId, [category.Id], cancellationToken);
        var mediaById = category.ImageMediaAssetId.HasValue
            ? await LoadMediaAsync(tenantId, [category.ImageMediaAssetId.Value], cancellationToken)
            : new Dictionary<Guid, MediaProjection>();

        var childCount = childCounts.GetValueOrDefault(category.Id);
        var productCount = productCounts.GetValueOrDefault(category.Id);
        var media = category.ImageMediaAssetId.HasValue ? mediaById.GetValueOrDefault(category.ImageMediaAssetId.Value) : null;
        var hasActiveMedia = media is not null && media.Status == "ACTIVE";
        var parentCode = category.ParentCategoryId.HasValue ? hierarchy.CodeById.GetValueOrDefault(category.ParentCategoryId.Value) : null;
        var parentName = category.ParentCategoryId.HasValue ? hierarchy.NameById.GetValueOrDefault(category.ParentCategoryId.Value) : null;

        return new CategoryResponse(
            category.Id,
            category.ParentCategoryId,
            parentCode,
            parentName,
            category.CategoryCode,
            category.CategoryName,
            category.CategorySlug,
            category.Description,
            hasActiveMedia ? category.ImageMediaAssetId : null,
            hasActiveMedia ? media!.PublicUrl : null,
            category.Status,
            category.SortOrder,
            category.CreatedAt,
            category.UpdatedAt,
            CategoryHierarchy.ComputeLevel(category.Id, hierarchy.ParentById),
            CategoryHierarchy.ComputePath(category.Id, hierarchy.NameById, hierarchy.ParentById),
            childCount,
            productCount,
            childCount > 0);
    }

    public Task<Category?> GetEditableAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == categoryId && x.Status != CategoryConstants.DeletedStatus, cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        _dbContext.Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        _dbContext.MediaAssets.Add(mediaAsset);
        return Task.CompletedTask;
    }

    public async Task MarkMediaAssetInactiveAsync(
        Guid tenantId,
        Guid mediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var mediaAsset = await _dbContext.MediaAssets
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == mediaAssetId, cancellationToken);

        mediaAsset?.MarkInactive(updatedByTenantUserId, now);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, Guid?>> LoadParentMapAsync(Guid tenantId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var hierarchy = await LoadHierarchyAsync(tenantId, includeDeleted, cancellationToken);
        return hierarchy.ParentById;
    }

    private async Task<HierarchyMaps> LoadHierarchyAsync(Guid tenantId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && (includeDeleted || x.Status != CategoryConstants.DeletedStatus))
            .Select(x => new { x.Id, x.ParentCategoryId, x.CategoryCode, x.CategoryName })
            .ToListAsync(cancellationToken);

        return new HierarchyMaps(
            rows.ToDictionary(x => x.Id, x => x.ParentCategoryId),
            rows.ToDictionary(x => x.Id, x => x.CategoryName),
            rows.ToDictionary(x => x.Id, x => x.CategoryCode));
    }

    private async Task<Dictionary<Guid, int>> LoadChildCountsAsync(Guid tenantId, IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ParentCategoryId.HasValue && categoryIds.Contains(x.ParentCategoryId.Value) && x.Status != CategoryConstants.DeletedStatus)
            .GroupBy(x => x.ParentCategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> LoadProductCountsAsync(Guid tenantId, IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return [];
        }

        return await (from productCategory in _dbContext.ProductCategories.AsNoTracking()
                      join product in _dbContext.Products.AsNoTracking() on productCategory.ProductId equals product.Id
                      where product.TenantId == tenantId &&
                            categoryIds.Contains(productCategory.CategoryId) &&
                            product.Status != "DELETED"
                      group productCategory by productCategory.CategoryId into grouped
                      select new { CategoryId = grouped.Key, Count = grouped.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);
    }

    private async Task<Dictionary<Guid, MediaProjection>> LoadMediaAsync(Guid tenantId, IReadOnlyList<Guid> mediaAssetIds, CancellationToken cancellationToken)
    {
        if (mediaAssetIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Set<MediaAsset>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && mediaAssetIds.Contains(x.Id))
            .Select(x => new MediaProjection(x.Id, x.Status, x.PublicUrl))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private sealed record HierarchyMaps(
        Dictionary<Guid, Guid?> ParentById,
        Dictionary<Guid, string> NameById,
        Dictionary<Guid, string> CodeById);

    private sealed record MediaProjection(Guid Id, string Status, string? PublicUrl);
}
