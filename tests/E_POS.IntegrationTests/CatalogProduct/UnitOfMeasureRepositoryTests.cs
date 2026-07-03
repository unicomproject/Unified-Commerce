using System.Reflection;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class UnitOfMeasureRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListAsync_ReturnsGlobalAndCurrentTenantUnitsOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.UnitOfMeasures.AddRange(
            CreateUnit(null, "PCS", "Pieces"),
            CreateUnit(tenantId, "BOTTLE", "Bottle"),
            CreateUnit(otherTenantId, "CASE", "Case"));
        await dbContext.SaveChangesAsync();
        var repository = new UnitOfMeasureRepository(dbContext);

        var result = await repository.ListAsync(tenantId, CancellationToken.None);

        Assert.Equal(["PCS", "BOTTLE"], result.Select(x => x.UomCode).ToArray());
        Assert.DoesNotContain(result, x => x.UomCode == "CASE");
        Assert.True(result.Single(x => x.UomCode == "PCS").IsGlobal);
        Assert.False(result.Single(x => x.UomCode == "BOTTLE").IsGlobal);
    }

    private static UnitOfMeasure CreateUnit(Guid? tenantId, string code, string name)
    {
        var unit = (UnitOfMeasure)Activator.CreateInstance(typeof(UnitOfMeasure), nonPublic: true)!;
        Set(unit, "Id", Guid.NewGuid());
        Set(unit, "TenantId", tenantId);
        Set(unit, "UomCode", code);
        Set(unit, "Name", name);
        Set<decimal?>(unit, "ConversionFactor", null);
        Set(unit, "CreatedAt", Now);
        Set<DateTimeOffset?>(unit, "UpdatedAt", Now);
        return unit;
    }

    private static void Set<T>(UnitOfMeasure unit, string propertyName, T value)
    {
        typeof(UnitOfMeasure)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(unit, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}