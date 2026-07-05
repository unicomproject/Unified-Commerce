using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Constants;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PricingTax.Repositories;

public sealed class PriceListRepository : IPriceListRepository
{
    private readonly EPosDbContext _dbContext;

    public PriceListRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> PriceListCodeExistsAsync(
        Guid tenantId,
        string code,
        Guid? excludePriceListId,
        CancellationToken cancellationToken)
    {
        return _dbContext.PriceLists
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.PriceListCode == code &&
                     x.Status != PricingTaxConstants.DeletedStatus &&
                     (!excludePriceListId.HasValue || x.Id != excludePriceListId.Value),
                cancellationToken);
    }



    public Task<PriceList?> GetEditableAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.PriceLists
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != PricingTaxConstants.DeletedStatus, cancellationToken);
    }

    public async Task<PriceListResponse?> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var priceList = await _dbContext.PriceLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != PricingTaxConstants.DeletedStatus, cancellationToken);

        if (priceList is null) return null;

        var outletIds = await _dbContext.PriceListOutlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PriceListId == id && x.Status == PricingTaxConstants.ActiveStatus)
            .Select(x => x.OutletId)
            .ToArrayAsync(cancellationToken);

        var channelIds = await _dbContext.PriceListChannels
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PriceListId == id && x.Status == PricingTaxConstants.ActiveStatus)
            .Select(x => x.SalesChannelId)
            .ToArrayAsync(cancellationToken);

        return new PriceListResponse(
            priceList.Id,
            priceList.PriceListCode,
            priceList.PriceListName,
            priceList.PriceListType,
            priceList.CurrencyCode,
            priceList.PriceIncludesTax,
            priceList.IsDefaultPriceList,
            priceList.Priority,
            priceList.ValidFrom,
            priceList.ValidUntil,
            priceList.Status,
            outletIds,
            channelIds,
            priceList.CreatedAt,
            priceList.UpdatedAt);
    }

    public async Task<PriceListListResponse> ListAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != PricingTaxConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.PriceListName, pattern) || EF.Functions.ILike(x.PriceListCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.PriceListName.ToUpper().Contains(normalizedTerm) || x.PriceListCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.PriceListName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PriceListSummaryResponse(
                x.Id,
                x.PriceListCode,
                x.PriceListName,
                x.PriceListType,
                x.CurrencyCode,
                x.PriceIncludesTax,
                x.IsDefaultPriceList,
                x.Priority,
                x.ValidFrom,
                x.ValidUntil,
                x.Status,
                x.CreatedAt,
                x.UpdatedAt))
            .ToArrayAsync(cancellationToken);

        return new PriceListListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task AddAsync(
        PriceList priceList,
        CancellationToken cancellationToken)
    {
        await _dbContext.PriceLists.AddAsync(priceList, cancellationToken);
    }

    public async Task AddOutletAssignmentsAsync(
        IEnumerable<PriceListOutlet> assignments,
        CancellationToken cancellationToken)
    {
        await _dbContext.PriceListOutlets.AddRangeAsync(assignments, cancellationToken);
    }

    public async Task AddChannelAssignmentsAsync(
        IEnumerable<PriceListChannel> assignments,
        CancellationToken cancellationToken)
    {
        await _dbContext.PriceListChannels.AddRangeAsync(assignments, cancellationToken);
    }

    public async Task ClearAssignmentsAsync(
        Guid priceListId,
        CancellationToken cancellationToken)
    {
        var outlets = await _dbContext.PriceListOutlets
            .Where(x => x.PriceListId == priceListId)
            .ToArrayAsync(cancellationToken);
        _dbContext.PriceListOutlets.RemoveRange(outlets);

        var channels = await _dbContext.PriceListChannels
            .Where(x => x.PriceListId == priceListId)
            .ToArrayAsync(cancellationToken);
        _dbContext.PriceListChannels.RemoveRange(channels);
    }

    public async Task ClearOtherDefaultsAsync(
        Guid tenantId,
        Guid defaultPriceListId,
        CancellationToken cancellationToken)
    {
        var otherDefaults = await _dbContext.PriceLists
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Id != defaultPriceListId && x.Status == PricingTaxConstants.ActiveStatus)
            .ToArrayAsync(cancellationToken);

        foreach (var priceList in otherDefaults)
        {
            priceList.ClearDefaultFlag();
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
