using System.Reflection;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosCheckoutRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CalculateSummaryAsync_WithOpenTillAndPricedVariant_ReturnsTotals()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, isTrusted: true);
        SeedTenantUser(dbContext, tenantId, userId, "Cashier 001");
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1250m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var request = new PosCheckoutSummaryRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(variantId, 2)]);

        var result = await repository.CalculateSummaryAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCash, PaymentPermissions.AcceptCard],
            request,
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Summary);
        Assert.Equal(2, result.Summary!.BillingSummary.ItemCount);
        Assert.Equal(2500, result.Summary.BillingSummary.Subtotal);
        Assert.Equal(2500, result.Summary.BillingSummary.TotalPayable);
        Assert.Equal("LKR", result.Summary.BillingSummary.Currency);
        Assert.Equal("New Sale", result.Summary.SaleDetails.SaleType);
        Assert.Equal("Cashier 001", result.Summary.SaleDetails.CashierName);
        Assert.Equal(["cash", "card"], result.Summary.PaymentMethods);
        Assert.Empty(result.Summary.ValidationMessages);
    }

    [Fact]
    public async Task CalculateSummaryAsync_WhenTillSessionNotOpen_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, isTrusted: true);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 500m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var request = new PosCheckoutSummaryRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(variantId, 1)]);

        var result = await repository.CalculateSummaryAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCash],
            request,
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.not_found", result.ErrorCode);
    }

    [Fact]
    public async Task CalculateSummaryAsync_WhenVariantMissing_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, isTrusted: true);
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var request = new PosCheckoutSummaryRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

        var result = await repository.CalculateSummaryAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCash],
            request,
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.variant_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task StartPaymentAsync_WithCashPayment_CreatesSalePaymentAndReceipt()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, isTrusted: true);
        SeedTenantUser(dbContext, tenantId, userId, "Cashier 001");
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1250m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var request = new PosCheckoutStartPaymentRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(variantId, 2)],
            "cash",
            3000);

        var result = await repository.StartPaymentAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCash],
            request,
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Payment);
        Assert.Equal(2500, result.Payment!.Subtotal);
        Assert.Equal(2500, result.Payment.GrandTotal);
        Assert.Equal(3000, result.Payment.CashReceived);
        Assert.Equal(500, result.Payment.ChangeDue);
        Assert.Equal("cash", result.Payment.PaymentMethod);
        Assert.Equal("completed", result.Payment.SaleStatus);
        Assert.Single(result.Payment.Items);
        Assert.Equal(1, await dbContext.SalesOrders.CountAsync());
        Assert.Equal(1, await dbContext.SalesPayments.CountAsync());
        Assert.Equal(1, await dbContext.Receipts.CountAsync());
    }

    [Fact]
    public async Task StartPaymentAsync_WhenCashReceivedTooLow_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, isTrusted: true);
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1250m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var request = new PosCheckoutStartPaymentRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(variantId, 2)],
            "cash",
            2000);

        var result = await repository.StartPaymentAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCash],
            request,
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.insufficient_cash", result.ErrorCode);
        Assert.Equal(0, await dbContext.SalesOrders.CountAsync());
    }

    private static PosCheckoutRepository CreateRepository(EPosDbContext dbContext)
    {
        var tillSessionRepository = new PosTillSessionRepository(
            dbContext,
            new CodeSequenceRepository(dbContext),
            NullLogger<PosTillSessionRepository>.Instance);

        return new PosCheckoutRepository(dbContext, tillSessionRepository);
    }

    private static void SeedDeviceContext(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        Guid userId,
        DateTimeOffset now,
        bool isTrusted)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "DEV-001",
            "dev-001",
            "Test Tenant",
            "active",
            "LKR",
            "UTC",
            null,
            null,
            now));

        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Main Outlet",
            "MAIN-01",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            null,
            now));

        dbContext.Tills.Add(Till.Create(
            tillId,
            tenantId,
            outletId,
            "Front Till 01",
            "Front",
            1,
            "FRONT-01",
            "STANDARD",
            0m,
            "LKR",
            true,
            "ACTIVE",
            null,
            now));

        var device = PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "POS-01",
            "Front POS Device",
            "TABLET",
            "ACTIVE",
            null,
            now);

        if (isTrusted)
        {
            typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!
                .SetValue(device, true);
        }

        dbContext.PosDevices.Add(device);
        dbContext.TillDeviceAssignments.Add(
            TillDeviceAssignment.Create(Guid.NewGuid(), tenantId, outletId, tillId, deviceId, userId, now));
    }

    private static void SeedTenantUser(EPosDbContext dbContext, Guid tenantId, Guid userId, string displayName)
    {
        dbContext.TenantUsers.Add(TenantUser.Create(
            userId,
            tenantId,
            "cashier@test.com",
            displayName,
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "cashier",
            "outlet",
            "default",
            Now));
    }

    private static void SeedOpenTillSession(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        Guid userId)
    {
        dbContext.TillSessions.Add(TillSession.Open(
            Guid.NewGuid(),
            tenantId,
            outletId,
            tillId,
            "TS-0001",
            DateOnly.FromDateTime(Now.UtcDateTime),
            userId,
            deviceId,
            100m,
            "LKR",
            null,
            Now));
    }

    private static Guid SeedSellableProduct(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid variantId)
    {
        var uomId = Guid.NewGuid();
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            uomId,
            tenantId,
            "EA",
            "Each",
            "COUNT",
            "ea",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));

        dbContext.Products.Add(Product.Create(
            productId,
            tenantId,
            "JER-001",
            "Team Jersey",
            "team-jersey",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now));

        dbContext.ProductVariants.Add(ProductVariant.Create(
            variantId,
            tenantId,
            productId,
            "DEFAULT",
            "Team Jersey",
            "JER-SKU",
            uomId,
            uomId,
            true,
            true,
            false,
            ProductConstants.ActiveStatus,
            null,
            Now));

        return uomId;
    }

    private static async Task SeedDefaultPriceListAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        decimal sellingPrice)
    {
        var priceList = new PriceList();
        Set(priceList, "Id", Guid.NewGuid());
        Set(priceList, "TenantId", tenantId);
        Set(priceList, "IsDefaultPriceList", true);
        Set(priceList, "Status", "ACTIVE");
        Set(priceList, "PriceListCode", "DEFAULT");
        Set(priceList, "PriceListName", "Default Price List");
        Set(priceList, "PriceListType", "POS");
        Set(priceList, "CurrencyCode", "LKR");
        Set(priceList, "CreatedAt", Now);
        Set(priceList, "UpdatedAt", Now);
        dbContext.PriceLists.Add(priceList);

        var priceItem = new PriceListItem();
        Set(priceItem, "Id", Guid.NewGuid());
        Set(priceItem, "TenantId", tenantId);
        Set(priceItem, "PriceListId", priceList.Id);
        Set(priceItem, "ProductId", productId);
        Set(priceItem, "ProductVariantId", variantId);
        Set(priceItem, "SellingPrice", sellingPrice);
        Set(priceItem, "MinQuantity", 1m);
        Set(priceItem, "Status", "ACTIVE");
        Set(priceItem, "CreatedAt", Now);
        Set(priceItem, "UpdatedAt", Now);
        dbContext.PriceListItems.Add(priceItem);

        await dbContext.SaveChangesAsync();
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var prop = entity.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
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
