using E_POS.Application.Modules.TenantFoundation.Contracts;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.TenantFoundation.Repositories;

public sealed class TenantLookupRepository : ITenantLookupRepository
{
    private readonly EPosDbContext _dbContext;

    public TenantLookupRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CurrencyExistsAsync(
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();
        return _dbContext.Currencies
            .AsNoTracking()
            .AnyAsync(x => x.CurrencyCode == normalized, cancellationToken);
    }

    public async Task<bool> AllSalesChannelsExistAsync(
        Guid tenantId,
        Guid[] channelIds,
        CancellationToken cancellationToken)
    {
        var count = await _dbContext.SalesChannels
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && channelIds.Contains(x.Id), cancellationToken);
        return count == channelIds.Length;
    }
}
