using E_POS.Domain.Modules.PricingTax.Entities;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface ITaxSetupRepository
{
    // Tax Class
    Task<TaxClass?> GetTaxClassByIdAsync(Guid tenantId, Guid taxClassId);
    Task<TaxClass?> GetTaxClassByCodeAsync(Guid tenantId, string taxClassCode);
    Task<(IEnumerable<TaxClass> Items, int TotalCount)> GetTaxClassesAsync(Guid tenantId, int page, int pageSize);
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
    Task AddTaxClassRatesAsync(IEnumerable<TaxClassRate> taxClassRates);
    void RemoveTaxClassRates(IEnumerable<TaxClassRate> taxClassRates);
    
    // Tax Jurisdiction (for existence checks)
    Task<bool> JurisdictionExistsAsync(Guid tenantId, Guid jurisdictionId);

    Task SaveChangesAsync();
}
