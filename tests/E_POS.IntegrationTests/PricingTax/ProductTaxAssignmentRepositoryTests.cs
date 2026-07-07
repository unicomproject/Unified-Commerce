using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Modules.Tenant.PricingTax.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PricingTax;

public sealed class ProductTaxAssignmentRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HasOverlappingAssignment_ReturnsTrue_WhenDatesOverlap()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxClassId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        var repository = new ProductTaxAssignmentRepository(dbContext);

        // First assignment valid from Jan 1 to Jan 31
        var assignment1 = ProductTaxAssignment.Create(
            tenantId, productId, null, taxClassId,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero),
            null, Now);

        await repository.AddAsync(assignment1);
        await repository.SaveChangesAsync();

        // Check if an assignment from Jan 15 to Feb 15 overlaps
        var hasOverlap = await repository.HasOverlappingAssignmentAsync(
            tenantId, productId, null, taxClassId,
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero));

        Assert.True(hasOverlap);
    }

    [Fact]
    public async Task HasOverlappingAssignment_ReturnsFalse_WhenDatesDoNotOverlap()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxClassId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        var repository = new ProductTaxAssignmentRepository(dbContext);

        // First assignment valid from Jan 1 to Jan 31
        var assignment1 = ProductTaxAssignment.Create(
            tenantId, productId, null, taxClassId,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero),
            null, Now);

        await repository.AddAsync(assignment1);
        await repository.SaveChangesAsync();

        // Check if an assignment from Feb 1 to Feb 28 overlaps
        var hasOverlap = await repository.HasOverlappingAssignmentAsync(
            tenantId, productId, null, taxClassId,
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero));

        Assert.False(hasOverlap);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new EPosDbContext(options);
    }
}


