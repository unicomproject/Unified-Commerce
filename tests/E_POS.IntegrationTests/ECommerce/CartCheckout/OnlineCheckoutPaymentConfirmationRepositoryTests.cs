using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CartCheckout;

public sealed class OnlineCheckoutPaymentConfirmationRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_Success_MarksOrderAndPaymentPaid()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId, transactionId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, transactionId) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var repository = new OnlineCheckoutPaymentConfirmationRepository(db);

        var result = await repository.ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);

        Assert.True(result.Found);
        Assert.True(result.Applied);
        Assert.True(result.ShouldNotify);
        Assert.Null(result.AnomalyCode);

        var order = await db.SalesOrders.SingleAsync(x => x.Id == orderId);
        var payment = await db.SalesPayments.SingleAsync(x => x.Id == paymentId);
        var transaction = await db.SalesPaymentTransactions.SingleAsync(x => x.Id == transactionId);
        Assert.Equal("PAID", order.PaymentStatus);
        Assert.Equal(0m, order.BalanceDue);
        Assert.Equal("PAID", payment.PaymentStatus);
        Assert.Equal(2, payment.RowVersion);
        Assert.Equal("SUCCEEDED", transaction.TransactionStatus);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_DuplicateDelivery_ReportsAlreadyProcessedAndStillNotifies()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId, transactionId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, transactionId) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();
        }
        _ = transactionId;

        await using (var firstDb = CreateDbContext(dbName))
        {
            var first = await new OnlineCheckoutPaymentConfirmationRepository(firstDb).ApplyCheckoutCompletedAsync(
                tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);
            Assert.True(first.Applied);
        }

        await using var secondDb = CreateDbContext(dbName);
        var second = await new OnlineCheckoutPaymentConfirmationRepository(secondDb).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now.AddMinutes(1), CancellationToken.None);

        Assert.True(second.Found);
        Assert.False(second.Applied);
        Assert.True(second.ShouldNotify);
        Assert.Null(second.AnomalyCode);

        var payment = await secondDb.SalesPayments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal(100m, payment.PaidAmount);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_ConflictingTerminalState_ReturnsAnomalyAndDoesNotNotify()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();

            var seedPayment = await seedDb.SalesPayments.SingleAsync(x => x.Id == paymentId);
            var seedOrder = await seedDb.SalesOrders.SingleAsync(x => x.Id == orderId);
            seedOrder.CancelForFailedOnlinePayment("Checkout session expired.", Now);
            seedPayment.MarkFailedOrCancelled("CANCELLED", "Checkout session expired.", Now);
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var result = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now.AddMinutes(5), CancellationToken.None);

        Assert.True(result.Found);
        Assert.False(result.Applied);
        Assert.False(result.ShouldNotify);
        Assert.Equal("payment_status_conflict", result.AnomalyCode);

        var payment = await db.SalesPayments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal("CANCELLED", payment.PaymentStatus);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_AmountMismatch_ReturnsAnomalyWithoutMutating()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var result = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 50m, "LKR", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);

        Assert.Equal("amount_mismatch", result.AnomalyCode);
        Assert.False(result.ShouldNotify);
        Assert.Equal("PENDING", (await db.SalesPayments.SingleAsync(x => x.Id == paymentId)).PaymentStatus);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_CurrencyMismatch_ReturnsAnomalyWithoutMutating()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m, currency: "LKR");
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var result = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "USD", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);

        Assert.Equal("currency_mismatch", result.AnomalyCode);
        Assert.False(result.ShouldNotify);
        Assert.Equal("PENDING", (await db.SalesPayments.SingleAsync(x => x.Id == paymentId)).PaymentStatus);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_SessionMismatch_ReturnsAnomalyWithoutMutating()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m, providerSessionId: "cs_test_real");
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var result = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_wrong", "pi_ext_1", null, Now, CancellationToken.None);

        Assert.Equal("session_mismatch", result.AnomalyCode);
        Assert.False(result.ShouldNotify);
        Assert.Equal("PENDING", (await db.SalesPayments.SingleAsync(x => x.Id == paymentId)).PaymentStatus);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_NoRecordedSession_SkipsSessionCheckForBackwardCompatibility()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m, providerSessionId: null);
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var result = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_any", "pi_ext_1", null, Now, CancellationToken.None);

        Assert.True(result.Applied);
        Assert.Null(result.AnomalyCode);
    }

    [Fact]
    public async Task ApplyCheckoutCompletedAsync_ConcurrentDeliveries_LoserReportsAlreadyProcessedNotConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId, transactionId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, transactionId) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();
        }

        await using var dbA = CreateDbContext(dbName);
        await using var dbB = CreateDbContext(dbName);
        // Prime both contexts' change trackers with the pre-transition snapshot (RowVersion 1)
        // before either commits, so both attempt the transition from the same starting point —
        // exactly what two racing webhook deliveries would each see.
        await dbA.SalesOrders.SingleAsync(x => x.Id == orderId);
        await dbA.SalesPayments.SingleAsync(x => x.Id == paymentId);
        await dbA.SalesPaymentTransactions.SingleAsync(x => x.Id == transactionId);
        await dbB.SalesOrders.SingleAsync(x => x.Id == orderId);
        await dbB.SalesPayments.SingleAsync(x => x.Id == paymentId);
        await dbB.SalesPaymentTransactions.SingleAsync(x => x.Id == transactionId);

        var resultA = await new OnlineCheckoutPaymentConfirmationRepository(dbA).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);
        var resultB = await new OnlineCheckoutPaymentConfirmationRepository(dbB).ApplyCheckoutCompletedAsync(
            tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);

        Assert.True(resultA.Applied);
        Assert.False(resultB.Applied);
        Assert.True(resultB.Found);
        Assert.Null(resultB.AnomalyCode);
        Assert.True(resultB.ShouldNotify);

        await using var verifyDb = CreateDbContext(dbName);
        var payment = await verifyDb.SalesPayments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal("PAID", payment.PaymentStatus);
        Assert.Equal(100m, payment.PaidAmount);
        Assert.Equal(2, payment.RowVersion);
    }

    [Fact]
    public async Task ApplyCheckoutExpiredAsync_Success_CancelsOrderAndPayment()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateDbContext(dbName);
        var result = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutExpiredAsync(
            tenantId, orderId, paymentId, "Stripe checkout session expired.", Now, CancellationToken.None);

        Assert.True(result.Applied);
        Assert.True(result.ShouldNotify);
        var order = await db.SalesOrders.SingleAsync(x => x.Id == orderId);
        var payment = await db.SalesPayments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal("CANCELLED", order.Status);
        Assert.Equal("CANCELLED", payment.PaymentStatus);
    }

    [Fact]
    public async Task ApplyCheckoutExpiredAsync_ArrivingAfterAlreadyPaid_ReturnsAnomalyAndDoesNotCancel()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantId, orderId, paymentId;
        await using (var seedDb = CreateDbContext(dbName))
        {
            (tenantId, orderId, paymentId, _) = SeedPendingOnlinePayment(seedDb, 100m);
            await seedDb.SaveChangesAsync();

            var result = await new OnlineCheckoutPaymentConfirmationRepository(seedDb).ApplyCheckoutCompletedAsync(
                tenantId, orderId, paymentId, 100m, "LKR", "cs_test_1", "pi_ext_1", null, Now, CancellationToken.None);
            Assert.True(result.Applied);
        }

        await using var db = CreateDbContext(dbName);
        var expired = await new OnlineCheckoutPaymentConfirmationRepository(db).ApplyCheckoutExpiredAsync(
            tenantId, orderId, paymentId, "Late expiry webhook.", Now.AddMinutes(10), CancellationToken.None);

        Assert.Equal("payment_status_conflict", expired.AnomalyCode);
        Assert.False(expired.ShouldNotify);
        Assert.Equal("PAID", (await db.SalesPayments.SingleAsync(x => x.Id == paymentId)).PaymentStatus);
    }

    private static (Guid TenantId, Guid OrderId, Guid PaymentId, Guid TransactionId) SeedPendingOnlinePayment(
        EPosDbContext db,
        decimal amount,
        string currency = "LKR",
        string? providerSessionId = "cs_test_1")
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        var order = SalesOrder.CreateClickAndCollect(
            orderId, tenantId, "EC-1", "idem-1", Guid.NewGuid(), Guid.NewGuid(),
            "CLICK_AND_COLLECT", Guid.NewGuid(), "MAIN", "Main Store", Guid.NewGuid(), "Test Customer",
            "customer@example.com", "+94110000000", currency, false, amount, 0m, 0m, 0m, amount,
            Now.AddHours(2), Now.AddHours(3), "Asia/Colombo", Now);

        var payment = SalesPayment.CreatePendingOnlinePayment(
            paymentId, tenantId, orderId, "PAY-1", Guid.NewGuid(), currency, amount, "idem-1", Now);

        var transaction = SalesPaymentTransaction.CreatePendingProviderCharge(
            transactionId, tenantId, paymentId, amount, currency, "STRIPE", providerSessionId, "idem-1", Now);

        db.AddRange(order, payment, transaction);
        return (tenantId, orderId, paymentId, transactionId);
    }

    private static EPosDbContext CreateDbContext(string dbName) => new(
        new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
