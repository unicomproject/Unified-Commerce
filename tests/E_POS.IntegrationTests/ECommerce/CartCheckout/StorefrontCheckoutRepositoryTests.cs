using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
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
    private static readonly DateTimeOffset CollectionAt =
        new(2026, 7, 17, 11, 0, 0, TimeSpan.FromHours(5.5));

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
            Guid.NewGuid(), scenario.CustomerId, created.Checkout!.Id, Now, CancellationToken.None);

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
        var selected = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OutletId,
                RequestedCollectionAt = CollectionAt
            },
            Now,
            CancellationToken.None);
        Assert.True(selected.IsSuccess);

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
        var order = await dbContext.SalesOrders.SingleAsync();
        Assert.Equal(CollectionAt.ToUniversalTime(), order.RequestedCollectionAt);
        Assert.Equal(CollectionAt.AddMinutes(30).ToUniversalTime(), order.RequestedCollectionEndAt);
        Assert.Equal("Asia/Colombo", order.CollectionTimezoneSnapshot);
    }

    [Fact]
    public async Task UpdateCollectionAsync_ChangesOutletAndMovesReservationAtomically()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(
            dbContext, selectedOutletStock: 5m, otherOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 2m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var result = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OtherOutletId!.Value,
                RequestedCollectionAt = CollectionAt
            },
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(scenario.OtherOutletId, result.Checkout!.SelectedOutletId);
        Assert.Equal(0m, (await dbContext.InventoryBalances
            .SingleAsync(x => x.Id == scenario.SelectedBalanceId)).ReservedQuantity);
        Assert.Equal(2m, (await dbContext.InventoryBalances
            .SingleAsync(x => x.Id == scenario.OtherBalanceId)).ReservedQuantity);
        Assert.Equal(2, await dbContext.InventoryReservations.CountAsync());
        Assert.Equal("RELEASED", (await dbContext.InventoryReservations
            .SingleAsync(x => x.FulfillmentOutletId == scenario.OutletId)).ReservationStatus);
    }

    [Fact]
    public async Task UpdateCollectionAsync_OtherTenantCannotChangeCheckoutSelection()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 1m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var result = await repository.UpdateCollectionAsync(
            Guid.NewGuid(),
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OutletId,
                RequestedCollectionAt = CollectionAt
            },
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.session_not_found", result.ErrorCode);
        Assert.Null((await dbContext.CheckoutSessions.SingleAsync()).RequestedCollectionAt);
    }

    [Fact]
    public async Task UpdateCollectionAsync_NewOutletInsufficientStock_LeavesOriginalReservationIntact()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(
            dbContext, selectedOutletStock: 5m, otherOutletStock: 1m);
        await AddCartItemAsync(dbContext, scenario, 2m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var result = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OtherOutletId!.Value,
                RequestedCollectionAt = CollectionAt
            },
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.insufficient_stock", result.ErrorCode);
        Assert.Equal(2m, (await dbContext.InventoryBalances
            .SingleAsync(x => x.Id == scenario.SelectedBalanceId)).ReservedQuantity);
        Assert.Equal(0m, (await dbContext.InventoryBalances
            .SingleAsync(x => x.Id == scenario.OtherBalanceId)).ReservedQuantity);
        Assert.Single(await dbContext.InventoryReservations.ToListAsync());
        Assert.Equal(
            scenario.OutletId,
            (await dbContext.CheckoutSessions.SingleAsync()).SelectedOutletId);
    }

    [Fact]
    public async Task UpdateCollectionAsync_AtFourteenthLocalDayBoundary_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 1m);
        dbContext.OutletBusinessHours.Add(OutletBusinessHour.Create(
            Guid.NewGuid(), scenario.TenantId, scenario.OutletId, 4,
            new TimeOnly(9, 0), new TimeOnly(18, 0), false, null, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);

        var result = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OutletId,
                RequestedCollectionAt =
                    new DateTimeOffset(2026, 7, 30, 11, 0, 0, TimeSpan.FromHours(5.5))
            },
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.collection_time_unavailable", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateCollectionAsync_ExactRetryAfterLeadWindowMoves_ReturnsExistingSelection()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 1m);
        dbContext.OutletBusinessHours.Add(OutletBusinessHour.Create(
            Guid.NewGuid(), scenario.TenantId, scenario.OutletId, 4,
            new TimeOnly(9, 0), new TimeOnly(18, 0), false, null, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);
        var request = new UpdateStorefrontCheckoutCollectionRequest
        {
            SelectedOutletId = scenario.OutletId,
            RequestedCollectionAt = Now.AddMinutes(30)
        };

        var first = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            request,
            Now,
            CancellationToken.None);
        var eventCount = await dbContext.CheckoutEvents.CountAsync();
        var retry = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout.Id,
            request,
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(retry.IsSuccess);
        Assert.Equal(first.Checkout!.RequestedCollectionAt, retry.Checkout!.RequestedCollectionAt);
        Assert.Equal(first.Checkout.RequestedCollectionEndAt, retry.Checkout.RequestedCollectionEndAt);
        Assert.Equal(eventCount, await dbContext.CheckoutEvents.CountAsync());
    }

    [Fact]
    public async Task ConfirmAsync_OutletTimezoneChangedSinceSelection_RequiresReselection()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 1m);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario);
        var selected = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OutletId,
                RequestedCollectionAt = CollectionAt
            },
            Now,
            CancellationToken.None);
        Assert.True(selected.IsSuccess);

        var outlet = await dbContext.Outlets.SingleAsync(x => x.Id == scenario.OutletId);
        outlet.UpdateProfile(
            outlet.OutletName,
            outlet.OutletCode,
            outlet.Status,
            outlet.OutletType,
            "Asia/Kolkata",
            outlet.IsDefaultOutlet,
            outlet.Phone,
            outlet.Email,
            null,
            Now.AddMinutes(1));
        await dbContext.SaveChangesAsync();

        var result = await repository.ConfirmAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout.Id,
            "timezone-change",
            Now.AddMinutes(2),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.collection_time_unavailable", result.ErrorCode);
        Assert.Empty(await dbContext.SalesOrders.ToListAsync());
        Assert.Equal(
            "Asia/Colombo",
            (await dbContext.CheckoutSessions.SingleAsync()).CollectionTimezoneSnapshot);
    }

    [Theory]
    [InlineData(4, 30)]
    [InlineData(5, 30)]
    public async Task UpdateCollectionAsync_DstAmbiguousStartOrEnd_IsRejected(
        int requestedUtcHour,
        int requestedUtcMinute)
    {
        var operationNow = new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(
            dbContext,
            selectedOutletStock: 5m,
            timezone: "America/New_York",
            businessDay: 0,
            openingTime: new TimeOnly(0, 0),
            closingTime: new TimeOnly(4, 0));
        await AddCartItemAsync(dbContext, scenario, 1m, operationNow);
        var repository = new StorefrontCheckoutRepository(dbContext);
        var created = await CreateCheckoutAsync(repository, scenario, operationNow);

        var result = await repository.UpdateCollectionAsync(
            scenario.TenantId,
            scenario.CustomerId,
            created.Checkout!.Id,
            new UpdateStorefrontCheckoutCollectionRequest
            {
                SelectedOutletId = scenario.OutletId,
                RequestedCollectionAt = new DateTimeOffset(
                    2026, 11, 1, requestedUtcHour, requestedUtcMinute, 0, TimeSpan.Zero)
            },
            operationNow,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.collection_time_unavailable", result.ErrorCode);
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
    public async Task CreateFromCartAsync_WithoutClickCollectEntitlement_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var scenario = await SeedScenarioAsync(dbContext, selectedOutletStock: 5m);
        await AddCartItemAsync(dbContext, scenario, 1m);
        var clickCollectEntitlement = await dbContext.TenantFeatureEntitlements
            .SingleAsync(x =>
                x.TenantId == scenario.TenantId &&
                x.PlatformFeatureId == SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId);
        dbContext.TenantFeatureEntitlements.Remove(clickCollectEntitlement);
        await dbContext.SaveChangesAsync();
        var repository = new StorefrontCheckoutRepository(dbContext);

        var result = await CreateCheckoutAsync(repository, scenario);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.feature_disabled", result.ErrorCode);
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
        CreateCheckoutAsync(
            StorefrontCheckoutRepository repository,
            Scenario scenario,
            DateTimeOffset? operationNow = null) =>
        repository.CreateFromCartAsync(
            scenario.TenantId,
            scenario.CustomerId,
            scenario.CartSessionId,
            new CreateStorefrontCheckoutFromCartRequest { SelectedOutletId = scenario.OutletId },
            operationNow ?? Now,
            CancellationToken.None);

    private static async Task AddCartItemAsync(
        EPosDbContext dbContext,
        Scenario scenario,
        decimal quantity,
        DateTimeOffset? operationNow = null)
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
            operationNow ?? Now,
            CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    private static async Task<Scenario> SeedScenarioAsync(
        EPosDbContext dbContext,
        decimal selectedOutletStock,
        decimal otherOutletStock = 0m,
        string timezone = "Asia/Colombo",
        short businessDay = 5,
        TimeOnly? openingTime = null,
        TimeOnly? closingTime = null)
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
        var fulfillmentMethodId = Guid.NewGuid();

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
        dbContext.PlatformFeatures.AddRange(
            PlatformFeature.Create(
                SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId,
                SubscriptionBillingCatalogSeedConstants.CoreCommerceModuleId,
                PlatformTenantFeatureCodes.OnlineStore,
                "Online Store",
                SubscriptionCatalogConstants.RecordStatus.Active,
                Now),
            PlatformFeature.Create(
                SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId,
                SubscriptionBillingCatalogSeedConstants.CoreCommerceModuleId,
                PlatformTenantFeatureCodes.ClickCollect,
                "Click & Collect",
                SubscriptionCatalogConstants.RecordStatus.Active,
                Now));
        dbContext.TenantFeatureEntitlements.AddRange(
            TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenantId,
                SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId,
                TenantEntitlementStatusConstants.Enabled,
                Now),
            TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenantId,
                SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId,
                TenantEntitlementStatusConstants.Enabled,
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
            timezone, true, null, null, null, Now));
        dbContext.FulfillmentMethods.Add(FulfillmentMethod.Create(
            fulfillmentMethodId, tenantId, "PICKUP", "Click and Collect",
            null, "ACTIVE", "PICKUP", Now));
        dbContext.FulfillmentMethodOutlets.Add(FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, fulfillmentMethodId, outletId,
            30, 30, new TimeOnly(17, 0), "ACTIVE", Now));
        dbContext.OutletBusinessHours.Add(OutletBusinessHour.Create(
            Guid.NewGuid(), tenantId, outletId, businessDay,
            openingTime ?? new TimeOnly(9, 0),
            closingTime ?? new TimeOnly(18, 0),
            false, null, null, Now));
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
        var priceListId = Guid.NewGuid();
        dbContext.PriceLists.Add(PriceList.Create(
            priceListId,
            tenantId,
            $"PL-{priceListId:N}"[..20],
            "Default LKR Price List",
            "STANDARD",
            "LKR",
            false,
            true,
            0,
            null,
            null,
            "ACTIVE",
            null,
            Now));
        dbContext.PriceListItems.Add(PriceListItem.Create(
            Guid.NewGuid(), tenantId, priceListId, productId, null, null,
            25m, null, 1m, null, null, "ACTIVE", null, Now));
        var selectedBalance = InventoryBalance.Create(
            selectedBalanceId, tenantId, selectedLocationId, productId, null, null, Now);
        selectedBalance.AdjustQuantities(selectedOutletStock, 0m, 0m, 0m, Now);
        dbContext.InventoryBalances.Add(selectedBalance);

        Guid? otherOutletId = null;
        Guid? otherBalanceId = null;
        if (otherOutletStock > 0m)
        {
            otherOutletId = Guid.NewGuid();
            var otherLocationId = Guid.NewGuid();
            dbContext.Outlets.Add(Outlet.Create(
                otherOutletId.Value, tenantId, "Other Store", "OTHER", "ACTIVE", "STORE",
                timezone, false, null, null, null, Now));
            dbContext.FulfillmentMethodOutlets.Add(FulfillmentMethodOutlet.Create(
                Guid.NewGuid(), tenantId, fulfillmentMethodId, otherOutletId.Value,
                30, 30, new TimeOnly(17, 0), "ACTIVE", Now));
            dbContext.OutletBusinessHours.Add(OutletBusinessHour.Create(
                Guid.NewGuid(), tenantId, otherOutletId.Value, businessDay,
                openingTime ?? new TimeOnly(9, 0),
                closingTime ?? new TimeOnly(18, 0),
                false, null, null, Now));
            dbContext.InventoryLocations.Add(InventoryLocation.Create(
                otherLocationId, tenantId, otherOutletId.Value, null, "OTHER-SELL", "Other Sellable",
                "SELLABLE", true, false, false, false, "ACTIVE", null, Now));
            otherBalanceId = Guid.NewGuid();
            var otherBalance = InventoryBalance.Create(
                otherBalanceId.Value, tenantId, otherLocationId, productId, null, null, Now);
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
            otherOutletId,
            otherBalanceId,
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
        Guid? OtherOutletId,
        Guid? OtherBalanceId,
        string CartSessionId);
}
