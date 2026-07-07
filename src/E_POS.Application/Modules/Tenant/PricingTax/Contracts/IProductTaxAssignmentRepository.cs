using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Contracts;

public interface IProductTaxAssignmentRepository
{
    Task<ProductTaxAssignment?> GetByIdAsync(Guid tenantId, Guid id);
    Task<(IEnumerable<ProductTaxAssignment> Items, int TotalCount)> GetByProductAsync(Guid tenantId, Guid productId, int page, int pageSize);
    Task AddAsync(ProductTaxAssignment assignment);
    void Update(ProductTaxAssignment assignment);
    Task<bool> HasOverlappingAssignmentAsync(Guid tenantId, Guid productId, Guid? productVariantId, Guid taxClassId, DateTimeOffset? appliesFrom, DateTimeOffset? appliesUntil, Guid? excludeAssignmentId = null);
    Task SaveChangesAsync();
}


