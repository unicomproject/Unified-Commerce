using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Domain.Modules.Shared.Media.Entities;
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

    public async Task<IEnumerable<StorefrontCategoryReadModel>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await (from category in _dbContext.Set<Category>().AsNoTracking()
                          join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                              on new { category.TenantId, MediaAssetId = category.ImageMediaAssetId }
                              equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                          from mediaAsset in mediaAssets.DefaultIfEmpty()
                          where category.TenantId == tenantId && category.Status == CategoryConstants.ActiveStatus
                          orderby category.SortOrder
                          select new
                          {
                              Category = category,
                              MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                          })
            .Take(10)
            .ToListAsync(cancellationToken);

        return rows.Select(x => x.Category.ToReadModel(x.MediaPublicUrl));
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
        var row = await (from category in _dbContext.Set<Category>().AsNoTracking()
                         join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                             on new { category.TenantId, MediaAssetId = category.ImageMediaAssetId }
                             equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                         from mediaAsset in mediaAssets.DefaultIfEmpty()
                         where category.TenantId == tenantId &&
                               category.CategorySlug == slug &&
                               category.Status == CategoryConstants.ActiveStatus
                         select new
                         {
                             Category = category,
                             MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                         })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return null;

        var itemCount = await GetItemCountForCategoryAsync(tenantId, row.Category.Id, cancellationToken);
        return row.Category.ToListReadModel(itemCount, row.MediaPublicUrl);
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
        var categoryRows = await (from category in _dbContext.Set<Category>().AsNoTracking()
                                  join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                                      on new { category.TenantId, MediaAssetId = category.ImageMediaAssetId }
                                      equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                                  from mediaAsset in mediaAssets.DefaultIfEmpty()
                                  where category.TenantId == tenantId &&
                                        category.Status == CategoryConstants.ActiveStatus &&
                                        (parentCategoryId.HasValue
                                            ? category.ParentCategoryId == parentCategoryId.Value
                                            : category.ParentCategoryId == null)
                                  orderby category.SortOrder, category.CategoryName
                                  select new
                                  {
                                      Category = category,
                                      MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                                  })
            .ToListAsync(cancellationToken);

        if (categoryRows.Count == 0)
        {
            return [];
        }

        var categoryIds = categoryRows.Select(c => c.Category.Id).ToList();
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

        return categoryRows.Select(row =>
            row.Category.ToListReadModel(
                itemCounts.TryGetValue(row.Category.Id, out var count) ? count : 0,
                row.MediaPublicUrl));
    }
}