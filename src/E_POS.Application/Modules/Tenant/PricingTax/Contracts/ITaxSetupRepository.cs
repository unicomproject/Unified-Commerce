using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Contracts;

public interface ITaxSetupRepository
{
    // Tax Class
    Task<TaxClass?> GetTaxClassByIdAsync(Guid tenantId, Guid taxClassId);
    Task<TaxClass?> GetTaxClassByCodeAsync(Guid tenantId, string taxClassCode);
    Task<(IEnumerable<TaxClass> Items, int TotalCount)> GetTaxClassesAsync(Guid tenantId, int page, int pageSize);
    Task<(IReadOnlyList<TaxClass> Items, int TotalCount)> GetTaxClassesFilteredAsync(
        Guid tenantId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task AddTaxClassAsync(TaxClass taxClass);
    void UpdateTaxClass(TaxClass taxClass);
    Task ClearDefaultTaxClassAsync(Guid tenantId, Guid? excludeTaxClassId);

    // Tax Rate
    Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid taxRateId);
    Task<TaxRate?> GetTaxRateByCodeAsync(Guid tenantId, string taxRateCode);
    Task<(IEnumerable<TaxRate> Items, int TotalCount)> GetTaxRatesAsync(Guid tenantId, int page, int pageSize);
    Task AddTaxRateAsync(TaxRate taxRate);
    void UpdateTaxRate(TaxRate taxRate);

    // Tax Class Rate Assignment
    Task<List<TaxClassRate>> GetTaxClassRatesAsync(Guid tenantId, Guid taxClassId);
    Task<List<TaxRate>> GetRatesForClassAsync(Guid tenantId, Guid taxClassId);
    Task<IReadOnlyDictionary<Guid, List<TaxRate>>> GetRatesForClassesAsync(Guid tenantId, IReadOnlyCollection<Guid> taxClassIds, CancellationToken cancellationToken);
    Task AddTaxClassRatesAsync(IEnumerable<TaxClassRate> taxClassRates);
    void RemoveTaxClassRates(IEnumerable<TaxClassRate> taxClassRates);

    // Products using
    Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(Guid tenantId, IReadOnlyCollection<Guid> taxClassIds, CancellationToken cancellationToken);
    Task<(IReadOnlyList<TaxProductUsingResponse> Items, int TotalCount)> GetProductsUsingTaxAsync(
        Guid tenantId,
        Guid taxClassId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<bool> HasProductAssignmentsAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken);
    Task<bool> HasTransactionalUsageAsync(Guid tenantId, Guid taxClassId, CancellationToken cancellationToken);
    Task<bool> RateReferencedByTransactionsAsync(Guid tenantId, Guid taxRateId, CancellationToken cancellationToken);

    // Tax Jurisdiction
    Task<bool> JurisdictionExistsAsync(Guid tenantId, Guid jurisdictionId);
    Task<TaxJurisdiction> ResolveDefaultJurisdictionAsync(Guid tenantId, Guid? userId, DateTimeOffset now);
    Task<string?> GetTenantTimezoneAsync(Guid tenantId, CancellationToken cancellationToken);

    Task SaveChangesAsync();
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
