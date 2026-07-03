using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Repositories;

public sealed class UnitOfMeasureRepository : IUnitOfMeasureRepository
{
    private readonly EPosDbContext _dbContext;

    public UnitOfMeasureRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UnitOfMeasureResponse>> ListAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x => x.TenantId == null || x.TenantId == tenantId)
            .OrderBy(x => x.TenantId == null ? 0 : 1)
            .ThenBy(x => x.UomCode)
            .Select(x => new UnitOfMeasureResponse(
                x.Id,
                x.TenantId,
                x.UomCode,
                x.Name,
                x.ConversionFactor,
                x.TenantId == null,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}