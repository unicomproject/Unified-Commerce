using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Infrastructure.Modules.PricingTax.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PricingTax;

public sealed class TaxSetupRepositoryTests
{
    [Fact]
    public async Task AddTaxClass_And_RetrieveByCode_Works()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        var repository = new TaxSetupRepository(dbContext);

        var taxClass = TaxClass.Create(tenantId, "TC-01", "VAT 20%", "Standard rate", true, null, DateTimeOffset.UtcNow);
        await repository.AddTaxClassAsync(taxClass);
        await repository.SaveChangesAsync();

        var retrieved = await repository.GetTaxClassByCodeAsync(tenantId, "tc-01");

        Assert.NotNull(retrieved);
        Assert.Equal("TC-01", retrieved.TaxClassCode);
        Assert.True(retrieved.IsDefaultTaxClass);
    }

    [Fact]
    public async Task ClearDefaultTaxClass_RemovesDefaultStatus_FromOthers()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        var repository = new TaxSetupRepository(dbContext);

        var taxClass1 = TaxClass.Create(tenantId, "TC-01", "Rate 1", null, true, null, DateTimeOffset.UtcNow);
        var taxClass2 = TaxClass.Create(tenantId, "TC-02", "Rate 2", null, true, null, DateTimeOffset.UtcNow); // Just for testing
        
        await repository.AddTaxClassAsync(taxClass1);
        await repository.AddTaxClassAsync(taxClass2);
        await repository.SaveChangesAsync();

        // Act
        await repository.ClearDefaultTaxClassAsync(tenantId, taxClass2.Id);
        await repository.SaveChangesAsync();

        // Assert
        var retrieved1 = await repository.GetTaxClassByIdAsync(tenantId, taxClass1.Id);
        var retrieved2 = await repository.GetTaxClassByIdAsync(tenantId, taxClass2.Id);

        Assert.NotNull(retrieved1);
        Assert.False(retrieved1.IsDefaultTaxClass); // Should be cleared
        Assert.NotNull(retrieved2);
        Assert.True(retrieved2.IsDefaultTaxClass); // Should remain default
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new EPosDbContext(options);
    }
}
