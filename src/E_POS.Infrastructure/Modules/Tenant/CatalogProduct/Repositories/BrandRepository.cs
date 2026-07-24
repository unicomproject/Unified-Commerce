using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly EPosDbContext _dbContext;

    public BrandRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> BrandCodeExistsAsync(Guid tenantId, string brandCode, Guid? excludeBrandId, CancellationToken cancellationToken)
    {
        return _dbContext.Brands
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.BrandCode == brandCode &&
                     (!excludeBrandId.HasValue || x.Id != excludeBrandId.Value),
                cancellationToken);
    }

    public async Task<BrandListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Brands
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != BrandConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.BrandName, pattern) || EF.Functions.ILike(x.BrandCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.BrandName.ToUpper().Contains(normalizedTerm) || x.BrandCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await (from brand in query
                          join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                              on new { brand.TenantId, MediaAssetId = brand.LogoMediaAssetId }
                              equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                          from mediaAsset in mediaAssets.DefaultIfEmpty()
                          select new
                          {
                              Brand = brand,
                              JoinedMediaAssetId = mediaAsset == null ? null : (Guid?)mediaAsset.Id,
                              MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                              MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl,
                          })
            .OrderBy(x => x.Brand.BrandCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x =>
            {
                var hasActiveMedia = IsActiveMedia(
                    x.Brand.LogoMediaAssetId,
                    x.JoinedMediaAssetId,
                    x.MediaStatus);

                return new BrandSummaryResponse(
                    x.Brand.Id,
                    x.Brand.BrandCode,
                    x.Brand.BrandName,
                    ResolveLogoUrl(
                        x.Brand.LogoMediaAssetId,
                        hasActiveMedia,
                        x.MediaPublicUrl,
                        x.Brand.LogoUrl),
                    hasActiveMedia ? x.JoinedMediaAssetId : null,
                    x.Brand.Status,
                    x.Brand.CreatedAt,
                    x.Brand.UpdatedAt);
            })
            .ToList();

        return new BrandListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task<BrandResponse?> GetByIdAsync(Guid tenantId, Guid brandId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var brands = _dbContext.Brands
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Id == brandId &&
                (includeDeleted || x.Status != BrandConstants.DeletedStatus));

        var row = await (from brand in brands
                         join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                             on new { brand.TenantId, MediaAssetId = brand.LogoMediaAssetId }
                             equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                         from mediaAsset in mediaAssets.DefaultIfEmpty()
                         select new
                         {
                             Brand = brand,
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
            row.Brand.LogoMediaAssetId,
            row.JoinedMediaAssetId,
            row.MediaStatus);

        return new BrandResponse(
            row.Brand.Id,
            row.Brand.BrandCode,
            row.Brand.BrandName,
            ResolveLogoUrl(
                row.Brand.LogoMediaAssetId,
                hasActiveMedia,
                row.MediaPublicUrl,
                row.Brand.LogoUrl),
            hasActiveMedia ? row.JoinedMediaAssetId : null,
            row.Brand.Status,
            row.Brand.CreatedAt,
            row.Brand.UpdatedAt);
    }

    public Task<Brand?> GetEditableAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken)
    {
        return _dbContext.Brands
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == brandId && x.Status != BrandConstants.DeletedStatus, cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        _dbContext.Brands.Add(brand);
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

    private static string? ResolveLogoUrl(
        Guid? linkedMediaAssetId,
        bool hasActiveMedia,
        string? mediaPublicUrl,
        string? legacyLogoUrl)
    {
        if (!linkedMediaAssetId.HasValue)
        {
            return MediaUrlResolver.PreferMediaAsset(null, legacyLogoUrl);
        }

        return hasActiveMedia
            ? MediaUrlResolver.PreferMediaAsset(mediaPublicUrl, legacyLogoUrl)
            : null;
    }
}
