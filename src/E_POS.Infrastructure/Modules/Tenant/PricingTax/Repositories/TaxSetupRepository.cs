using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
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

    public async Task<IReadOnlyDictionary<Guid, List<TaxRate>>> GetRatesForClassesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> taxClassIds,
        CancellationToken cancellationToken)
    {
        if (taxClassIds.Count == 0)
            return new Dictionary<Guid, List<TaxRate>>();

        var links = await _dbContext.TaxClassRates
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && taxClassIds.Contains(x.TaxClassId) && x.Status != "DELETED")
            .Select(x => new { x.TaxClassId, x.TaxRateId })
            .ToListAsync(cancellationToken);

        var rateIds = links.Select(x => x.TaxRateId).Distinct().ToList();
        var rates = await _dbContext.TaxRates
            .Where(x => x.TenantId == tenantId && rateIds.Contains(x.Id) && x.Status != "DELETED")
            .ToListAsync(cancellationToken);
        var rateById = rates.ToDictionary(x => x.Id);

        var result = taxClassIds.ToDictionary(id => id, _ => new List<TaxRate>());
        foreach (var link in links)
        {
            if (rateById.TryGetValue(link.TaxRateId, out var rate))
                result[link.TaxClassId].Add(rate);
        }

        return result;
    }

    public async Task<(IReadOnlyList<TaxClass> Items, int TotalCount)> GetTaxClassesFilteredAsync(
        Guid tenantId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.TaxClasses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != "DELETED");

        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status == normalized);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.TaxClassName.ToUpper().Contains(term) ||
                x.TaxClassCode.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.TaxClassName)
            .ThenBy(x => x.TaxClassCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> taxClassIds,
        CancellationToken cancellationToken)
    {
        if (taxClassIds.Count == 0)
            return new Dictionary<Guid, int>();

        var rows = await _dbContext.ProductTaxAssignments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                taxClassIds.Contains(x.TaxClassId) &&
                x.Status == "ACTIVE")
            .GroupBy(x => x.TaxClassId)
            .Select(g => new { TaxClassId = g.Key, Count = g.Select(x => x.ProductId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.TaxClassId, x => x.Count);
    }

    public async Task<(IReadOnlyList<TaxProductUsingResponse> Items, int TotalCount)> GetProductsUsingTaxAsync(
        Guid tenantId,
        Guid taxClassId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query =
            from assignment in _dbContext.ProductTaxAssignments.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on new { assignment.TenantId, Id = assignment.ProductId }
                equals new { product.TenantId, product.Id }
            where assignment.TenantId == tenantId &&
                  assignment.TaxClassId == taxClassId &&
                  assignment.Status == "ACTIVE"
            select new
            {
                product.Id,
                product.ProductName,
                product.ProductCode,
                product.Status,
                product.IsTaxExclusive
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.ProductName.ToUpper().Contains(term) ||
                x.ProductCode.ToUpper().Contains(term));
        }

        var distinct = query
            .GroupBy(x => x.Id)
            .Select(g => g.First());

        var totalCount = await distinct.CountAsync(cancellationToken);
        var pageItems = await distinct
            .OrderBy(x => x.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pageItems.Select(x => new TaxProductUsingResponse
        {
            ProductId = x.Id,
            ProductName = x.ProductName,
            ProductCode = x.ProductCode,
            Status = x.Status,
            TaxPriceMode = x.IsTaxExclusive ? "EXCLUSIVE" : "INCLUSIVE"
        }).ToList();

        return (items, totalCount);
    }

    public Task<bool> HasProductAssignmentsAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken)
    {
        return _dbContext.ProductTaxAssignments
            .AsNoTracking()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.TaxClassId == taxClassId &&
                x.Status == "ACTIVE",
                cancellationToken);
    }

    public async Task<bool> HasTransactionalUsageAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken)
    {
        var inSnapshots = await _dbContext.SalesOrderTaxes
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.TaxClassId == taxClassId, cancellationToken);
        if (inSnapshots)
            return true;

        return await HasProductAssignmentsAsync(tenantId, taxClassId, cancellationToken);
    }

    public Task<bool> RateReferencedByTransactionsAsync(Guid tenantId, Guid taxRateId, CancellationToken cancellationToken)
    {
        return _dbContext.SalesOrderTaxes
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.TaxRateId == taxRateId, cancellationToken);
    }

    public async Task<string?> GetTenantTimezoneAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.DefaultTimezone)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
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

    public async Task<TaxJurisdiction> ResolveDefaultJurisdictionAsync(Guid tenantId, Guid? userId, DateTimeOffset now)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        
        string countryCode = "US";
        if (tenant != null && !string.IsNullOrWhiteSpace(tenant.DefaultLocale))
        {
            var parts = tenant.DefaultLocale.Split('-');
            if (parts.Length > 1)
            {
                countryCode = parts.Last().ToUpperInvariant();
            }
        }
        
        var jurisdictionCode = $"DEFAULT-{countryCode}";

        var existing = await _dbContext.TaxJurisdictions
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.JurisdictionCode == jurisdictionCode);

        if (existing != null)
        {
            return existing;
        }

        var newJurisdiction = TaxJurisdiction.Create(
            tenantId, 
            jurisdictionCode, 
            "Default Tax Jurisdiction", 
            "COUNTRY", 
            countryCode, 
            null, 
            null, 
            null, 
            userId, 
            now);

        await _dbContext.TaxJurisdictions.AddAsync(newJurisdiction);
        return newJurisdiction;
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}



