using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class ReturnPolicyRepository : IReturnPolicyRepository
{
    private readonly EPosDbContext _dbContext;

    public ReturnPolicyRepository(EPosDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> PolicyCodeExistsAsync(Guid tenantId, string policyCode, Guid? excludePolicyId, CancellationToken cancellationToken)
    {
        return _dbContext.ReturnPolicies.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.ReturnPolicyCode == policyCode && (!excludePolicyId.HasValue || x.Id != excludePolicyId.Value), cancellationToken);
    }

    public async Task<ReturnPolicyListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.ReturnPolicies.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status != ReturnPolicyConstants.DeletedStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.ReturnPolicyName, pattern) || EF.Functions.ILike(x.ReturnPolicyCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.ReturnPolicyName.ToUpper().Contains(normalizedTerm) || x.ReturnPolicyCode.ToUpper().Contains(normalizedTerm));
            }
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.ReturnPolicyCode).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new ReturnPolicySummaryResponse(x.Id, x.ReturnPolicyCode, x.ReturnPolicyName, x.ReturnWindowDays, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new ReturnPolicyListResponse(items, pageNumber, pageSize, totalCount);
    }

    public Task<ReturnPolicyResponse?> GetByIdAsync(Guid tenantId, Guid policyId, bool includeDeleted, CancellationToken cancellationToken)
    {
        return _dbContext.ReturnPolicies.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == policyId && (includeDeleted || x.Status != ReturnPolicyConstants.DeletedStatus))
            .Select(x => new ReturnPolicyResponse(x.Id, x.ReturnPolicyCode, x.ReturnPolicyName, x.ReturnWindowDays, x.Status, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ReturnPolicy?> GetEditableAsync(Guid tenantId, Guid policyId, CancellationToken cancellationToken)
    {
        return _dbContext.ReturnPolicies.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == policyId && x.Status != ReturnPolicyConstants.DeletedStatus, cancellationToken);
    }

    public async Task AddAsync(ReturnPolicy policy, CancellationToken cancellationToken)
    {
        _dbContext.ReturnPolicies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}


