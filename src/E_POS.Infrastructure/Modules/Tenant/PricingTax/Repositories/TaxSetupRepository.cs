using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Repositories;

public sealed class TaxSetupRepository : ITaxSetupRepository
{
    private readonly EPosDbContext _dbContext;

    public TaxSetupRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaxClass?> GetTaxClassByIdAsync(Guid tenantId, Guid taxClassId)
    {
        return await _dbContext.TaxClasses
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == taxClassId && x.Status != "DELETED");
    }

    public async Task<TaxClass?> GetTaxClassByCodeAsync(Guid tenantId, string taxClassCode)
    {
        var codeUpper = taxClassCode.Trim().ToUpperInvariant();
        return await _dbContext.TaxClasses
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TaxClassCode == codeUpper && x.Status != "DELETED");
    }

    public async Task<(IEnumerable<TaxClass> Items, int TotalCount)> GetTaxClassesAsync(Guid tenantId, int page, int pageSize)
    {
        var query = _dbContext.TaxClasses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != "DELETED");

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.TaxClassCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddTaxClassAsync(TaxClass taxClass)
    {
        await _dbContext.TaxClasses.AddAsync(taxClass);
    }

    public void UpdateTaxClass(TaxClass taxClass)
    {
        _dbContext.TaxClasses.Update(taxClass);
    }

    public async Task ClearDefaultTaxClassAsync(Guid tenantId, Guid? excludeTaxClassId)
    {
        var defaultClasses = await _dbContext.TaxClasses
            .Where(x => x.TenantId == tenantId && 
                        x.IsDefaultTaxClass && 
                        x.Status != "DELETED" && 
                        (!excludeTaxClassId.HasValue || x.Id != excludeTaxClassId.Value))
            .ToListAsync();

        foreach (var taxClass in defaultClasses)
        {
            taxClass.SetDefault(false, null);
            _dbContext.TaxClasses.Update(taxClass);
        }
    }

    public async Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid taxRateId)
    {
        return await _dbContext.TaxRates
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == taxRateId && x.Status != "DELETED");
    }

    public async Task<TaxRate?> GetTaxRateByCodeAsync(Guid tenantId, string taxRateCode)
    {
        var codeUpper = taxRateCode.Trim().ToUpperInvariant();
        return await _dbContext.TaxRates
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TaxRateCode == codeUpper && x.Status != "DELETED");
    }

    public async Task<(IEnumerable<TaxRate> Items, int TotalCount)> GetTaxRatesAsync(Guid tenantId, int page, int pageSize)
    {
        var query = _dbContext.TaxRates
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != "DELETED");

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.TaxRateCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddTaxRateAsync(TaxRate taxRate)
    {
        await _dbContext.TaxRates.AddAsync(taxRate);
    }

    public void UpdateTaxRate(TaxRate taxRate)
    {
        _dbContext.TaxRates.Update(taxRate);
    }

    public async Task<List<TaxClassRate>> GetTaxClassRatesAsync(Guid tenantId, Guid taxClassId)
    {
        return await _dbContext.TaxClassRates
            .Where(x => x.TenantId == tenantId && x.TaxClassId == taxClassId && x.Status != "DELETED")
            .ToListAsync();
    }

    public async Task<List<TaxRate>> GetRatesForClassAsync(Guid tenantId, Guid taxClassId)
    {
        var rateIds = await _dbContext.TaxClassRates
            .Where(x => x.TenantId == tenantId && x.TaxClassId == taxClassId && x.Status != "DELETED")
            .Select(x => x.TaxRateId)
            .ToListAsync();

        return await _dbContext.TaxRates
            .Where(x => x.TenantId == tenantId && rateIds.Contains(x.Id) && x.Status != "DELETED")
            .ToListAsync();
    }

    public async Task AddTaxClassRatesAsync(IEnumerable<TaxClassRate> taxClassRates)
    {
        await _dbContext.TaxClassRates.AddRangeAsync(taxClassRates);
    }

    public void RemoveTaxClassRates(IEnumerable<TaxClassRate> taxClassRates)
    {
        _dbContext.TaxClassRates.RemoveRange(taxClassRates);
    }

    public async Task<bool> JurisdictionExistsAsync(Guid tenantId, Guid jurisdictionId)
    {
        return await _dbContext.TaxJurisdictions
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == jurisdictionId && x.Status != "DELETED");
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}



