using System.Reflection;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.Payment.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Application.Modules.Tenant.Discount.Services;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
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

    [Theory]
    [InlineData(1, 2800, 700, 2100)]
    [InlineData(2, 5600, 1400, 4200)]
    public async Task CalculateSummaryAsync_WithEligibleAutomaticPercentageOffer_ReturnsAuthoritativeTotals(
        int quantity, int expectedSubtotal, int expectedDiscount, int expectedTotal)
    {
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var productId = Guid.NewGuid(); var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, true);
        SeedTenantUser(dbContext, tenantId, userId, "Cashier 001");
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 2800m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        SeedAutomaticPercentageOffer(dbContext, tenantId, productId, 25m, "ACTIVE", Now.AddHours(-1), Now.AddHours(1));
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CalculateSummaryAsync(
            tenantId, userId, [PaymentPermissions.AcceptCash],
            new(deviceId, "NewSale", null, [new(variantId, quantity)]),
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedSubtotal, result.Summary!.BillingSummary.Subtotal);
        Assert.Equal(expectedDiscount, result.Summary.BillingSummary.AutomaticDiscount);
        Assert.Equal(expectedTotal, result.Summary.BillingSummary.TotalPayable);
        var line = Assert.Single(result.Summary.Lines!);
        Assert.Equal(2800, line.BaseUnitPrice);
        Assert.Equal(expectedDiscount, line.AutomaticDiscount);
        Assert.Equal(2100, line.EffectiveUnitPrice);
        Assert.NotNull(line.AppliedPromotion);
    }

    [Theory]
    [InlineData("INACTIVE", -1, 1)]
    [InlineData("ACTIVE", -2, -1)]
    public async Task CalculateSummaryAsync_WithInactiveOrExpiredOffer_DoesNotApply(
        string status, int startsHours, int endsHours)
    {
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var productId = Guid.NewGuid(); var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, true);
        SeedTenantUser(dbContext, tenantId, userId, "Cashier 001");
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 2800m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        SeedAutomaticPercentageOffer(dbContext, tenantId, productId, 25m, status,
            Now.AddHours(startsHours), Now.AddHours(endsHours));
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CalculateSummaryAsync(
            tenantId, userId, [PaymentPermissions.AcceptCash],
            new(deviceId, "NewSale", null, [new(variantId, 1)]), Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Summary!.BillingSummary.AutomaticDiscount);
        Assert.Equal(2800, result.Summary.BillingSummary.TotalPayable);
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
    public async Task CalculateSummaryAsync_NormalizesDuplicateVariantsBeforeCalculating()
    {
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var productId = Guid.NewGuid(); var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, true);
        SeedTenantUser(dbContext, tenantId, userId, "Cashier 001");
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 500m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        var location = InventoryLocation.Create(Guid.NewGuid(), tenantId, outletId, null,
            "POS-FLOOR", "POS Floor", "STORE", true, false, false, false,
            "ACTIVE", userId, Now);
        var balance = InventoryBalance.Create(Guid.NewGuid(), tenantId, location.Id,
            productId, variantId, null, Now);
        balance.AdjustQuantities(5, 0, 0, 0, Now);
        dbContext.InventoryLocations.Add(location);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CalculateSummaryAsync(
            tenantId, userId, [PaymentPermissions.AcceptCash],
            new PosCheckoutSummaryRequestDto(deviceId, "NewSale", null,
                [new(variantId, 1), new(variantId, 2)]), Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Summary!.BillingSummary.ItemCount);
        Assert.Equal(1500, result.Summary.BillingSummary.Subtotal);
    }

    [Fact]
    public async Task CalculateSummaryAsync_WhenAnyLineIsInvalid_RejectsWholeCart()
    {
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var productId = Guid.NewGuid(); var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, true);
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 500m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CalculateSummaryAsync(
            tenantId, userId, [], new PosCheckoutSummaryRequestDto(deviceId, "NewSale", null,
                [new(variantId, 1), new(Guid.Empty, 1)]), Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.invalid_lines", result.ErrorCode);
    }

    [Fact]
    public async Task CalculateSummaryAsync_WithApprovedDiscount_ReturnsDiscountedTotals()
    {
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var productId = Guid.NewGuid(); var variantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, Now, true);
        SeedTenantUser(dbContext, tenantId, userId, "Cashier 001");
        SeedOpenTillSession(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await SeedDefaultPriceListAsync(dbContext, tenantId, productId, variantId, 1000m);
        SeedSellableProduct(dbContext, tenantId, productId, variantId);
        await dbContext.SaveChangesAsync();
        var sessionId = await dbContext.TillSessions.Select(x => x.Id).SingleAsync();
        var type = DiscountType.Create(Guid.NewGuid(), "TEST_PERCENT", "Test Percent", "PERCENTAGE", true, "ACTIVE", Now);
        var policy = DiscountPolicy.Create(Guid.NewGuid(), tenantId, type.Id, "TEST10", "Test 10%", null,
            "ORDER", 10m, null, null, null, null, false, false, null, 1, null, null,
            "ACTIVE", userId, Now);
        dbContext.DiscountTypes.Add(type); dbContext.DiscountPolicies.Add(policy);
        var lines = new[] { new PosCheckoutLineRequestDto(variantId, 2) };
        var snapshot = PosDiscountCartFingerprint.CreateSnapshotJson(deviceId, "NewSale", null, lines, 2000, "LKR");
        var application = PosDiscountApplication.Create(Guid.NewGuid(), tenantId, policy.Id, type.Id,
            outletId, tillId, sessionId, deviceId, userId, null, null, "summary-key", "POLICY", "ORDER",
            policy.DiscountPolicyCode, policy.DiscountPolicyName, "PERCENTAGE", 10m, 10m, 10m,
            2000m, 2000m, 200m, 1800m, "LKR", snapshot, PosDiscountCartFingerprint.Hash(snapshot),
            null, false, Now.AddMinutes(15), Now);
        dbContext.PosDiscountApplications.Add(application); await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CalculateSummaryAsync(tenantId, userId,
            [PaymentPermissions.AcceptCash], new(deviceId, "NewSale", null, lines, application.Id),
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Summary!.BillingSummary.Discount);
        Assert.Equal(1800, result.Summary.BillingSummary.TotalPayable);
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
        var location = InventoryLocation.Create(Guid.NewGuid(), tenantId, outletId, null,
            "POS-FLOOR", "POS Floor", "STORE", true, false, false, false,
            "ACTIVE", userId, Now);
        var balance = InventoryBalance.Create(Guid.NewGuid(), tenantId, location.Id,
            productId, variantId, null, Now);
        balance.AdjustQuantities(5, 0, 0, 0, Now);
        dbContext.InventoryLocations.Add(location);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var request = new PosCheckoutStartPaymentRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(variantId, 2)],
            "cash",
            3000,
            null,
            "cash-success-key");

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
        Assert.Null((await dbContext.SalesOrders.SingleAsync()).CustomerId);
        Assert.Equal(1, await dbContext.SalesPayments.CountAsync());
        Assert.Equal(1, await dbContext.Receipts.CountAsync());
        Assert.Equal(1, await dbContext.SalesPaymentTransactions.CountAsync());
        Assert.Equal(1, await dbContext.SalesPaymentEvents.CountAsync());
        Assert.Equal(1, await dbContext.StockMovements.CountAsync());
        Assert.Equal(1, await dbContext.StockMovementReferences.CountAsync());
        Assert.Equal(3, (await dbContext.InventoryBalances.SingleAsync()).OnHandQuantity);

        var payment = await dbContext.SalesPayments.SingleAsync();
        Assert.Null(payment.ExternalReference);
        var txn = await dbContext.SalesPaymentTransactions.SingleAsync();
        Assert.Equal("CASH", txn.ProviderName);
        Assert.Null(txn.ExternalTransactionReference);
        Assert.Null(txn.ProviderResponseJson);
        Assert.DoesNotContain("cardBrand", txn.ProviderResponseJson ?? string.Empty);
        Assert.DoesNotContain("cardLast4", txn.ProviderResponseJson ?? string.Empty);

        var replay = await repository.StartPaymentAsync(
            tenantId, userId, [PaymentPermissions.AcceptCash], request, Now.AddSeconds(1),
            CancellationToken.None);
        Assert.True(replay.IsSuccess);
        Assert.Equal(result.Payment.SaleId, replay.Payment!.SaleId);
        Assert.Equal(1, await dbContext.SalesOrders.CountAsync());
        Assert.Equal(1, await dbContext.SalesPayments.CountAsync());
        Assert.Equal(1, await dbContext.StockMovements.CountAsync());
    }

    [Fact]
    public async Task StartPaymentAsync_WithActiveSameTenantCustomer_PersistsCustomerIdOnce()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedCashCheckoutFixtureAsync(dbContext);
        var customerId = Guid.NewGuid();
        dbContext.Customers.Add(CustomerEntity.CreatePosCustomer(
            customerId, fixture.TenantId, "CUS-STEP3", "Step 3 Customer",
            "+94770000003", "step3@example.test", fixture.UserId, Now));
        await dbContext.SaveChangesAsync();
        var request = CashRequest(fixture, customerId, "customer-persist-key");
        var repository = CreateRepository(dbContext);

        var result = await repository.StartPaymentAsync(
            fixture.TenantId, fixture.UserId, [PaymentPermissions.AcceptCash],
            request, Now, CancellationToken.None);
        var replay = await repository.StartPaymentAsync(
            fixture.TenantId, fixture.UserId, [PaymentPermissions.AcceptCash],
            request, Now.AddSeconds(1), CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.True(replay.IsSuccess, replay.ErrorCode);
        Assert.Equal(customerId, (await dbContext.SalesOrders.SingleAsync()).CustomerId);
        Assert.Equal(result.Payment!.SaleId, replay.Payment!.SaleId);
        Assert.Equal(1, await dbContext.SalesOrders.CountAsync());
    }

    [Fact]
    public async Task StartPaymentAsync_WithUnknownCustomer_RejectsWithoutOrder()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedCashCheckoutFixtureAsync(dbContext);

        var result = await CreateRepository(dbContext).StartPaymentAsync(
            fixture.TenantId, fixture.UserId, [PaymentPermissions.AcceptCash],
            CashRequest(fixture, Guid.NewGuid(), "unknown-customer-key"),
            Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.customer_not_found", result.ErrorCode);
        Assert.Empty(dbContext.SalesOrders);
    }

    [Fact]
    public async Task StartPaymentAsync_WithCrossTenantCustomer_RejectsWithoutOrder()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedCashCheckoutFixtureAsync(dbContext);
        var customerId = Guid.NewGuid();
        dbContext.Customers.Add(CustomerEntity.CreatePosCustomer(
            customerId, Guid.NewGuid(), "CUS-OTHER", "Other Tenant Customer",
            "+94770000004", null, fixture.UserId, Now));
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).StartPaymentAsync(
            fixture.TenantId, fixture.UserId, [PaymentPermissions.AcceptCash],
            CashRequest(fixture, customerId, "cross-tenant-key"),
            Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.customer_not_found", result.ErrorCode);
        Assert.Empty(dbContext.SalesOrders);
    }

    [Fact]
    public async Task StartPaymentAsync_WithInactiveCustomer_RejectsWithoutOrder()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedCashCheckoutFixtureAsync(dbContext);
        var customerId = Guid.NewGuid();
        var customer = CustomerEntity.CreatePosCustomer(
            customerId, fixture.TenantId, "CUS-INACTIVE", "Inactive Customer",
            "+94770000005", null, fixture.UserId, Now);
        customer.UpdatePosProfile(
            "Inactive Customer", "+94770000005", null, "INACTIVE",
            fixture.UserId, Now);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).StartPaymentAsync(
            fixture.TenantId, fixture.UserId, [PaymentPermissions.AcceptCash],
            CashRequest(fixture, customerId, "inactive-customer-key"),
            Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.customer_inactive", result.ErrorCode);
        Assert.Empty(dbContext.SalesOrders);
    }

    [Fact]
    public async Task StartPaymentAsync_WithCardProviderUnavailable_DoesNotPersistAsCash()
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
        var result = await repository.StartPaymentAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCard],
            new PosCheckoutStartPaymentRequestDto(
                deviceId,
                "NewSale",
                null,
                [new PosCheckoutLineRequestDto(variantId, 1)],
                "card",
                null,
                null,
                "card-blocked-key",
                Guid.NewGuid()),
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.card_provider_not_configured", result.ErrorCode);
        Assert.Equal(0, await dbContext.SalesPayments.CountAsync());
        Assert.Equal(0, await dbContext.SalesPaymentTransactions.CountAsync());
        Assert.Equal(0, await dbContext.SalesOrders.CountAsync());
    }

    [Fact]
    public async Task StartPaymentAsync_WithCompletedCardCapture_PersistsProviderPayment_NotCash()
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
        var location = InventoryLocation.Create(Guid.NewGuid(), tenantId, outletId, null,
            "CARD-POS-FLOOR", "Card POS Floor", "STORE", true, false, false, false,
            "ACTIVE", userId, Now);
        var balance = InventoryBalance.Create(Guid.NewGuid(), tenantId, location.Id,
            productId, variantId, null, Now);
        balance.AdjustQuantities(5, 0, 0, 0, Now);
        dbContext.InventoryLocations.Add(location);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(
            dbContext,
            new StubCardGateway(new CardPaymentCaptureResult(
                CardPaymentOutcome.Completed,
                "TEST_TERMINAL",
                "provider-txn-1",
                "VISA",
                "4242",
                "AUTH-1",
                "TERM-1")));
        var result = await repository.StartPaymentAsync(
            tenantId,
            userId,
            [PaymentPermissions.AcceptCard],
            new PosCheckoutStartPaymentRequestDto(
                deviceId, "NewSale", null,
                [new PosCheckoutLineRequestDto(variantId, 1)],
                "card", null, null, "card-success-key", Guid.NewGuid()),
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        var transaction = await dbContext.SalesPaymentTransactions.SingleAsync();
        Assert.Equal("TEST_TERMINAL", transaction.ProviderName);
        Assert.NotEqual("CASH", transaction.ProviderName);
        Assert.Contains("\"cardLast4\":\"4242\"", transaction.ProviderResponseJson);
        Assert.Equal("4242", result.Payment!.Tenders!.Single().MaskedCardLast4);
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
            2000,
            null,
            "cash-low-key");

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

    [Fact]
    public async Task StartPaymentAsync_WithApprovedLineDiscount_PersistsTargetLineAndAppliesApplication()
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
        var location = InventoryLocation.Create(Guid.NewGuid(), tenantId, outletId, null,
            "POS-FLOOR", "POS Floor", "STORE", true, false, false, false,
            "ACTIVE", userId, Now);
        var balance = InventoryBalance.Create(Guid.NewGuid(), tenantId, location.Id,
            productId, variantId, null, Now);
        balance.AdjustQuantities(5, 0, 0, 0, Now);
        dbContext.InventoryLocations.Add(location);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();

        var sessionId = await dbContext.TillSessions.Select(x => x.Id).SingleAsync();
        var type = DiscountType.Create(Guid.NewGuid(), "TEST_PERCENT", "Test Percent", "PERCENTAGE", true, "ACTIVE", Now);
        var policy = DiscountPolicy.Create(
            Guid.NewGuid(), tenantId, type.Id, "TEST10", "Test 10%", null, "LINE",
            10m, null, null, null, null, false, false, null, 1, null, null,
            "ACTIVE", userId, Now);
        dbContext.DiscountTypes.Add(type);
        dbContext.DiscountPolicies.Add(policy);

        var lines = new[] { new PosCheckoutLineRequestDto(variantId, 2) };
        var snapshot = PosDiscountCartFingerprint.CreateSnapshotJson(
            deviceId, "NewSale", null, lines, 2500, "LKR");
        var application = PosDiscountApplication.Create(
            Guid.NewGuid(), tenantId, policy.Id, type.Id, outletId, tillId, sessionId,
            deviceId, userId, null, variantId, "test-key", "POLICY", "LINE",
            policy.DiscountPolicyCode, policy.DiscountPolicyName, "PERCENTAGE",
            10m, 10m, 10m, 2500m, 2500m, 250m, 2250m, "LKR",
            snapshot, PosDiscountCartFingerprint.Hash(snapshot), null, false,
            Now.AddMinutes(15), Now);
        dbContext.PosDiscountApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.StartPaymentAsync(
            tenantId, userId, [PaymentPermissions.AcceptCash],
            new PosCheckoutStartPaymentRequestDto(
                deviceId, "NewSale", null, lines, "cash", 2500, application.Id,
                "discount-payment-key"),
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.Payment!.DiscountTotal);
        Assert.Equal(2250, result.Payment.GrandTotal);
        Assert.Equal(250, result.Payment.ChangeDue);
        var orderLine = await dbContext.SalesOrderLines.SingleAsync();
        var orderDiscount = await dbContext.SalesOrderDiscounts.SingleAsync();
        Assert.Equal("LINE", orderDiscount.DiscountTargetScope);
        Assert.Equal(orderLine.Id, orderDiscount.SalesOrderLineId);
        Assert.Equal("APPLIED", (await dbContext.PosDiscountApplications.SingleAsync()).ApplicationStatus);
        Assert.Contains(await dbContext.PosDiscountApplicationEvents.ToListAsync(), x => x.EventType == "APPLIED");
    }

    private static PosCheckoutRepository CreateRepository(
        EPosDbContext dbContext,
        ICardPaymentGateway? cardGateway = null)
    {
        var tillSessionRepository = new PosTillSessionRepository(
            dbContext,
            new CodeSequenceRepository(dbContext),
            NullLogger<PosTillSessionRepository>.Instance);

        return new PosCheckoutRepository(dbContext, tillSessionRepository, new StubReceiptTemplateResolutionService(), cardGateway);
    }

    private static PosCheckoutStartPaymentRequestDto CashRequest(
        CashCheckoutFixture fixture,
        Guid? customerId,
        string idempotencyKey) => new(
            fixture.DeviceId,
            "NewSale",
            customerId,
            [new PosCheckoutLineRequestDto(fixture.VariantId, 1)],
            "cash",
            1500,
            null,
            idempotencyKey);

    private static async Task<CashCheckoutFixture> SeedCashCheckoutFixtureAsync(
        EPosDbContext dbContext)
    {
        var fixture = new CashCheckoutFixture(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SeedDeviceContext(dbContext, fixture.TenantId, fixture.OutletId,
            fixture.TillId, fixture.DeviceId, fixture.UserId, Now, isTrusted: true);
        SeedTenantUser(dbContext, fixture.TenantId, fixture.UserId, "Cashier 001");
        SeedOpenTillSession(dbContext, fixture.TenantId, fixture.OutletId,
            fixture.TillId, fixture.DeviceId, fixture.UserId);
        await SeedDefaultPriceListAsync(dbContext, fixture.TenantId,
            fixture.ProductId, fixture.VariantId, 1250m);
        SeedSellableProduct(dbContext, fixture.TenantId,
            fixture.ProductId, fixture.VariantId);
        var location = InventoryLocation.Create(
            Guid.NewGuid(), fixture.TenantId, fixture.OutletId, null,
            "STEP3-POS-FLOOR", "Step 3 POS Floor", "STORE", true, false,
            false, false, "ACTIVE", fixture.UserId, Now);
        var balance = InventoryBalance.Create(
            Guid.NewGuid(), fixture.TenantId, location.Id, fixture.ProductId,
            fixture.VariantId, null, Now);
        balance.AdjustQuantities(5, 0, 0, 0, Now);
        dbContext.InventoryLocations.Add(location);
        dbContext.InventoryBalances.Add(balance);
        await dbContext.SaveChangesAsync();
        return fixture;
    }

    private sealed record CashCheckoutFixture(
        Guid TenantId,
        Guid OutletId,
        Guid TillId,
        Guid DeviceId,
        Guid UserId,
        Guid ProductId,
        Guid VariantId);

    private sealed class StubCardGateway(CardPaymentCaptureResult result) : ICardPaymentGateway
    {
        public Task<CardPaymentCaptureResult> CaptureAsync(
            CardPaymentCaptureRequest request,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class StubReceiptTemplateResolutionService : E_POS.Application.Modules.Tenant.POSOperations.Contracts.IReceiptTemplateResolutionService
    {
        public Task<E_POS.Application.Modules.Tenant.POSOperations.Dtos.ResolvedReceiptTemplateDto?> ResolveTemplateAsync(
            Guid tenantId, Guid outletId, Guid tillId, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult<E_POS.Application.Modules.Tenant.POSOperations.Dtos.ResolvedReceiptTemplateDto?>(null);
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

        var methods = new[]
        {
            ("CASH", "Cash", "CASH", true, 0),
            ("CARD", "Card", "CARD", false, 1),
            ("QR", "QR", "QR", false, 2),
            ("SPLIT", "Split", "OTHER", true, 3)
        };
        foreach (var method in methods)
        {
            dbContext.PaymentMethods.Add(PaymentMethod.Create(
                Guid.NewGuid(), tenantId, method.Item1, method.Item2, method.Item3,
                true, method.Item4, method.Item5, "ACTIVE", null, Now));
        }

        await dbContext.SaveChangesAsync();
    }

    private static void SeedAutomaticPercentageOffer(
        EPosDbContext dbContext, Guid tenantId, Guid productId, decimal value,
        string status, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        var type = DiscountType.Create(Guid.NewGuid(), "AUTO_PERCENT", "Automatic percentage",
            "PERCENTAGE", true, "ACTIVE", Now);
        var policy = DiscountPolicy.Create(Guid.NewGuid(), tenantId, type.Id,
            "AUTO25", "25% automatic offer", null, "LINE", value, "LKR", null,
            null, null, false, false, null, 10, startsAt, endsAt, status, null, Now);
        var target = DiscountPolicyTarget.Create(Guid.NewGuid(), tenantId, policy.Id,
            "PRODUCT", "INCLUDE", productId, null, null, null, null,
            "ACTIVE", null, Now);
        dbContext.DiscountTypes.Add(type);
        dbContext.DiscountPolicies.Add(policy);
        dbContext.DiscountPolicyTargets.Add(target);
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
