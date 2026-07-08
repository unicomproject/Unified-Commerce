using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Repositories;

public sealed class ProductTaxAssignmentRepository : IProductTaxAssignmentRepository
{
    private readonly EPosDbContext _dbContext;

    public ProductTaxAssignmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductTaxAssignment?> GetByIdAsync(Guid tenantId, Guid id)
    {
        return await _dbContext.ProductTaxAssignments
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != "DELETED");
    }

    public async Task<(IEnumerable<ProductTaxAssignment> Items, int TotalCount)> GetByProductAsync(Guid tenantId, Guid productId, int page, int pageSize)
    {
        var query = _dbContext.ProductTaxAssignments
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status != "DELETED");

        var totalCount = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(ProductTaxAssignment assignment)
    {
        await _dbContext.ProductTaxAssignments.AddAsync(assignment);
    }

    public void Update(ProductTaxAssignment assignment)
    {
        _dbContext.ProductTaxAssignments.Update(assignment);
    }

    public async Task<bool> HasOverlappingAssignmentAsync(Guid tenantId, Guid productId, Guid? productVariantId, Guid taxClassId, DateTimeOffset? appliesFrom, DateTimeOffset? appliesUntil, Guid? excludeAssignmentId = null)
    {
        var query = _dbContext.ProductTaxAssignments
            .Where(x => x.TenantId == tenantId 
                        && x.ProductId == productId 
                        && x.ProductVariantId == productVariantId 
                        && x.Status == "ACTIVE");

        if (excludeAssignmentId.HasValue)
        {
            query = query.Where(x => x.Id != excludeAssignmentId.Value);
        }

        var activeAssignments = await query.ToListAsync();

        foreach (var existing in activeAssignments)
        {
            // Check for overlap
            // Logic: Two ranges overlap if (StartA <= EndB) and (EndA >= StartB)
            // If appliesFrom/appliesUntil is null, it means negative/positive infinity.
            
            var existingStart = existing.AppliesFrom ?? DateTimeOffset.MinValue;
            var existingEnd = existing.AppliesUntil ?? DateTimeOffset.MaxValue;
            var newStart = appliesFrom ?? DateTimeOffset.MinValue;
            var newEnd = appliesUntil ?? DateTimeOffset.MaxValue;

            if (existingStart < newEnd && newStart < existingEnd)
            {
                return true; // Overlap found
            }
        }

        return false;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}



