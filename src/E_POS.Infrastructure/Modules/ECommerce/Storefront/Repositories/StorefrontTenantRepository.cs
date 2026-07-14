using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontTenantRepository : IStorefrontTenantRepository
{
    private readonly EPosDbContext _dbContext;

    public StorefrontTenantRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim();
        var tenant = await _dbContext.Set<TenantEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantSlug == normalizedSlug && t.Status.ToLower() == TenantStatusConstants.Active, cancellationToken);

        return tenant?.Id;
    }
}