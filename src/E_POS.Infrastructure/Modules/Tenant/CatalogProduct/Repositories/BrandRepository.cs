using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
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
        var items = await query
            .OrderBy(x => x.BrandCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BrandSummaryResponse(x.Id, x.BrandCode, x.BrandName, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new BrandListResponse(items, pageNumber, pageSize, totalCount);
    }

    public Task<BrandResponse?> GetByIdAsync(Guid tenantId, Guid brandId, bool includeDeleted, CancellationToken cancellationToken)
    {
        return _dbContext.Brands
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == brandId && (includeDeleted || x.Status != BrandConstants.DeletedStatus))
            .Select(x => new BrandResponse(x.Id, x.BrandCode, x.BrandName, x.Status, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}


