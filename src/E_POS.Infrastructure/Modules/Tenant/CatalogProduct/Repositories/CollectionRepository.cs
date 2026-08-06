using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class CollectionRepository : ICollectionRepository
{
    private readonly EPosDbContext _dbContext;

    public CollectionRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CollectionCodeExistsAsync(Guid tenantId, string collectionCode, Guid? excludeCollectionId, CancellationToken cancellationToken)
    {
        return _dbContext.Collections
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.CollectionCode == collectionCode &&
                     (!excludeCollectionId.HasValue || x.Id != excludeCollectionId.Value),
                cancellationToken);
    }

    public Task<bool> HasProductLinksAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken)
    {
        return (from productCollection in _dbContext.ProductCollections.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking() on productCollection.ProductId equals product.Id
                where product.TenantId == tenantId &&
                      productCollection.CollectionId == collectionId &&
                      product.Status != "DELETED"
                select productCollection.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<CollectionListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != CollectionConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.CollectionName, pattern) || EF.Functions.ILike(x.CollectionCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.CollectionName.ToUpper().Contains(normalizedTerm) || x.CollectionCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.CollectionCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CollectionSummaryResponse(x.Id, x.CollectionCode, x.CollectionName, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new CollectionListResponse(items, pageNumber, pageSize, totalCount);
    }

    public Task<CollectionResponse?> GetByIdAsync(Guid tenantId, Guid collectionId, bool includeDeleted, CancellationToken cancellationToken)
    {
        return _dbContext.Collections
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == collectionId && (includeDeleted || x.Status != CollectionConstants.DeletedStatus))
            .Select(x => new CollectionResponse(x.Id, x.CollectionCode, x.CollectionName, x.Status, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Collection?> GetEditableAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken)
    {
        return _dbContext.Collections
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == collectionId && x.Status != CollectionConstants.DeletedStatus, cancellationToken);
    }

    public async Task AddAsync(Collection collection, CancellationToken cancellationToken)
    {
        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Collection?> GetByCodeAsync(Guid tenantId, string collectionCode, CancellationToken cancellationToken)
    {
        return _dbContext.Collections
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CollectionCode == collectionCode && x.Status != CollectionConstants.DeletedStatus, cancellationToken);
    }

    public async Task<IReadOnlyList<CollectionProductResponseDto>> GetCollectionProductsAsync(Guid tenantId, Guid collectionId, CancellationToken cancellationToken)
    {
        var query = from pc in _dbContext.ProductCollections.AsNoTracking()
                    join p in _dbContext.Products.AsNoTracking() on pc.ProductId equals p.Id
                    where pc.TenantId == tenantId && pc.CollectionId == collectionId && p.Status != "DELETED"
                    orderby pc.SortOrder
                    select new
                    {
                        p.Id,
                        p.ProductName,
                        p.Status,
                        pc.SortOrder,
                        Sku = _dbContext.ProductVariants
                            .Where(v => v.ProductId == p.Id && v.Status != "DELETED")
                            .OrderByDescending(v => v.IsDefaultVariant)
                            .Select(v => v.Sku)
                            .FirstOrDefault()
                    };

        var items = await query.ToListAsync(cancellationToken);

        return items.Select(x => new CollectionProductResponseDto(
            x.Id,
            x.ProductName,
            x.Sku,
            x.Status,
            x.SortOrder
        )).ToList();
    }

    public async Task ReplaceCollectionProductsAsync(Guid tenantId, Guid collectionId, List<Guid> productIds, Guid? userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await _dbContext.ProductCollections
                .Where(x => x.TenantId == tenantId && x.CollectionId == collectionId)
                .ToListAsync(cancellationToken);

            _dbContext.ProductCollections.RemoveRange(existing);

            for (int i = 0; i < productIds.Count; i++)
            {
                var pc = ProductCollection.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productIds[i],
                    collectionId,
                    i,
                    userId,
                    now);
                _dbContext.ProductCollections.Add(pc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> AllProductsExistAndNotDeletedAsync(Guid tenantId, List<Guid> productIds, CancellationToken cancellationToken)
    {
        var count = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id) && x.Status != "DELETED")
            .CountAsync(cancellationToken);

        return count == productIds.Distinct().Count();
    }
}


