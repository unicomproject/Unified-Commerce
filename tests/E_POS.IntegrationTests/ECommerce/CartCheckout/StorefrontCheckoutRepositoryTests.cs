using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.IntegrationTests.ECommerce.CartCheckout;

public sealed class StorefrontCheckoutRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateFromCartAsync_CreatesTenantScopedSnapshotAndReservesOutletStock()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 2m);
        var repository = new StorefrontCheckoutRepository(dbContext);

        var result = await repository.CreateFromCartAsync(
            scenario.TenantId,
            scenario.CustomerId,
            scenario.CartSessionId,
            new CreateStorefrontCheckoutFromCartRequest
            {
                SelectedOutletId = scenario.OutletId,
                PickupContactName = "Test Customer"
            },
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PENDING", result.Checkout!.Status);
        Assert.Equal(2m, result.Checkout.TotalQuantity);
        Assert.Equal(50m, result.Checkout.GrandTotal);
        Assert.Equal(2m, (await dbContext.InventoryBalances
            .SingleAsync(x => x.Id == scenario.SelectedBalanceId)).ReservedQuantity);
        Assert.Single(await dbContext.InventoryReservations.ToListAsync());
        Assert.Single(await dbContext.CheckoutSessionLines.ToListAsync());
        Assert.Single(await dbContext.CheckoutEvents.ToListAsync());
    }

    [Fact]
    public async Task GetAsync_OtherTenantCannotReadCheckoutSession()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 1m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var result = await repository.GetAsync(
            Guid.NewGuid(), scenario.CustomerId, created.Checkout!.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.session_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmAsync_CreatesOneClickAndCollectOrderAndIsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 2m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var first = await repository.ConfirmAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            "confirm-1",
            Now.AddMinutes(1),
            CancellationToken.None);
        var retry = await repository.ConfirmAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout.Id,
            "confirm-1",
            Now.AddMinutes(2),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(retry.IsSuccess);
        Assert.Equal("COMPLETED", retry.Checkout!.Status);
        Assert.NotNull(retry.Checkout.Order);
        Assert.Equal("CONFIRMED", retry.Checkout.Order!.Status);
        Assert.Equal("UNPAID", retry.Checkout.Order.PaymentStatus);
        Assert.Single(await dbContext.SalesOrders.ToListAsync());
        Assert.Single(await dbContext.SalesOrderLines.ToListAsync());
        Assert.Equal("CONVERTED", (await dbContext.ShoppingCarts.SingleAsync()).CartStatus);
        Assert.Equal("CONFIRMED", (await dbContext.InventoryReservations.SingleAsync()).ReservationStatus);
    }

    [Fact]
    public async Task CreateFromCartAsync_SelectedOutletInsufficientStock_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(
            dbContext, selectedOutletStock: 1m, otherOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 2m);
        var repository = new StorefrontCheckoutRepository(dbContext);

        var result = await CreateCheckoutAsync(repository, scenario);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.insufficient_stock", result.ErrorCode);
        Assert.Empty(await dbContext.CheckoutSessions.ToListAsync());
    }

    [Fact]
    public async Task ConfirmAsync_ExpiredSession_ReleasesReservedInventory()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 2m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var result = await repository.ConfirmAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            "expired-confirm",
            Now.AddMinutes(16),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.session_expired", result.ErrorCode);
        Assert.Equal(0m, (await dbContext.InventoryBalances
            .SingleAsync(x => x.Id == scenario.SelectedBalanceId)).ReservedQuantity);
        Assert.Equal("EXPIRED", (await dbContext.CheckoutSessions.SingleAsync()).CheckoutStatus);
        Assert.Equal("EXPIRED", (await dbContext.InventoryReservations.SingleAsync()).ReservationStatus);
    }

    private static Task<E_POS.Application.Modules.ECommerce.CartCheckout.Contracts.StorefrontCheckoutRepositoryResult>
        CreateCheckoutAsync(StorefrontCheckoutRepository repository, Scenario scenario) =>
        repository.CreateFromCartAsync(
            scenario.TenantId,
            scenario.CustomerId,
            scenario.CartSessionId,
            new CreateStorefrontCheckoutFromCartRequest { SelectedOutletId = scenario.OutletId },
            Now,
            CancellationToken.None);

    private static async Task AddCartItemAsync(
        EPosDbContext dbContext,
        Scenario scenario,
        decimal quantity)
    {
        var cartRepository = new StorefrontCartRepository(dbContext);
        var result = await cartRepository.AddItemAsync(
            scenario.TenantId,
            scenario.CartSessionId,
            new AddStorefrontCartItemRequest
            {
                ProductId = scenario.ProductId,
                Quantity = quantity
            },
            Now,
            CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    private static async Task<Scenario> SeedScenarioAsync(
        EPosDbContext dbContext,
        decimal selectedOutletStock,
        decimal otherOutletStock = 0m)
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var onlineSalesChannelId = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var selectedLocationId = Guid.NewGuid();
        var selectedBalanceId = Guid.NewGuid();

        dbContext.Tenants.Add(TenantEntity.Create(
            tenantId,
            $"T{tenantId:N}"[..12],
            $"tenant-{tenantId:N}",
            "Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
        dbContext.PlatformSalesChannels.Add(PlatformSalesChannel.Create(
            PlatformSalesChannelSeedConstants.OnlineChannelId,
            "ONLINE",
            "E-Commerce",
            "ONLINE",
            Now));
        dbContext.SalesChannels.Add(SalesChannel.Create(
            onlineSalesChannelId,
            tenantId,
            PlatformSalesChannelSeedConstants.OnlineChannelId,
            "E-Commerce Storefront",
            "ACTIVE",
            1,
            Now));
        dbContext.Customers.Add(CustomerEntity.CreatePosCustomer(
            customerId,
            tenantId,
            $"C{customerId:N}"[..12],
            "Test Customer",
            "+94770000000",
            "customer@example.com",
            creatorId,
            Now));
        dbContext.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Store", "MAIN", "ACTIVE", "STORE",
            "Asia/Colombo", true, null, null, null, Now));
        dbContext.InventoryLocations.Add(InventoryLocation.Create(
            selectedLocationId, tenantId, outletId, null, "MAIN-SELL", "Main Sellable",
            "SELLABLE", true, false, false, false, "ACTIVE", null, Now));
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            uomId, null, "PCS", "Pieces", "COUNT", "pc", null, 1m, "ACTIVE", Now));
        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            $"P{productId:N}",
            "Test Product",
            $"product-{productId:N}",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            "Test",
            "Test product",
            true,
            false,
            "ACTIVE",
            null,
            Now));
        dbContext.PriceListItems.Add(PriceListItem.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null,
            25m, null, 1m, null, null, "ACTIVE", null, Now));
        var selectedBalance = InventoryBalance.Create(
            selectedBalanceId, tenantId, selectedLocationId, productId, null, null, Now);
        selectedBalance.AdjustQuantities(selectedOutletStock, 0m, 0m, 0m, Now);
        dbContext.InventoryBalances.Add(selectedBalance);

        if (otherOutletStock > 0m)
        {
            var otherOutletId = Guid.NewGuid();
            var otherLocationId = Guid.NewGuid();
            dbContext.Outlets.Add(Outlet.Create(
                otherOutletId, tenantId, "Other Store", "OTHER", "ACTIVE", "STORE",
                "Asia/Colombo", false, null, null, null, Now));
            dbContext.InventoryLocations.Add(InventoryLocation.Create(
                otherLocationId, tenantId, otherOutletId, null, "OTHER-SELL", "Other Sellable",
                "SELLABLE", true, false, false, false, "ACTIVE", null, Now));
            var otherBalance = InventoryBalance.Create(
                Guid.NewGuid(), tenantId, otherLocationId, productId, null, null, Now);
            otherBalance.AdjustQuantities(otherOutletStock, 0m, 0m, 0m, Now);
            dbContext.InventoryBalances.Add(otherBalance);
        }

        await dbContext.SaveChangesAsync();
        return new Scenario(
            tenantId,
            customerId,
            outletId,
            productId,
            selectedBalanceId,
            $"cart-{Guid.NewGuid():N}");
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private sealed record Scenario(
        Guid TenantId,
        Guid CustomerId,
        Guid OutletId,
        Guid ProductId,
        Guid SelectedBalanceId,
        string CartSessionId);
}
