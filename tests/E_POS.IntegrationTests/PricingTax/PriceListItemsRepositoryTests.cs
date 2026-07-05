using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Infrastructure.Modules.PricingTax.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PricingTax;

public sealed class PriceListItemsRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ItemExistsAsync_IdentifiesDuplicate_AndRemovesDeletedRow()
    {
        var tenantId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();

        var item = PriceListItem.Create(
            Guid.NewGuid(), tenantId, priceListId, productId, null, null, 10m, null, 1m, null, null, "DELETED", null, Now);
        dbContext.PriceListItems.Add(item);
        await dbContext.SaveChangesAsync();

        var repository = new PriceListItemsRepository(dbContext);

        // Check if item exists. Since the row is DELETED, it should return false BUT physically delete it.
        var exists = await repository.ItemExistsAsync(tenantId, priceListId, productId, null, null, 1m, null, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.False(exists);
        var searchItem = await dbContext.PriceListItems.FindAsync(item.Id);
        Assert.Null(searchItem); // Deleted row should have been permanently deleted!
    }

    [Fact]
    public async Task AddAsync_PersistsPriceListItem()
    {
        var tenantId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        var repository = new PriceListItemsRepository(dbContext);

        var item = PriceListItem.Create(
            itemId, tenantId, priceListId, productId, null, null, 15m, 20m, 1m, null, null, "ACTIVE", null, Now);

        await repository.AddAsync(item, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var response = await repository.GetByIdAsync(tenantId, itemId, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(15m, response.SellingPrice);
        Assert.Equal(20m, response.CompareAtPrice);
        Assert.Equal("ACTIVE", response.Status);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
