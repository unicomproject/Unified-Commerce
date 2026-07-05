using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PricingTax.Repositories;

public sealed class PriceListItemsRepository : IPriceListItemsRepository
{
    private readonly EPosDbContext _dbContext;

    public PriceListItemsRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ItemExistsAsync(
        Guid tenantId,
        Guid priceListId,
        Guid productId,
        Guid? variantId,
        Guid? uomId,
        decimal minQuantity,
        Guid? excludeItemId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.PriceListItems
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.PriceListId == priceListId &&
                     x.ProductId == productId &&
                     x.ProductVariantId == variantId &&
                     x.UomId == uomId &&
                     x.MinQuantity == minQuantity &&
                     x.Status != "DELETED" &&
                     (!excludeItemId.HasValue || x.Id != excludeItemId.Value),
                cancellationToken);

        if (exists) return true;

        var deletedItem = await _dbContext.PriceListItems
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.PriceListId == priceListId &&
                     x.ProductId == productId &&
                     x.ProductVariantId == variantId &&
                     x.UomId == uomId &&
                     x.MinQuantity == minQuantity &&
                     x.Status == "DELETED",
                cancellationToken);

        if (deletedItem != null)
        {
            _dbContext.PriceListItems.Remove(deletedItem);
        }

        return false;
    }

    public Task<PriceListItem?> GetEditableAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.PriceListItems
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED", cancellationToken);
    }

    public async Task<PriceListItemResponse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await _dbContext.PriceListItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED", cancellationToken);

        if (item is null) return null;

        return Map(item);
    }

    public async Task<PriceListItemListResponse> ListAsync(
        Guid tenantId,
        Guid priceListId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.PriceListItems
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PriceListId == priceListId && x.Status != "DELETED");

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.ProductVariantId)
            .ThenBy(x => x.MinQuantity)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        return new PriceListItemListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task AddAsync(PriceListItem priceListItem, CancellationToken cancellationToken)
    {
        await _dbContext.PriceListItems.AddAsync(priceListItem, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PriceListItemResponse Map(PriceListItem item)
    {
        return new PriceListItemResponse(
            item.Id,
            item.PriceListId,
            item.ProductId,
            item.ProductVariantId,
            item.UomId,
            item.SellingPrice,
            item.CompareAtPrice,
            item.MinQuantity,
            item.ValidFrom,
            item.ValidUntil,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt);
    }
}
