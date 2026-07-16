using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.IntegrationTests.ECommerce.CartCheckout;

public sealed class StorefrontCartRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddItemAsync_CreatesCartMergesLineAndCalculatesServerTotals()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedProductAsync(dbContext, tenantId, productId, 10m, 3m);
        var repository = new StorefrontCartRepository(dbContext);
        var request = new AddStorefrontCartItemRequest { ProductId = productId, Quantity = 2m };

        var first = await repository.AddItemAsync(tenantId, "guest-1", request, Now, CancellationToken.None);
        request.Quantity = 1m;
        var second = await repository.AddItemAsync(tenantId, "guest-1", request, Now.AddMinutes(1), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var line = Assert.Single(second.Cart!.Items);
        Assert.Equal(3m, line.Quantity);
        Assert.Equal(30m, line.Subtotal);
        Assert.Equal(30m, second.Cart.Subtotal);
        Assert.Equal(30m, second.Cart.GrandTotal);
        Assert.Equal("LKR", second.Cart.CurrencyCode);
        Assert.Single(await dbContext.ShoppingCarts.ToListAsync());
        Assert.Single(await dbContext.ShoppingCartItems.ToListAsync());
    }

    [Fact]
    public async Task UpdateItemAsync_QuantityAboveAvailableStock_IsRejectedWithoutChangingLine()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedProductAsync(dbContext, tenantId, productId, 15m, 2m);
        var repository = new StorefrontCartRepository(dbContext);
        var added = await repository.AddItemAsync(
            tenantId, "guest-2",
            new AddStorefrontCartItemRequest { ProductId = productId, Quantity = 1m },
            Now, CancellationToken.None);
        var itemId = Assert.Single(added.Cart!.Items).Id;

        var result = await repository.UpdateItemAsync(
            tenantId, "guest-2", itemId, 3m, Now.AddMinutes(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_cart.insufficient_stock", result.ErrorCode);
        Assert.Equal(1m, (await dbContext.ShoppingCartItems.SingleAsync()).Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_OtherTenantCannotAccessCartItem()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedProductAsync(dbContext, tenantId, productId, 20m, 5m);
        dbContext.Tenants.Add(TenantEntity.Create(
            otherTenantId, "OTHER", "other", "Other", TenantStatusConstants.Active,
            "LKR", "Asia/Colombo", null, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontCartRepository(dbContext);
        var added = await repository.AddItemAsync(
            tenantId, "shared-session",
            new AddStorefrontCartItemRequest { ProductId = productId, Quantity = 1m },
            Now, CancellationToken.None);
        var itemId = Assert.Single(added.Cart!.Items).Id;

        var result = await repository.RemoveItemAsync(
            otherTenantId, "shared-session", itemId, Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_cart.item_not_found", result.ErrorCode);
        Assert.Equal("ACTIVE", (await dbContext.ShoppingCartItems.SingleAsync()).LineStatus);
    }

    [Fact]
    public async Task ClearAsync_RemovesActiveLinesAndResetsTotals()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedProductAsync(dbContext, tenantId, productId, 12.50m, 10m);
        var repository = new StorefrontCartRepository(dbContext);
        await repository.AddItemAsync(
            tenantId, "guest-3",
            new AddStorefrontCartItemRequest { ProductId = productId, Quantity = 2m },
            Now, CancellationToken.None);

        var result = await repository.ClearAsync(
            tenantId, "guest-3", Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Cart!.Items);
        Assert.Equal(0m, result.Cart.GrandTotal);
        Assert.Equal("REMOVED", (await dbContext.ShoppingCartItems.SingleAsync()).LineStatus);
    }

    private static async Task SeedProductAsync(
        EPosDbContext dbContext, Guid tenantId, Guid productId, decimal price, decimal stock)
    {
        dbContext.Tenants.Add(TenantEntity.Create(
            tenantId, $"T{tenantId:N}"[..12], $"tenant-{tenantId:N}", "Tenant",
            TenantStatusConstants.Active, "LKR", "Asia/Colombo", null, null, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, $"P{productId:N}", "Test Product", $"product-{productId:N}",
            "STANDARD", "SIMPLE", null, null, null, "Test", "Test product",
            true, true, "ACTIVE", null, Now));
        dbContext.PriceListItems.Add(PriceListItem.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null,
            price, null, 1m, null, null, "ACTIVE", null, Now));
        var balance = InventoryBalance.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null, Now);
        balance.AdjustQuantities(stock, 0m, 0m, 0m, Now);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
