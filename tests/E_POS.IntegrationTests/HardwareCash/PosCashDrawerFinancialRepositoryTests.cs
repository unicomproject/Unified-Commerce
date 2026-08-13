using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.HardwareCash;

public sealed class PosCashDrawerFinancialRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Summary_UsesAuthoritativeCashSourcesWithoutDoubleCountingOrNonCash()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 25000m);
        var cash = Guid.NewGuid(); var card = Guid.NewGuid();
        db.PaymentMethods.Add(PaymentMethod.Create(cash, ids.Tenant, "CASH", "Cash", "CASH", true, true, 1, "ACTIVE", ids.User, Now));
        db.PaymentMethods.Add(PaymentMethod.Create(card, ids.Tenant, "CARD", "Card", "CARD", true, false, 2, "ACTIVE", ids.User, Now));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CASH", cash, ids.Till, ids.Session, "LKR", 5000m, 5000m, 5000m, 0m, "idem-cash", "hash", ids.User, Now));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CARD", card, ids.Till, ids.Session, "LKR", 9000m, 9000m, 9000m, 0m, "idem-card", "hash", ids.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateCashIn(Guid.NewGuid(), ids.Tenant, ids.Session, 2000m, "LKR", "Float", "CIN-1", ids.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateCashOut(Guid.NewGuid(), ids.Tenant, ids.Session, 3000m, "LKR", "Petty cash", "COUT-1", ids.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateCashIn(Guid.NewGuid(), ids.Tenant, ids.Session, 5000m, "LKR", "Mirrored sale", "PAY-CASH", ids.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateManual(Guid.NewGuid(), ids.Tenant, ids.Session, ids.Device, Guid.NewGuid(), "CASH_DROP", 5000m, "LKR", "Safe drop", null, ids.User, Now));
        await db.SaveChangesAsync();

        var summary = await new PosDrawerRepository(db).GetFinancialSummaryAsync(ids.Tenant, ids.Session, default);

        Assert.NotNull(summary);
        Assert.Equal(5000m, summary!.CashSales);
        Assert.Equal(2000m, summary.CashIn);
        Assert.Equal(3000m, summary.CashOut);
        Assert.Equal(5000m, summary.CashDrops);
        Assert.Equal(24000m, summary.CurrentExpectedCash);
    }

    [Theory]
    [InlineData("CASH_IN", 27000)]
    [InlineData("CASH_OUT", 22000)]
    [InlineData("CASH_DROP", 20000)]
    public async Task ManualMovement_PersistsAndUpdatesExpectedCash(string type, decimal expected)
    {
        await using var db = CreateDb(); var ids = SeedOpenContext(db, 25000m); await db.SaveChangesAsync();
        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, ids.Session, type,
            type == "CASH_IN" ? 2000m : type == "CASH_OUT" ? 3000m : 5000m, "Approved reason", "REF-1");
        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now, default);
        var summary = await new PosDrawerRepository(db).GetFinancialSummaryAsync(ids.Tenant, ids.Session, default);
        Assert.Null(result.ErrorCode); Assert.NotNull(result.Movement); Assert.Equal(expected, summary!.CurrentExpectedCash);
        var saved = await db.TillCashMovements.SingleAsync();
        Assert.Equal(ids.Tenant, saved.TenantId); Assert.Equal(ids.Device, saved.PosDeviceId); Assert.Equal(request.RequestId, saved.RequestId);
        Assert.Equal(ids.User, saved.PerformedByTenantUserId); Assert.Equal("LKR", saved.CurrencyCode); Assert.Equal("Approved reason", saved.Reason);
    }

    [Fact]
    public async Task Movement_IsIdempotentAndConflictingPayloadIsRejected()
    {
        await using var db = CreateDb(); var ids = SeedOpenContext(db, 5000m); await db.SaveChangesAsync();
        var requestId = Guid.NewGuid(); var repo = new PosDrawerRepository(db);
        var request = new CreatePosCashMovementRequest(requestId, ids.Device, ids.Session, "CASH_IN", 1000m, "Float");
        var first = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now, default);
        var replay = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now.AddSeconds(1), default);
        var conflict = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request with { Amount = 2000m }, Now, default);
        Assert.Null(first.ErrorCode); Assert.Null(replay.ErrorCode); Assert.Equal(first.Movement!.MovementId, replay.Movement!.MovementId);
        Assert.Equal("cash_drawer.idempotency_conflict", conflict.ErrorCode); Assert.Single(db.TillCashMovements);
    }

    [Fact]
    public async Task CashOut_RejectsOverdrawAndAllowsExactBoundary()
    {
        await using var db = CreateDb(); var ids = SeedOpenContext(db, 5000m); await db.SaveChangesAsync(); var repo = new PosDrawerRepository(db);
        var over = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, ids.Session, "CASH_OUT", 6000m, "Over"), Now, default);
        Assert.Equal("cash_drawer.insufficient_expected_cash", over.ErrorCode); Assert.Empty(db.TillCashMovements);
        var exact = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, ids.Session, "CASH_OUT", 5000m, "Exact"), Now, default);
        Assert.Null(exact.ErrorCode); Assert.Equal(0m, (await repo.GetFinancialSummaryAsync(ids.Tenant, ids.Session, default))!.CurrentExpectedCash);
    }

    [Fact]
    public async Task History_IsTenantSessionScopedNewestFirstAndPaginated()
    {
        await using var db = CreateDb(); var ids = SeedOpenContext(db, 1000m);
        db.TillCashMovements.Add(TillCashMovement.CreateManual(Guid.NewGuid(), ids.Tenant, ids.Session, ids.Device, Guid.NewGuid(), "CASH_IN", 100m, "LKR", "First", null, ids.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateManual(Guid.NewGuid(), ids.Tenant, ids.Session, ids.Device, Guid.NewGuid(), "CASH_DROP", 50m, "LKR", "Second", null, ids.User, Now.AddMinutes(1)));
        db.TillCashMovements.Add(TillCashMovement.CreateManual(Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), ids.Device, Guid.NewGuid(), "CASH_OUT", 10m, "LKR", "Other session", null, ids.User, Now.AddMinutes(2)));
        await db.SaveChangesAsync();
        var page = await new PosDrawerRepository(db).GetFinancialMovementsAsync(ids.Tenant, ids.Session, 1, 1, default);
        Assert.Single(page.Items); Assert.Equal("CASH_DROP", page.Items[0].MovementType); Assert.Equal("OUT", page.Items[0].Direction);
        Assert.Equal(2, page.TotalCount); Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task History_ExcludesOtherTenantAndDoesNotInflateTotalWithMirroredPaymentRows()
    {
        await using var db = CreateDb();
        var tenantA = SeedOpenContext(db, 1000m, "A");
        var tenantB = SeedOpenContext(db, 1000m, "B");
        var cash = Guid.NewGuid();
        db.PaymentMethods.Add(PaymentMethod.Create(cash, tenantA.Tenant, "CASH", "Cash", "CASH", true, true, 1, "ACTIVE", tenantA.User, Now));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantA.Tenant, Guid.NewGuid(), "PAY-A", cash, tenantA.Till, tenantA.Session,
            "LKR", 2000m, 2000m, 2000m, 0m, "idem-a", "hash", tenantA.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateCashIn(
            Guid.NewGuid(), tenantA.Tenant, tenantA.Session, 2000m, "LKR", "Mirrored", "PAY-A", tenantA.User, Now));
        db.TillCashMovements.Add(TillCashMovement.CreateManual(
            Guid.NewGuid(), tenantA.Tenant, tenantA.Session, tenantA.Device, Guid.NewGuid(), "CASH_IN", 100m, "LKR",
            "Tenant A manual", null, tenantA.User, Now.AddMinutes(1)));
        db.TillCashMovements.Add(TillCashMovement.CreateManual(
            Guid.NewGuid(), tenantB.Tenant, tenantB.Session, tenantB.Device, Guid.NewGuid(), "CASH_IN", 999m, "LKR",
            "Tenant B manual", null, tenantB.User, Now.AddMinutes(2)));
        await db.SaveChangesAsync();

        var page = await new PosDrawerRepository(db).GetFinancialMovementsAsync(tenantA.Tenant, tenantA.Session, 1, 25, default);

        Assert.Equal(2, page.TotalCount);
        Assert.DoesNotContain(page.Items, x => x.Amount == 999m);
        Assert.DoesNotContain(page.Items, x => x.Reference == "PAY-A" && x.MovementType == "CASH_IN");
        Assert.Contains(page.Items, x => x.MovementType == "CASH_SALE" && x.Amount == 2000m);
        Assert.Contains(page.Items, x => x.MovementType == "CASH_IN" && x.Amount == 100m);
    }

    [Fact]
    public async Task Summary_IncludesOnlyFinalizedCashPaymentsAndExcludesFailedCancelledPendingCardQr()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 10000m);
        var cash = Guid.NewGuid(); var card = Guid.NewGuid(); var qr = Guid.NewGuid();
        db.PaymentMethods.Add(PaymentMethod.Create(cash, ids.Tenant, "CASH", "Cash", "CASH", true, true, 1, "ACTIVE", ids.User, Now));
        db.PaymentMethods.Add(PaymentMethod.Create(card, ids.Tenant, "CARD", "Card", "CARD", true, false, 2, "ACTIVE", ids.User, Now));
        db.PaymentMethods.Add(PaymentMethod.Create(qr, ids.Tenant, "QR", "QR", "QR", true, false, 3, "ACTIVE", ids.User, Now));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-OK", cash, ids.Till, ids.Session,
            "LKR", 1500m, 1500m, 1500m, 0m, "idem-ok", "hash", ids.User, Now));
        db.SalesPayments.Add(WithStatus(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-FAIL", cash, ids.Till, ids.Session,
            "LKR", 4000m, 4000m, 4000m, 0m, "idem-fail", "hash", ids.User, Now), "FAILED"));
        db.SalesPayments.Add(WithStatus(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CANCEL", cash, ids.Till, ids.Session,
            "LKR", 3000m, 3000m, 3000m, 0m, "idem-cancel", "hash", ids.User, Now), "CANCELLED"));
        db.SalesPayments.Add(WithStatus(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-PEND", cash, ids.Till, ids.Session,
            "LKR", 2000m, 2000m, 2000m, 0m, "idem-pend", "hash", ids.User, Now), "PENDING"));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CARD", card, ids.Till, ids.Session,
            "LKR", 5000m, 5000m, 5000m, 0m, "idem-card", "hash", ids.User, Now));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-QR", qr, ids.Till, ids.Session,
            "LKR", 2500m, 2500m, 2500m, 0m, "idem-qr", "hash", ids.User, Now));
        await db.SaveChangesAsync();

        var summary = await new PosDrawerRepository(db).GetFinancialSummaryAsync(ids.Tenant, ids.Session, default);

        Assert.NotNull(summary);
        Assert.Equal(1500m, summary!.CashSales);
        Assert.Equal(0m, summary.CashRefunds);
        Assert.Equal(11500m, summary.CurrentExpectedCash);
    }

    [Fact]
    public async Task Summary_MixedCashAndCardSplit_CountsOnlyCashPortion()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 10000m);
        var cash = Guid.NewGuid(); var card = Guid.NewGuid();
        db.PaymentMethods.Add(PaymentMethod.Create(cash, ids.Tenant, "CASH", "Cash", "CASH", true, true, 1, "ACTIVE", ids.User, Now));
        db.PaymentMethods.Add(PaymentMethod.Create(card, ids.Tenant, "CARD", "Card", "CARD", true, false, 2, "ACTIVE", ids.User, Now));
        var orderId = Guid.NewGuid();
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, orderId, "PAY-SPLIT-CASH", cash, ids.Till, ids.Session,
            "LKR", 1000m, 1000m, 1000m, 0m, "idem-split-cash", "hash", ids.User, Now));
        db.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, orderId, "PAY-SPLIT-CARD", card, ids.Till, ids.Session,
            "LKR", 2000m, 2000m, 2000m, 0m, "idem-split-card", "hash", ids.User, Now));
        await db.SaveChangesAsync();

        var summary = await new PosDrawerRepository(db).GetFinancialSummaryAsync(ids.Tenant, ids.Session, default);

        Assert.Equal(1000m, summary!.CashSales);
        Assert.Equal(11000m, summary.CurrentExpectedCash);
    }

    [Fact]
    public async Task Summary_AppliesCompletedCashRefundsIncludingPartialAndExcludesNonCashRefunds()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 10000m);
        var cash = Guid.NewGuid(); var card = Guid.NewGuid(); var qr = Guid.NewGuid();
        db.PaymentMethods.Add(PaymentMethod.Create(cash, ids.Tenant, "CASH", "Cash", "CASH", true, true, 1, "ACTIVE", ids.User, Now));
        db.PaymentMethods.Add(PaymentMethod.Create(card, ids.Tenant, "CARD", "Card", "CARD", true, false, 2, "ACTIVE", ids.User, Now));
        db.PaymentMethods.Add(PaymentMethod.Create(qr, ids.Tenant, "QR", "QR", "QR", true, false, 3, "ACTIVE", ids.User, Now));

        var cashPayment = SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CASH-REF", cash, ids.Till, ids.Session,
            "LKR", 5000m, 5000m, 5000m, 0m, "idem-cash-ref", "hash", ids.User, Now);
        cashPayment.RecordRefund(2000m, ids.User, Now.AddMinutes(1));
        db.SalesPayments.Add(cashPayment);

        var fullCashRefund = SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CASH-FULL", cash, ids.Till, ids.Session,
            "LKR", 500m, 500m, 500m, 0m, "idem-cash-full", "hash", ids.User, Now.AddMinutes(2));
        fullCashRefund.RecordRefund(500m, ids.User, Now.AddMinutes(3));
        db.SalesPayments.Add(fullCashRefund);

        var cardPayment = SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-CARD-REF", card, ids.Till, ids.Session,
            "LKR", 3000m, 3000m, 3000m, 0m, "idem-card-ref", "hash", ids.User, Now);
        cardPayment.RecordRefund(1000m, ids.User, Now.AddMinutes(1));
        db.SalesPayments.Add(cardPayment);

        var qrPayment = SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), ids.Tenant, Guid.NewGuid(), "PAY-QR-REF", qr, ids.Till, ids.Session,
            "LKR", 800m, 800m, 800m, 0m, "idem-qr-ref", "hash", ids.User, Now);
        qrPayment.RecordRefund(800m, ids.User, Now.AddMinutes(1));
        db.SalesPayments.Add(qrPayment);
        await db.SaveChangesAsync();

        var summary = await new PosDrawerRepository(db).GetFinancialSummaryAsync(ids.Tenant, ids.Session, default);

        Assert.Equal(5500m, summary!.CashSales);
        Assert.Equal(2500m, summary.CashRefunds);
        Assert.Equal(13000m, summary.CurrentExpectedCash);
    }

    [Fact]
    public async Task SameRequestId_AcrossTenants_CreatesIndependentMovements()
    {
        await using var db = CreateDb();
        var tenantA = SeedOpenContext(db, 5000m, "A");
        var tenantB = SeedOpenContext(db, 5000m, "B");
        await db.SaveChangesAsync();
        var sharedRequestId = Guid.NewGuid();
        var repo = new PosDrawerRepository(db);

        var first = await repo.CreateFinancialMovementAsync(
            tenantA.Tenant, tenantA.User, tenantA.Till,
            new(sharedRequestId, tenantA.Device, tenantA.Session, "CASH_IN", 1000m, "Tenant A"), Now, default);
        var second = await repo.CreateFinancialMovementAsync(
            tenantB.Tenant, tenantB.User, tenantB.Till,
            new(sharedRequestId, tenantB.Device, tenantB.Session, "CASH_IN", 1000m, "Tenant B"), Now, default);

        Assert.Null(first.ErrorCode);
        Assert.Null(second.ErrorCode);
        Assert.NotEqual(first.Movement!.MovementId, second.Movement!.MovementId);
        Assert.Equal(2, await db.TillCashMovements.CountAsync(x => x.RequestId == sharedRequestId));
    }

    [Fact]
    public async Task DistinctRequestIds_SameTenant_PersistTwoMovements()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        await db.SaveChangesAsync();
        var repo = new PosDrawerRepository(db);

        var first = await repo.CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, ids.Session, "CASH_IN", 1000m, "First"), Now, default);
        var second = await repo.CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, ids.Session, "CASH_IN", 1000m, "Second"), Now, default);

        Assert.Null(first.ErrorCode);
        Assert.Null(second.ErrorCode);
        Assert.Equal(2, await db.TillCashMovements.CountAsync());
    }

    [Fact]
    public async Task CreateMovement_ClosedTill_IsRejectedWithoutPersisting()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        await db.SaveChangesAsync();
        var session = await db.TillSessions.SingleAsync(x => x.Id == ids.Session);
        session.Close(ids.User, ids.Device, "Closed for test", Now.AddHours(1));
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, ids.Session, "CASH_IN", 1000m, "Should fail"), Now, default);

        Assert.Equal("cash_drawer.till_session_not_open", result.ErrorCode);
        Assert.Empty(db.TillCashMovements);
    }

    private static SalesPayment WithStatus(SalesPayment payment, string status)
    {
        typeof(SalesPayment).GetProperty(nameof(SalesPayment.PaymentStatus))!.SetValue(payment, status);
        return payment;
    }

    private static EPosDbContext CreateDb() => new(new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static (Guid Tenant, Guid Outlet, Guid Till, Guid Device, Guid User, Guid Session) SeedOpenContext(
        EPosDbContext db, decimal opening, string? suffix = null)
    {
        var tag = suffix ?? Guid.NewGuid().ToString("N")[..8];
        var tenant = Guid.NewGuid(); var outlet = Guid.NewGuid(); var till = Guid.NewGuid(); var device = Guid.NewGuid(); var user = Guid.NewGuid(); var session = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(tenant, $"T-{tag}", $"t-{tag}", "Tenant", "active", "LKR", "UTC", null, null, Now));
        db.Outlets.Add(Outlet.Create(outlet, tenant, "Outlet", $"OUT-{tag}", "ACTIVE", "STORE", "UTC", true, null, null, null, Now));
        db.Tills.Add(Till.Create(till, tenant, outlet, "Till", "Till", 1, $"TILL-{tag}", "STANDARD", 0m, "LKR", true, "ACTIVE", null, Now));
        db.PosDevices.Add(PosDevice.Create(device, tenant, outlet, $"POS-{tag}", "POS", "TABLET", "ACTIVE", null, Now));
        db.TenantUsers.Add(TenantUser.Create(
            user,
            tenant,
            $"cashier-{tag}@test.com",
            "Cashier",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "cashier",
            "outlet",
            "default",
            Now,
            staffCode: $"USR-2026-{user:N}"));
        db.TillSessions.Add(TillSession.Open(session, tenant, outlet, till, $"TS-{tag}", DateOnly.FromDateTime(Now.UtcDateTime), user, device, opening, "LKR", null, Now));
        return (tenant, outlet, till, device, user, session);
    }
}
