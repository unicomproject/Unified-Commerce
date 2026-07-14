using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
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

    public async Task<IEnumerable<StorefrontBanner>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
    {
        var normalizedBannerType = bannerType.Trim().ToUpperInvariant();

        return await _dbContext.Set<StorefrontBanner>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.BannerType == normalizedBannerType && b.Status == ActiveStatus)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);
    }
}