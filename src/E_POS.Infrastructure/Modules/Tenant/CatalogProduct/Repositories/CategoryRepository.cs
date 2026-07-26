using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
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
                              on new { category.TenantId, ParentCategoryId = category.ParentCategoryId }
                              equals new { parent.TenantId, ParentCategoryId = (Guid?)parent.Id } into parentJoin
                          from parent in parentJoin.DefaultIfEmpty()
                          join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                              on new { category.TenantId, MediaAssetId = category.ImageMediaAssetId }
                              equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                          from mediaAsset in mediaAssets.DefaultIfEmpty()
                          select new
                          {
                              Category = category,
                              Parent = parent,
                              JoinedMediaAssetId = mediaAsset == null ? null : (Guid?)mediaAsset.Id,
                              MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                              MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl,
                          })
            .OrderBy(x => x.Category.ParentCategoryId.HasValue)
            .ThenBy(x => x.Category.SortOrder)
            .ThenBy(x => x.Category.CategoryCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x =>
            {
                var hasActiveMedia = IsActiveMedia(
                    x.Category.ImageMediaAssetId,
                    x.JoinedMediaAssetId,
                    x.MediaStatus);

                return new CategorySummaryResponse(
                    x.Category.Id,
                    x.Category.CategoryCode,
                    x.Category.CategoryName,
                    ResolveImageUrl(
                        x.Category.ImageMediaAssetId,
                        hasActiveMedia,
                        x.MediaPublicUrl,
                        x.Category.ImageUrl),
                    hasActiveMedia ? x.JoinedMediaAssetId : null,
                    x.Category.Status,
                    x.Category.ParentCategoryId,
                    x.Parent == null ? null : x.Parent.CategoryCode,
                    x.Parent == null ? null : x.Parent.CategoryName,
                    x.Category.SortOrder,
                    x.Category.CreatedAt,
                    x.Category.UpdatedAt);
            })
            .ToList();

        return new CategoryListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid tenantId, Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var categories = _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == categoryId && (includeDeleted || x.Status != CategoryConstants.DeletedStatus));

        var row = await (from category in categories
                         join parent in _dbContext.Categories.AsNoTracking()
                             on new { category.TenantId, ParentCategoryId = category.ParentCategoryId }
                             equals new { parent.TenantId, ParentCategoryId = (Guid?)parent.Id } into parentJoin
                         from parent in parentJoin.DefaultIfEmpty()
                         join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                             on new { category.TenantId, MediaAssetId = category.ImageMediaAssetId }
                             equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                         from mediaAsset in mediaAssets.DefaultIfEmpty()
                         select new
                         {
                             Category = category,
                             Parent = parent,
                             JoinedMediaAssetId = mediaAsset == null ? null : (Guid?)mediaAsset.Id,
                             MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                             MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl,
                         })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var hasActiveMedia = IsActiveMedia(
            row.Category.ImageMediaAssetId,
            row.JoinedMediaAssetId,
            row.MediaStatus);

        return new CategoryResponse(
            row.Category.Id,
            row.Category.CategoryCode,
            row.Category.CategoryName,
            ResolveImageUrl(
                row.Category.ImageMediaAssetId,
                hasActiveMedia,
                row.MediaPublicUrl,
                row.Category.ImageUrl),
            hasActiveMedia ? row.JoinedMediaAssetId : null,
            row.Category.Status,
            row.Category.ParentCategoryId,
            row.Parent == null ? null : row.Parent.CategoryCode,
            row.Parent == null ? null : row.Parent.CategoryName,
            row.Category.SortOrder,
            row.Category.CreatedAt,
            row.Category.UpdatedAt);
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

    private static bool IsActiveMedia(
        Guid? linkedMediaAssetId,
        Guid? joinedMediaAssetId,
        string? mediaStatus)
    {
        return linkedMediaAssetId.HasValue &&
               joinedMediaAssetId == linkedMediaAssetId &&
               mediaStatus == "ACTIVE";
    }

    private static string? ResolveImageUrl(
        Guid? linkedMediaAssetId,
        bool hasActiveMedia,
        string? mediaPublicUrl,
        string? legacyImageUrl)
    {
        if (!linkedMediaAssetId.HasValue)
        {
            return MediaUrlResolver.PreferMediaAsset(null, legacyImageUrl);
        }

        return hasActiveMedia
            ? MediaUrlResolver.PreferMediaAsset(mediaPublicUrl, legacyImageUrl)
            : null;
    }
}
