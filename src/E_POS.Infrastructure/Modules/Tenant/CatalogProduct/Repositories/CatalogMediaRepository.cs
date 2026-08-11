using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
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
                     x.Status != ProductConstants.DeletedStatus &&
                     x.Status != ProductConstants.ArchivedStatus,
                cancellationToken);
    }

    public Task<Product?> GetProductForUpdateAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus &&
                     x.Status != ProductConstants.ArchivedStatus,
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

    public Task<int> CountActiveProductImagesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductImages
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.Status == ProductConstants.ActiveStatus &&
                     x.ProductVariantId == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductImage>> GetActiveProductImagesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductImages
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.ProductVariantId == null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductImage?> GetProductImageAsync(
        Guid tenantId,
        Guid productId,
        Guid productImageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductImages
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.Id == productImageId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<MediaAsset?> GetMediaAssetAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        return _dbContext.MediaAssets
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == mediaAssetId, cancellationToken);
    }

    public Task<bool> IsMediaAssetLinkedAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductImages
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.MediaAssetId == mediaAssetId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantAdminProductImageResponse>> GetProductImageResponsesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await (
            from image in _dbContext.ProductImages.AsNoTracking()
            join media in _dbContext.MediaAssets.AsNoTracking()
                on new { image.TenantId, MediaAssetId = image.MediaAssetId!.Value }
                equals new { media.TenantId, MediaAssetId = media.Id }
                into mediaJoin
            from media in mediaJoin.DefaultIfEmpty()
            where image.TenantId == tenantId &&
                  image.ProductId == productId &&
                  image.Status == ProductConstants.ActiveStatus &&
                  image.ProductVariantId == null
            orderby image.SortOrder, image.CreatedAt
            select new TenantAdminProductImageResponse(
                image.Id,
                image.MediaAssetId,
                image.ProductVariantId,
                media != null ? media.PublicUrl ?? string.Empty : string.Empty,
                image.AltText,
                image.ImagePurpose,
                image.SortOrder,
                image.IsPrimaryImage))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
