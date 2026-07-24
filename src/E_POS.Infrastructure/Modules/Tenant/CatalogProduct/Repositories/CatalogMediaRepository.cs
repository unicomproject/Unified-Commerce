using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class CatalogMediaRepository : ICatalogMediaRepository
{
    private readonly EPosDbContext _dbContext;

    public CatalogMediaRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ProductExistsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<bool> ProductVariantExistsAsync(
        Guid tenantId,
        Guid productId,
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductVariants
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.Id == productVariantId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<Category?> GetCategoryForImageUpdateAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == categoryId &&
                     x.Status != CategoryConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<Brand?> GetBrandForLogoUpdateAsync(
        Guid tenantId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Brands
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == brandId &&
                     x.Status != BrandConstants.DeletedStatus,
                cancellationToken);
    }

    public Task AddMediaAssetAsync(
        MediaAsset mediaAsset,
        CancellationToken cancellationToken)
    {
        _dbContext.MediaAssets.Add(mediaAsset);
        return Task.CompletedTask;
    }

    public Task AddProductImageAsync(
        ProductImage productImage,
        CancellationToken cancellationToken)
    {
        _dbContext.ProductImages.Add(productImage);
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
}
