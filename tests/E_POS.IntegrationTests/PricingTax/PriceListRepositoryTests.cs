using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.PricingTax.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PricingTax;

public sealed class PriceListRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PriceListCodeExistsAsync_ReturnsTrue_WhenCodeExists()
    {
        var tenantId = Guid.NewGuid();
        var code = "PL-ABC";
        await using var dbContext = CreateDbContext();
        var priceList = PriceList.Create(
            Guid.NewGuid(), tenantId, code, "Test List", "POS", "USD", false, false, 0, null, null, "ACTIVE", null, Now);
        
        dbContext.PriceLists.Add(priceList);
        await dbContext.SaveChangesAsync();
        var repository = new PriceListRepository(dbContext);

        var exists = await repository.PriceListCodeExistsAsync(tenantId, "PL-ABC", null, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ClearOtherDefaultsAsync_ClearsDefaultFlagsForOtherLists()
    {
        var tenantId = Guid.NewGuid();
        var currency = new Currency();
        Set(currency, "CurrencyCode", "USD");
        Set(currency, "Name", "US Dollar");
        Set(currency, "DecimalPlaces", 2);
        Set(currency, "CreatedAt", Now);
        Set(currency, "UpdatedAt", Now);

        await using var dbContext = CreateDbContext();
        dbContext.Currencies.Add(currency);

        var priceList1 = PriceList.Create(
            Guid.NewGuid(), tenantId, "PL-1", "Default List", "POS", "USD", false, true, 0, null, null, "ACTIVE", null, Now);
        var priceList2 = PriceList.Create(
            Guid.NewGuid(), tenantId, "PL-2", "Promotional List", "POS", "USD", false, true, 0, null, null, "ACTIVE", null, Now);

        dbContext.PriceLists.AddRange(priceList1, priceList2);
        await dbContext.SaveChangesAsync();
        var repository = new PriceListRepository(dbContext);

        await repository.ClearOtherDefaultsAsync(tenantId, priceList2.Id, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var updatedList1 = await dbContext.PriceLists.FindAsync(priceList1.Id);
        var updatedList2 = await dbContext.PriceLists.FindAsync(priceList2.Id);

        Assert.False(updatedList1!.IsDefaultPriceList);
        Assert.True(updatedList2!.IsDefaultPriceList);
    }

    [Fact]
    public async Task GetByIdAsync_RetrievesCorrectAssignments()
    {
        var tenantId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        var priceList = PriceList.Create(
            priceListId, tenantId, "PL-1", "Standard List", "POS", "USD", false, true, 0, null, null, "ACTIVE", null, Now);
        var outletAssignment = PriceListOutlet.Create(
            Guid.NewGuid(), tenantId, priceListId, outletId, "ACTIVE", null, Now);
        var channelAssignment = PriceListChannel.Create(
            Guid.NewGuid(), tenantId, priceListId, channelId, "ACTIVE", null, Now);

        await using var dbContext = CreateDbContext();
        dbContext.PriceLists.Add(priceList);
        dbContext.PriceListOutlets.Add(outletAssignment);
        dbContext.PriceListChannels.Add(channelAssignment);
        await dbContext.SaveChangesAsync();
        var repository = new PriceListRepository(dbContext);

        var response = await repository.GetByIdAsync(tenantId, priceListId, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("PL-1", response.PriceListCode);
        var singleOutlet = Assert.Single(response.AssignedOutletIds);
        Assert.Equal(outletId, singleOutlet);
        var singleChannel = Assert.Single(response.AssignedSalesChannelIds);
        Assert.Equal(channelId, singleChannel);
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var prop = entity.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        prop?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}


