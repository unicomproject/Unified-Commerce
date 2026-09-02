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

    public async Task<(Guid? TenantId, string? BaseCurrencyCode)> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim();
        var tenant = await _dbContext.Set<TenantEntity>()
            .AsNoTracking()
            .Select(t => new { t.Id, t.TenantSlug, t.Status, t.BaseCurrencyCode })
            .FirstOrDefaultAsync(t => t.TenantSlug == normalizedSlug && t.Status.ToLower() == TenantStatusConstants.Active, cancellationToken);

        return (tenant?.Id, tenant?.BaseCurrencyCode);
    }

    public async Task<(Guid? TenantId, string? BaseCurrencyCode)> GetTenantIdByHostAsync(string host, CancellationToken cancellationToken = default)
    {
        var normalizedHost = host.Trim().TrimEnd('.').ToLowerInvariant();

        var tenant = await (
            from domain in _dbContext.TenantDomains.AsNoTracking()
            join channel in _dbContext.SalesChannels.AsNoTracking()
                on domain.SalesChannelId equals channel.Id
            join platformChannel in _dbContext.PlatformSalesChannels.AsNoTracking()
                on channel.PlatformSalesChannelId equals platformChannel.Id
            join candidate in _dbContext.Tenants.AsNoTracking()
                on domain.TenantId equals candidate.Id
            where domain.DomainName.ToLower() == normalizedHost
                && domain.VerificationStatus == "VERIFIED"
                && domain.SslStatus == "ACTIVE"
                && domain.Status == "ACTIVE"
                && channel.Status == "ACTIVE"
                && platformChannel.ChannelCode == "ONLINE"
                && candidate.Status.ToLower() == TenantStatusConstants.Active
            select new { candidate.Id, candidate.BaseCurrencyCode })
            .FirstOrDefaultAsync(cancellationToken);

        return (tenant?.Id, tenant?.BaseCurrencyCode);
    }
}
