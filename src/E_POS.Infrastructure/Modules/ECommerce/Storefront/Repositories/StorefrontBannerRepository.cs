using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontBannerRepository : IStorefrontBannerRepository
{
    private const string ActiveStatus = "ACTIVE";
    private readonly EPosDbContext _dbContext;

    public StorefrontBannerRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StorefrontBannerReadModel>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
    {
        var normalizedBannerType = bannerType.Trim().ToUpperInvariant();

        var rows = await (from banner in _dbContext.Set<StorefrontBanner>().AsNoTracking()
                          join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                              on new { banner.TenantId, MediaAssetId = banner.ImageMediaAssetId }
                              equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                          from mediaAsset in mediaAssets.DefaultIfEmpty()
                          where banner.TenantId == tenantId &&
                                banner.BannerType == normalizedBannerType &&
                                banner.Status == ActiveStatus
                          orderby banner.SortOrder
                          select new
                          {
                              Banner = banner,
                              MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                          })
            .ToListAsync(cancellationToken);

        return rows.Select(x => x.Banner.ToReadModel(x.MediaPublicUrl));
    }
}