using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly EPosDbContext _dbContext;

    public DepartmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> DepartmentCodeExistsAsync(Guid tenantId, string departmentCode, Guid? excludeDepartmentId, CancellationToken cancellationToken)
    {
        return _dbContext.Departments
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.DepartmentCode == departmentCode &&
                     (!excludeDepartmentId.HasValue || x.Id != excludeDepartmentId.Value),
                cancellationToken);
    }

    public async Task<DepartmentListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Departments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != DepartmentConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.DepartmentName, pattern) || EF.Functions.ILike(x.DepartmentCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.DepartmentName.ToUpper().Contains(normalizedTerm) || x.DepartmentCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.DepartmentCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DepartmentSummaryResponse(x.Id, x.DepartmentCode, x.DepartmentName, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new DepartmentListResponse(items, pageNumber, pageSize, totalCount);
    }

    public Task<DepartmentResponse?> GetByIdAsync(Guid tenantId, Guid departmentId, bool includeDeleted, CancellationToken cancellationToken)
    {
        return _dbContext.Departments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == departmentId && (includeDeleted || x.Status != DepartmentConstants.DeletedStatus))
            .Select(x => new DepartmentResponse(x.Id, x.DepartmentCode, x.DepartmentName, x.Status, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Department?> GetEditableAsync(Guid tenantId, Guid departmentId, CancellationToken cancellationToken)
    {
        return _dbContext.Departments
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == departmentId && x.Status != DepartmentConstants.DeletedStatus, cancellationToken);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken)
    {
        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}