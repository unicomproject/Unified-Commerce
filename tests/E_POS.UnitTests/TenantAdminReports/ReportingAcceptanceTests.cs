using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Shared.Refund.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OfflineSync.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace E_POS.UnitTests.TenantAdminReports;

/// <summary>
/// OneVerz Reporting Release 1 acceptance criteria AC-01..AC-16 against persisted EF data and the
/// real reporting repository/service. The business timezone is Asia/Colombo (UTC+05:30).
/// </summary>
public sealed class ReportingAcceptanceTests
{
    private static readonly DateOnly Day = new(2026, 9, 20);
    // 2026-09-20 12:00 local
    private static readonly DateTimeOffset Noon = new(2026, 9, 20, 6, 30, 0, TimeSpan.Zero);

    private static T Entity<T>(params (string Name, object? Value)[] values) where T : class
    {
        // Run the (EF) parameterless constructor so string initialisers apply; fall back for types without one.
        var entity = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) is { } ctor
            ? (T)ctor.Invoke(null)
            : (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        Set(entity, "CreatedAt", Noon.AddDays(-30));
        Set(entity, "UpdatedAt", Noon.AddDays(-30));
        foreach (var (name, value) in values) Set(entity, name, value);
        return entity;
    }

    private static void Set(object entity, string name, object? value)
    {
        for (var type = entity.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property == null) continue;
            property.SetValue(entity, value);
            return;
        }
        throw new InvalidOperationException($"Unknown {entity.GetType().Name}.{name}");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public readonly Guid Tenant = Guid.NewGuid(), User = Guid.NewGuid(), Outlet = Guid.NewGuid(), OtherOutlet = Guid.NewGuid();
        public readonly Guid Channel = Guid.NewGuid(), Cash = Guid.NewGuid(), Card = Guid.NewGuid(), Till = Guid.NewGuid(), OtherTill = Guid.NewGuid();
        public readonly Guid Location = Guid.NewGuid();
        public readonly EPosDbContext Db = new(new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        private int _sequence;

        public TenantRequestContext Context(params string[] permissions) => new(Tenant, User, permissions);
        public TenantAdminReportsRepository Repository => new(Db);

        public static ReportQueryRequest Query(string section, DateOnly? from = null, DateOnly? to = null) =>
            new(from ?? Day, to ?? from ?? Day, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, section, 1, 100);

        public TenantAdminReportsService Service(bool entitled = true)
        {
            var entitlements = new Mock<ITenantFeatureEntitlementEvaluator>();
            entitlements.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entitled);
            var clock = new Mock<IDateTimeProvider>();
            clock.SetupGet(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
            return new TenantAdminReportsService(Repository, entitlements.Object, clock.Object, new Mock<ITenantAdminReportsAuditLogger>().Object);
        }

        public async Task SeedAsync()
        {
            Db.Tenants.Add(Entity<Tenant>(("Id", Tenant), ("DefaultTimezone", "Asia/Colombo"), ("BaseCurrencyCode", "LKR")));
            Db.TenantUsers.Add(Entity<TenantUser>(("Id", User), ("TenantId", Tenant), ("AccountStatus", "ACTIVE"), ("FullName", "Owner"),
                ("StaffCode", "OWN"), ("OutletAccessScope", "ALL_OUTLETS"), ("TillAccessScope", "ALL_ACCESSIBLE_TILLS")));
            foreach (var (id, name) in new[] { (Outlet, "Colombo"), (OtherOutlet, "Kandy") })
                Db.Outlets.Add(Entity<Outlet>(("Id", id), ("TenantId", Tenant), ("OutletName", name), ("OutletCode", name), ("Status", "ACTIVE")));
            foreach (var (id, outlet) in new[] { (Till, Outlet), (OtherTill, OtherOutlet) })
                Db.Tills.Add(Entity<Till>(("Id", id), ("TenantId", Tenant), ("OutletId", outlet), ("TillCode", "T-" + id.ToString()[..4]),
                    ("TillName", "Till"), ("TillType", "STANDARD"), ("Status", "ACTIVE")));
            var platformChannel = Guid.NewGuid();
            Db.PlatformSalesChannels.Add(Entity<PlatformSalesChannel>(("Id", platformChannel), ("ChannelCode", "POS"), ("DefaultName", "Point of Sale"), ("ChannelType", "POS")));
            Db.SalesChannels.Add(Entity<SalesChannel>(("Id", Channel), ("TenantId", Tenant), ("PlatformSalesChannelId", platformChannel), ("CustomName", "POS"), ("Status", "ACTIVE")));
            Db.PaymentMethods.Add(Entity<PaymentMethod>(("Id", Cash), ("TenantId", Tenant), ("MethodCode", "CASH"), ("MethodName", "Cash"), ("MethodType", "CASH"), ("Status", "ACTIVE")));
            Db.PaymentMethods.Add(Entity<PaymentMethod>(("Id", Card), ("TenantId", Tenant), ("MethodCode", "CARD"), ("MethodName", "Card"), ("MethodType", "CARD"), ("Status", "ACTIVE")));
            Db.InventoryLocations.Add(Entity<InventoryLocation>(("Id", Location), ("TenantId", Tenant), ("OutletId", Outlet), ("LocationName", "Store"), ("LocationCode", "L1"), ("Status", "ACTIVE")));
            await Db.SaveChangesAsync();
        }

        public Guid Order(decimal total, decimal tax, DateTimeOffset? postedAt = null, Guid? outlet = null, string orderType = "POS_SALE",
            string status = "COMPLETED", string paymentStatus = "PAID", string fulfilment = "FULFILLED", Guid? till = null, Guid? session = null)
        {
            var id = Guid.NewGuid();
            var at = postedAt ?? Noon;
            Db.SalesOrders.Add(Entity<SalesOrder>(("Id", id), ("TenantId", Tenant), ("OrderNumber", $"S-{++_sequence:D4}"),
                ("ReportingOutletId", outlet ?? Outlet), ("ReportingOutletNameSnapshot", "Colombo"), ("SalesChannelId", Channel),
                ("OrderType", orderType), ("Status", status), ("PaymentStatus", paymentStatus), ("FulfillmentStatus", fulfilment),
                ("BusinessDate", DateOnly.FromDateTime(at.ToOffset(TimeSpan.FromMinutes(330)).DateTime)), ("PlacedAt", at),
                ("CompletedAt", status == "COMPLETED" ? at : null), ("TotalAmount", total), ("SubtotalAmount", total - tax), ("TaxAmount", tax),
                ("CurrencyCode", "LKR"), ("TillId", till), ("TillSessionId", session), ("CreatedByTenantUserId", User)));
            return id;
        }

        public Guid Line(Guid order, decimal quantity, decimal total, decimal tax, string name = "Tea", Guid? product = null, Guid? variant = null)
        {
            var id = Guid.NewGuid();
            Db.SalesOrderLines.Add(Entity<SalesOrderLine>(("Id", id), ("TenantId", Tenant), ("SalesOrderId", order), ("LineNumber", 1),
                ("ProductId", product ?? Guid.NewGuid()), ("ProductVariantId", variant), ("UomId", Guid.Empty), ("ProductNameSnapshot", name),
                ("SkuSnapshot", "SKU-" + name), ("UomCodeSnapshot", "EA"), ("UomNameSnapshot", "Each"), ("Quantity", quantity),
                ("LineTotalAmount", total), ("LineTaxAmount", tax), ("LineSubtotalAmount", total - tax), ("UnitPrice", total / quantity),
                ("ProductTypeSnapshot", "STANDARD"), ("ProductStructureSnapshot", "STANDALONE"), ("LineStatus", "COMPLETED")));
            return id;
        }

        public Guid Payment(Guid order, Guid method, decimal paid, string status = "PAID", decimal change = 0m, DateTimeOffset? at = null,
            Guid? session = null, string? key = null)
        {
            var id = Guid.NewGuid();
            Db.SalesPayments.Add(Entity<SalesPayment>(("Id", id), ("TenantId", Tenant), ("SalesOrderId", order), ("PaymentMethodId", method),
                ("PaymentNumber", $"P-{++_sequence:D4}"), ("PaymentStatus", status), ("RequestedAmount", paid), ("PaidAmount", status == "PAID" ? paid : 0m),
                ("TenderedAmount", paid + change), ("ChangeAmount", change), ("CurrencyCode", "LKR"), ("InitiatedAt", at ?? Noon),
                ("PaidAt", status == "PAID" ? at ?? Noon : null), ("TillSessionId", session), ("ExternalReference", "PSP-000012345678"),
                ("IdempotencyKey", key ?? Guid.NewGuid().ToString())));
            return id;
        }

        public Guid Return(Guid order, Guid line, decimal subtotal, decimal tax, DateTimeOffset at, string disposition = "RESTOCKED")
        {
            var id = Guid.NewGuid();
            Db.SalesReturns.Add(SalesReturn.CreateCompleted(id, Tenant, order, null, Outlet, Guid.NewGuid(), $"R-{++_sequence:D4}", 1m,
                subtotal + tax, null, "return-" + id, User, at));
            var returnLine = SalesReturnLine.CreateReceived(Guid.NewGuid(), Tenant, id, line, Guid.NewGuid(), 1m, subtotal, tax, subtotal, tax, false, null, at);
            Set(returnLine, "DispositionStatus", disposition);
            Db.SalesReturnLines.Add(returnLine);
            return id;
        }

        public Guid Product(string name)
        {
            var id = Guid.NewGuid();
            Db.Products.Add(Entity<Product>(("Id", id), ("TenantId", Tenant), ("ProductName", name), ("ProductCode", name), ("Status", "ACTIVE")));
            return id;
        }

        public void Movement(Guid balance, string type, decimal change, DateTimeOffset at) =>
            Db.StockMovements.Add(Entity<StockMovement>(("Id", Guid.NewGuid()), ("TenantId", Tenant), ("InventoryBalanceId", balance),
                ("MovementNumber", $"M-{++_sequence:D4}"), ("MovementType", type), ("QuantityChange", change), ("OccurredAt", at)));

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private static decimal D(IReadOnlyDictionary<string, object?> row, string key) => Convert.ToDecimal(row[key]);

    [Fact]
    public async Task AC01_SaleWithStoredTax_ReportsExTaxTaxTotalAndOneSale()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        f.Order(110m, 10m); await f.Db.SaveChangesAsync();

        var result = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);

        Assert.Equal("REP-01A", result.ReportId);
        Assert.Equal(100m, D(result.Summary, "salesExcludingTax"));
        Assert.Equal(10m, D(result.Summary, "netTax"));
        Assert.Equal(110m, D(result.Summary, "netSalesIncludingTax"));
        Assert.Equal(1, result.Summary["completedSaleCount"]);
        Assert.Equal(110m, D(result.Summary, "averageSaleValue"));
        var row = Assert.Single(result.Records);
        Assert.Equal("SALE", row["rowType"]);
        Assert.Equal(110m, D(row, "totalAmount"));
    }

    [Fact]
    public async Task AC02_SplitTender_IsOneSaleWithTwoPaymentRowsAndPerMethodAllocation()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var order = f.Order(110m, 10m);
        f.Payment(order, f.Cash, 40m); f.Payment(order, f.Card, 70m);
        await f.Db.SaveChangesAsync();

        var sales = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        var events = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payment-transactions"), default);
        var methods = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payments"), default);

        Assert.Equal(1, sales.Summary["completedSaleCount"]);
        Assert.Equal(2, events.Records.Count);
        Assert.All(events.Records, x => Assert.Equal(order, x["orderId"]));
        Assert.Equal(110m, D(events.Summary, "successfulReceipts"));
        Assert.Equal(40m, D(methods.Records.Single(x => (string)x["paymentMethodCode"]! == "CASH"), "successfulReceipts"));
        Assert.Equal(70m, D(methods.Records.Single(x => (string)x["paymentMethodCode"]! == "CARD"), "successfulReceipts"));
        Assert.All(events.Records, x => Assert.Equal("************5678", x["maskedReference"]));
    }

    [Fact]
    public async Task AC03_CashTenderWithChange_CountsAllocatedAmountOnly()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        f.Payment(f.Order(30m, 0m), f.Cash, 30m, change: 20m); await f.Db.SaveChangesAsync();

        var methods = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payments"), default);
        var events = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payment-transactions"), default);

        var cash = Assert.Single(methods.Records);
        Assert.Equal(30m, D(cash, "successfulReceipts"));
        Assert.Equal(30m, D(methods.Summary, "netReceipts"));
        var row = Assert.Single(events.Records);
        Assert.Equal(50m, D(row, "tenderedAmount"));
        Assert.Equal(20m, D(row, "changeAmount"));
        Assert.Equal(30m, D(row, "signedAmount"));
    }

    [Fact]
    public async Task AC04_PaidOnlineOrderCollectedLater_IsOneSaleOneReceipt_CollectionOnlyChangesFulfilment()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var collectedAt = Noon.AddDays(2);
        var order = f.Order(50m, 5m, Noon, orderType: "CLICK_AND_COLLECT", status: "COMPLETED", fulfilment: "COLLECTED");
        Set(f.Db.SalesOrders.Local.Single(), "CompletedAt", collectedAt); // collection completes the order two days later
        f.Payment(order, f.Card, 50m, at: Noon);
        await f.Db.SaveChangesAsync();

        var paidDay = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        var collectedDay = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions", Day.AddDays(2)), default);
        var receiptsPaidDay = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payments"), default);
        var receiptsCollectedDay = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payments", Day.AddDays(2)), default);
        var online = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("online"), default);
        var workload = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("collections"), default);

        Assert.Equal(1, paidDay.Summary["completedSaleCount"]);
        Assert.Equal(0, collectedDay.Summary["completedSaleCount"]);
        Assert.Equal(50m, D(receiptsPaidDay.Summary, "successfulReceipts"));
        Assert.Equal(0m, D(receiptsCollectedDay.Summary, "successfulReceipts"));
        var row = Assert.Single(online.Records);
        Assert.Equal("COLLECTED", row["fulfilmentStatus"]);
        Assert.Equal(collectedAt, row["collectedAt"]);
        Assert.Equal(0m, D(row, "outstandingAmount"));
        Assert.Empty(workload.Records);
        Assert.Empty(f.Db.StockMovements);
    }

    [Fact]
    public async Task AC05_UnpaidOnlineOrder_IsOutstandingAndExcludedFromSalesAndReceipts()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var order = f.Order(80m, 8m, orderType: "CLICK_AND_COLLECT", status: "CONFIRMED", paymentStatus: "UNPAID", fulfilment: "PENDING");
        f.Payment(order, f.Card, 80m, status: "PENDING");
        await f.Db.SaveChangesAsync();

        var sales = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        var events = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payment-transactions"), default);
        var online = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("online"), default);
        var workload = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("collections", new DateOnly(2020, 1, 1)), default);

        Assert.Equal(0, sales.Summary["completedSaleCount"]);
        Assert.Null(sales.Summary["averageSaleValue"]);
        Assert.Equal(0m, D(events.Summary, "successfulReceipts"));
        var pending = Assert.Single(events.Records);
        Assert.Equal("PENDING", pending["outcome"]);
        Assert.Equal(0m, D(pending, "signedAmount"));
        Assert.Equal(80m, D(online.Summary, "outstandingAmount"));
        Assert.Equal(80m, D(Assert.Single(workload.Records), "outstandingAmount"));
        Assert.Equal("ALL DATES / CURRENT WORKLOAD", workload.Summary["dateBasis"]);
        Assert.Null(workload.From);
    }

    [Fact]
    public async Task AC06_ReturnReversesSalesOnReturnDate_PendingRefundContributesZeroUntilCompleted()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var order = f.Order(110m, 10m, Noon.AddDays(-5));
        var line = f.Line(order, 5m, 110m, 10m);
        var payment = f.Payment(order, f.Cash, 110m, at: Noon.AddDays(-5));
        var returnId = f.Return(order, line, 20m, 2m, Noon);
        var refund = SalesRefund.CreateCompleted(Guid.NewGuid(), f.Tenant, order, returnId, "RF-1", "ORIGINAL", "LKR", 22m, "Return", f.User, Noon);
        Set(refund, "RefundStatus", "PENDING");
        Set(refund, "CompletedAt", null);
        f.Db.SalesRefunds.Add(refund);
        f.Db.SalesRefundPaymentAllocations.Add(SalesRefundPaymentAllocation.CreateCompleted(Guid.NewGuid(), f.Tenant, refund.Id, payment, f.Cash, 22m, null, Noon));
        await f.Db.SaveChangesAsync();

        var sales = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        Assert.Equal(20m, D(sales.Summary, "completedReturnsExcludingTax"));
        Assert.Equal(-20m, D(sales.Summary, "netSalesExcludingTax"));
        Assert.Equal(-2m, D(sales.Summary, "netTax"));
        var returnRow = Assert.Single(sales.Records, x => (string)x["rowType"]! == "RETURN");
        Assert.Equal(order, returnRow["originalOrderId"]);

        var returns = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("returns"), default);
        Assert.Equal(0m, D(returns.Summary, "successfullyRefundedAmount"));
        Assert.Equal(22m, D(returns.Summary, "pendingRefundAmount"));
        Assert.Equal(22m, D(returns.Summary, "completedReturnValue"));
        Assert.Equal("PENDING", Assert.Single(returns.Records)["refundOutcome"]);
        var payments = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payments"), default);
        Assert.Equal(0m, D(payments.Summary, "successfulRefunds"));

        Set(refund, "RefundStatus", "COMPLETED");
        Set(refund, "CompletedAt", Noon);
        Set(refund, "RefundedAmount", 22m);
        await f.Db.SaveChangesAsync();
        var afterRefund = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payments"), default);
        Assert.Equal(22m, D(afterRefund.Summary, "successfulRefunds"));
        Assert.Equal(-22m, D(afterRefund.Summary, "netReceipts"));
    }

    [Fact]
    public async Task AC06_FailedRefund_KeepsReturnVisibleWithZeroRefund()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var order = f.Order(110m, 10m, Noon.AddDays(-5));
        var returnId = f.Return(order, f.Line(order, 5m, 110m, 10m), 20m, 2m, Noon);
        var refund = SalesRefund.CreateCompleted(Guid.NewGuid(), f.Tenant, order, returnId, "RF-2", "ORIGINAL", "LKR", 22m, "Return", f.User, Noon);
        Set(refund, "RefundStatus", "FAILED");
        f.Db.SalesRefunds.Add(refund);
        await f.Db.SaveChangesAsync();

        var returns = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("returns"), default);

        var row = Assert.Single(returns.Records);
        Assert.Equal("FAILED", row["refundOutcome"]);
        Assert.Equal(0m, D(row, "refundAmount"));
        Assert.Equal(22m, D(returns.Summary, "failedRefundAmount"));
        Assert.Equal(0m, D(returns.Summary, "successfullyRefundedAmount"));
    }

    private static async Task<(Guid Session, CashReconciliation Reconciliation)> SeedClosedSessionAsync(Fixture f, DateTimeOffset closed)
    {
        var session = Guid.NewGuid();
        f.Db.TillSessions.Add(Entity<TillSession>(("Id", session), ("TenantId", f.Tenant), ("OutletId", f.Outlet), ("TillId", f.Till),
            ("SessionNumber", "S-1"), ("BusinessDate", Day), ("OpenedByTenantUserId", f.User), ("ClosedByTenantUserId", f.User),
            ("OpeningFloatAmount", 100m), ("Status", "CLOSED"), ("OpenedAt", closed.AddHours(-9)), ("ClosedAt", closed), ("CurrencyCode", "LKR")));
        // Stored exactly as PosTillSessionRepository serialises its ExpectedCashCalculation (refunds are netted into ExpectedCash).
        var reconciliation = CashReconciliation.Create(Guid.NewGuid(), f.Tenant, session, "REC-S-1", 340m, 335m, -5m, "LKR", "SHORTAGE",
            "{\"Version\":1,\"CurrencyCode\":\"LKR\",\"OpeningFloat\":100,\"CashPayments\":300,\"CashIn\":10,\"CashOut\":50,\"OpeningAdjustments\":0,\"ClosingRemovals\":0,\"ExpectedCash\":340}",
            closed);
        reconciliation.Submit(f.User, closed);
        f.Db.CashReconciliations.Add(reconciliation);
        await f.Db.SaveChangesAsync();
        return (session, reconciliation);
    }

    [Fact]
    public async Task AC07_TillClosing_ExpectedCashFormulaAndDifferenceWithReason()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        await SeedClosedSessionAsync(f, Noon.AddHours(8));

        var result = await f.Repository.GetOutletsAsync(f.Context(), Fixture.Query("tills"), default);

        var row = Assert.Single(result.Records);
        Assert.Equal(100m, D(row, "openingFloat"));
        Assert.Equal(300m, D(row, "cashReceipts"));
        Assert.Equal(20m, D(row, "cashRefunds"));
        Assert.Equal(10m, D(row, "otherCashIn"));
        Assert.Equal(50m, D(row, "otherCashOut"));
        Assert.Equal(340m, D(row, "expectedCashAmount"));
        Assert.Equal(340m, ReportRules.CalculateExpectedCash(100m, 300m, 20m, 10m, 50m));
        Assert.Equal(335m, D(row, "countedCashAmount"));
        Assert.Equal(-5m, D(row, "cashDifference"));
        Assert.Equal(-5m, ReportRules.CalculateCashDifference(335m, 340m));
        Assert.Equal("SHORTAGE", row["varianceReason"]);
        Assert.Equal("SUBMITTED", row["reviewStatus"]);
    }

    [Fact]
    public async Task AC08_StockLedger_IncludesPosSaleIssues_AndReconcilesOpeningToClosing()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var product = f.Product("Rice");
        var balance = Guid.NewGuid();
        f.Db.InventoryBalances.Add(Entity<InventoryBalance>(("Id", balance), ("TenantId", f.Tenant), ("InventoryLocationId", f.Location),
            ("ProductId", product), ("OnHandQuantity", 24m), ("AvailableQuantity", 24m)));
        var start = new DateTimeOffset(2026, 9, 19, 18, 30, 0, TimeSpan.Zero); // local midnight of the 20th
        f.Movement(balance, "STOCK_IN", 20m, start.AddMinutes(-1));   // opening
        f.Movement(balance, "STOCK_IN", 10m, start.AddHours(1));      // receipt
        f.Movement(balance, "SALE", -3m, start.AddHours(2));          // POS checkout issue
        f.Movement(balance, "STOCK_OUT", -2m, start.AddHours(3));     // manual issue
        f.Movement(balance, "RETURN", 1m, start.AddHours(4));         // restocked return
        f.Movement(balance, "ADJUSTMENT", -2m, start.AddHours(5));    // signed adjustment
        f.Movement(balance, "TRANSFER", 4m, start.AddHours(6));       // transfer in
        f.Movement(balance, "TRANSFER", -4m, start.AddHours(7));      // transfer out
        f.Movement(balance, "STOCK_IN", 100m, start.AddDays(1));      // after period
        await f.Db.SaveChangesAsync();

        var result = await f.Repository.GetStockAsync(f.Context(), Fixture.Query("movements"), default);

        Assert.Equal(20m, D(result.Summary, "openingQuantity"));
        Assert.Equal(10m, D(result.Summary, "receipts"));
        Assert.Equal(5m, D(result.Summary, "stockIssues"));
        Assert.Equal(1m, D(result.Summary, "restockableReturns"));
        Assert.Equal(-2m, D(result.Summary, "signedAdjustments"));
        Assert.Equal(4m, D(result.Summary, "transferIn"));
        Assert.Equal(4m, D(result.Summary, "transferOut"));
        Assert.Equal(24m, D(result.Summary, "closingQuantity"));
        Assert.Equal(7, result.Records.Count);
        var perVariant = Assert.Single((IReadOnlyList<IReadOnlyDictionary<string, object?>>)result.Sections["reconciliationByVariant"]!);
        Assert.Equal(24m, D(perVariant, "closingQuantity"));

        // Row filters narrow detail rows but never break the reconciliation.
        var salesOnly = await f.Repository.GetStockAsync(f.Context(), Fixture.Query("movements") with { MovementType = "SALE" }, default);
        Assert.Single(salesOnly.Records);
        Assert.Equal(24m, D(salesOnly.Summary, "closingQuantity"));
    }

    [Fact]
    public async Task AC09_PersistentEventIdentifiers_AreUniqueInTheModel_AndEachPersistedEventCountsOnce()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        static bool HasUniqueIdempotency(EPosDbContext db, Type type) =>
            db.Model.FindEntityType(type)!.GetIndexes().Any(i => i.IsUnique
                && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "IdempotencyKey" }));
        Assert.True(HasUniqueIdempotency(f.Db, typeof(SalesPayment)));
        Assert.True(HasUniqueIdempotency(f.Db, typeof(StockMovement)));
        Assert.True(HasUniqueIdempotency(f.Db, typeof(SalesReturn)));
        Assert.Contains(f.Db.Model.FindEntityType(typeof(SalesOrder))!.GetIndexes(), i => i.IsUnique
            && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "OrderNumber" }));

        // A payment referenced by two refund allocations and a resent callback key still reports one receipt.
        var order = f.Order(100m, 0m);
        var payment = f.Payment(order, f.Card, 100m, key: "psp-callback-1");
        for (var i = 0; i < 2; i++)
        {
            var refund = SalesRefund.CreateCompleted(Guid.NewGuid(), f.Tenant, order, Guid.NewGuid(), $"RF-{i}", "ORIGINAL", "LKR", 10m, "x", f.User, Noon);
            f.Db.SalesRefunds.Add(refund);
            f.Db.SalesRefundPaymentAllocations.Add(SalesRefundPaymentAllocation.CreateCompleted(Guid.NewGuid(), f.Tenant, refund.Id, payment, f.Card, 10m, null, Noon));
        }
        await f.Db.SaveChangesAsync();
        var events = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("payment-transactions"), default);
        Assert.Single(events.Records, x => (string)x["eventType"]! == "PAYMENT");
        Assert.Equal(100m, D(events.Summary, "successfulReceipts"));
        Assert.Equal(20m, D(events.Summary, "successfulRefunds"));
        var sales = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        Assert.Equal(1, sales.Summary["completedSaleCount"]);
    }

    [Fact]
    public async Task AC10_BusinessTimezoneBoundaries_AndSessionCrossingMidnightKeepsAllActivity()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        f.Order(10m, 0m, new DateTimeOffset(2026, 9, 19, 18, 15, 0, TimeSpan.Zero)); // 19th 23:45 local
        f.Order(20m, 0m, new DateTimeOffset(2026, 9, 19, 18, 45, 0, TimeSpan.Zero)); // 20th 00:15 local
        f.Order(40m, 0m, new DateTimeOffset(2026, 9, 20, 18, 20, 0, TimeSpan.Zero)); // 20th 23:50 local
        var session = Guid.NewGuid();
        var opened = new DateTimeOffset(2026, 9, 20, 16, 30, 0, TimeSpan.Zero); // 20th 22:00 local
        f.Db.TillSessions.Add(Entity<TillSession>(("Id", session), ("TenantId", f.Tenant), ("OutletId", f.Outlet), ("TillId", f.Till),
            ("SessionNumber", "S-9"), ("BusinessDate", Day), ("OpenedByTenantUserId", f.User), ("OpeningFloatAmount", 50m),
            ("Status", "OPEN"), ("OpenedAt", opened), ("CurrencyCode", "LKR")));
        var afterMidnight = f.Order(15m, 0m, opened.AddHours(3), till: f.Till, session: session); // 21st 01:00 local
        f.Payment(afterMidnight, f.Cash, 15m, at: opened.AddHours(3), session: session);
        await f.Db.SaveChangesAsync();

        var day20 = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        var day19 = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions", Day.AddDays(-1)), default);
        var tills = await f.Repository.GetOutletsAsync(f.Context(), Fixture.Query("tills"), default);

        Assert.Equal(60m, D(day20.Summary, "salesExcludingTax"));
        Assert.Equal(2, day20.Summary["completedSaleCount"]);
        Assert.Equal(10m, D(day19.Summary, "salesExcludingTax"));
        var row = Assert.Single(tills.Records);
        Assert.Equal(15m, D(row, "cashReceipts"));
        Assert.Equal(65m, D(row, "expectedCashAmount"));
    }

    [Fact]
    public async Task AC11_ChangingProductAndTaxConfigurationAfterSale_DoesNotRewriteHistory()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var product = f.Product("Green Tea");
        var order = f.Order(110m, 10m);
        var line = f.Line(order, 1m, 110m, 10m, "Green Tea", product);
        f.Db.SalesOrderTaxes.Add(Entity<SalesOrderTax>(("Id", Guid.NewGuid()), ("TenantId", f.Tenant), ("SalesOrderId", order), ("SalesOrderLineId", line),
            ("TaxRateCodeSnapshot", "VAT10"), ("TaxTreatmentSnapshot", "TAXABLE"), ("TaxNameSnapshot", "VAT"), ("TaxRatePercent", 10m),
            ("TaxableAmount", 100m), ("TaxAmount", 10m)));
        await f.Db.SaveChangesAsync();

        Set(f.Db.Products.Local.Single(), "ProductName", "Renamed Tea");
        await f.Db.SaveChangesAsync();

        var products = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("products"), default);
        var tax = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("tax"), default);

        Assert.Equal("Green Tea", Assert.Single(products.Records)["productName"]);
        var taxRow = Assert.Single(tax.Records);
        Assert.Equal("VAT10", taxRow["taxCode"]);
        Assert.Equal(10m, D(taxRow, "taxRate"));
        Assert.Equal(100m, D(taxRow, "taxableAmount"));
        Assert.Equal(10m, D(taxRow, "netTaxAmount"));
    }

    [Fact]
    public async Task AC12_ExportFromLoadedFirstPage_ContainsEveryFilteredRowAndSameSnapshotTotals()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        for (var i = 0; i < 60; i++) f.Order(10m, 0m, Noon.AddMinutes(i));
        await f.Db.SaveChangesAsync();
        var service = f.Service();
        var context = f.Context(TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
        var screenQuery = Fixture.Query("transactions") with { PageSize = 25 };

        var screen = await service.GetSalesAsync(context, screenQuery, default);
        Assert.True(screen.IsSuccess, screen.Error?.Message);
        Assert.Equal(25, screen.Value!.Records.Count);
        for (var i = 0; i < 5; i++) f.Order(10m, 0m, Noon.AddMinutes(90 + i)); // arrives after the screen was captured
        await f.Db.SaveChangesAsync();

        var export = await service.CreateExportAsync(context,
            new ReportExportRequest("sales", "transactions", "csv", screenQuery with { SnapshotId = screen.Value.SnapshotId }), default);
        Assert.True(export.IsSuccess, export.Error?.Message);
        var csv = Encoding.UTF8.GetString((await service.DownloadExportAsync(context, export.Value!.JobId, default)).Value!);
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var header = Array.FindIndex(lines, l => l.StartsWith("rowType,"));

        Assert.Equal(60, lines.Length - header - 1);
        Assert.Contains($"Summary completedSaleCount,{screen.Value.Summary["completedSaleCount"]}", lines);
        Assert.Contains($"Snapshot ID,{screen.Value.SnapshotId}", lines);
        Assert.Contains("Business Timezone,Asia/Colombo", lines);
    }

    private static async Task<(Fixture F, Guid Scoped, Guid OrderInOtherOutlet)> OutletScopedUserAsync()
    {
        var f = new Fixture(); await f.SeedAsync();
        var scoped = Guid.NewGuid();
        f.Db.TenantUsers.Add(Entity<TenantUser>(("Id", scoped), ("TenantId", f.Tenant), ("AccountStatus", "ACTIVE"), ("FullName", "Manager"),
            ("StaffCode", "MGR"), ("OutletAccessScope", "SELECTED_OUTLETS"), ("TillAccessScope", "SELECTED_TILLS")));
        f.Db.OutletUserRoles.Add(Entity<OutletUserRole>(("Id", Guid.NewGuid()), ("TenantId", f.Tenant), ("OutletId", f.Outlet),
            ("TenantUserId", scoped), ("TenantRoleId", Guid.NewGuid())));
        f.Db.TenantUserTillAccess.Add(Entity<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", f.Tenant), ("TenantUserId", scoped),
            ("TillId", f.Till), ("AssignedAt", Noon)));
        f.Order(10m, 0m, till: f.Till);
        var other = f.Order(99m, 0m, outlet: f.OtherOutlet, till: f.OtherTill);
        await f.Db.SaveChangesAsync();
        return (f, scoped, other);
    }

    [Fact]
    public async Task AC13_ServerDeniesOtherOutletTillTenantAndSaleDetailAccess()
    {
        var (f, scoped, otherOrder) = await OutletScopedUserAsync();
        await using var _ = f;
        var service = f.Service();
        var permissions = new[] { TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.TillsView, TenantAdminReportPermissions.Export };
        var manager = new TenantRequestContext(f.Tenant, scoped, permissions);

        var otherOutlet = await service.GetSalesAsync(manager, Fixture.Query("transactions") with { OutletId = f.OtherOutlet }, default);
        var otherTill = await service.GetOutletsAsync(manager, Fixture.Query("tills") with { TillId = f.OtherTill }, default);
        var tillFromOtherOutlet = await service.GetSalesAsync(manager, Fixture.Query("transactions") with { OutletId = f.Outlet, TillId = f.OtherTill }, default);
        var detail = await service.GetSalesTransactionDetailAsync(manager, otherOrder, default);
        var export = await service.CreateExportAsync(manager, new ReportExportRequest("sales", "transactions", "csv",
            Fixture.Query("transactions") with { OutletId = f.OtherOutlet }), default);
        var otherTenant = await service.GetSalesAsync(new TenantRequestContext(Guid.NewGuid(), f.User, permissions), Fixture.Query("transactions"), default);
        var own = await service.GetSalesAsync(manager, Fixture.Query("transactions"), default);

        Assert.Equal("reports.permission_denied", otherOutlet.Error!.Code);
        Assert.Equal("reports.permission_denied", otherTill.Error!.Code);
        Assert.Equal("reports.permission_denied", tillFromOtherOutlet.Error!.Code);
        Assert.Equal("reports.not_found", detail.Error!.Code);
        Assert.Equal("reports.permission_denied", export.Error!.Code);
        Assert.Equal("reports.permission_denied", otherTenant.Error!.Code);
        Assert.True(own.IsSuccess, own.Error?.Message);
        Assert.DoesNotContain(own.Value!.Records, x => (Guid)x["orderId"]! == otherOrder);
        Assert.Equal(10m, D(own.Value.Summary, "salesExcludingTax"));
    }

    [Fact]
    public async Task AC13_ExportJobAndDownloadAreBoundToTheRequestingUser()
    {
        var (f, scoped, _) = await OutletScopedUserAsync();
        await using var _f = f;
        var service = f.Service();
        var owner = f.Context(TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
        var export = await service.CreateExportAsync(owner, new ReportExportRequest("sales", "transactions", "csv", Fixture.Query("transactions")), default);
        Assert.True(export.IsSuccess, export.Error?.Message);
        var intruder = new TenantRequestContext(f.Tenant, scoped, owner.Permissions);

        Assert.Equal("reports.not_found", (await service.GetExportAsync(intruder, export.Value!.JobId, default)).Error!.Code);
        Assert.Equal("reports.not_found", (await service.DownloadExportAsync(intruder, export.Value.JobId, default)).Error!.Code);
        Assert.True((await service.DownloadExportAsync(owner, export.Value.JobId, default)).IsSuccess);
    }

    [Fact]
    public async Task Security_MissingPermissionEntitlementExportRightOrInactiveUser_IsDenied()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        f.Order(10m, 0m); await f.Db.SaveChangesAsync();
        var viewer = f.Context(TenantAdminReportPermissions.SalesView);

        var cashierWithoutReports = await f.Service().GetSalesAsync(f.Context("pos.sale.create"), Fixture.Query("transactions"), default);
        var notEntitled = await f.Service(entitled: false).GetSalesAsync(viewer, Fixture.Query("transactions"), default);
        var exportWithoutRight = await f.Service().CreateExportAsync(viewer, new ReportExportRequest("sales", "transactions", "csv", Fixture.Query("transactions")), default);
        var stockWithoutRight = await f.Service().GetStockAsync(viewer, Fixture.Query("current"), default);
        var filtersWithoutEntitlement = await f.Service(entitled: false).GetFilterOptionsAsync(viewer, new(null, null, null, null, null, null), default);
        Set(f.Db.TenantUsers.Local.Single(), "AccountStatus", "INACTIVE");
        await f.Db.SaveChangesAsync();
        var inactive = await f.Service().GetSalesAsync(viewer, Fixture.Query("transactions"), default);
        var inactiveHome = await f.Service().GetDashboardAsync(viewer, Fixture.Query("dashboard"), default);

        Assert.Equal("reports.permission_denied", cashierWithoutReports.Error!.Code);
        Assert.Equal("reports.permission_denied", notEntitled.Error!.Code);
        Assert.Equal("reports.permission_denied", exportWithoutRight.Error!.Code);
        Assert.Equal("reports.permission_denied", stockWithoutRight.Error!.Code);
        Assert.Equal("reports.permission_denied", filtersWithoutEntitlement.Error!.Code);
        Assert.Equal("reports.permission_denied", inactive.Error!.Code);
        Assert.Equal("reports.permission_denied", inactiveHome.Error!.Code);
    }

    [Fact]
    public async Task FilterOptions_AreScopedToTheCallersOutletsAndTills()
    {
        var (f, scoped, _) = await OutletScopedUserAsync();
        await using var _f = f;
        f.Db.TenantUsers.Add(Entity<TenantUser>(("Id", Guid.NewGuid()), ("TenantId", f.Tenant), ("AccountStatus", "ACTIVE"), ("FullName", "Kandy cashier"),
            ("StaffCode", "KC"), ("OutletAccessScope", "SELECTED_OUTLETS"), ("TillAccessScope", "SELECTED_TILLS")));
        await f.Db.SaveChangesAsync();
        var manager = new TenantRequestContext(f.Tenant, scoped, [TenantAdminReportPermissions.SalesView]);

        var options = await f.Repository.GetFilterOptionsAsync(manager, new(null, null, null, null, null, null), default);

        Assert.Equal(f.Outlet.ToString(), Assert.Single(options.Groups["outlets"]).Id);
        Assert.Equal(f.Till.ToString(), Assert.Single(options.Groups["tills"]).Id);
        Assert.DoesNotContain(options.Groups["cashiers"], x => x.Name == "Kandy cashier");
        Assert.Contains(options.Groups["movementTypes"], x => x.Code == "SALE");
    }

    [Fact]
    public async Task AC14_EmptyResultIsSuccessWithZeroTotals_ButBackendFailureIsAnError()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var service = f.Service();
        var context = f.Context(TenantAdminReportPermissions.SalesView);

        var empty = await service.GetSalesAsync(context, Fixture.Query("payments"), default);
        Assert.True(empty.IsSuccess, empty.Error?.Message);
        Assert.Empty(empty.Value!.Records);
        Assert.Equal(0m, D(empty.Value.Summary, "successfulReceipts"));
        Assert.Equal("COMPLETE", empty.Value.Completeness);

        var failingRepository = new Mock<ITenantAdminReportsRepository>();
        failingRepository.Setup(x => x.CanAccessAsync(It.IsAny<TenantRequestContext>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        failingRepository.Setup(x => x.GetScopeStampAsync(It.IsAny<TenantRequestContext>(), It.IsAny<CancellationToken>())).ReturnsAsync("scope");
        failingRepository.Setup(x => x.GetSalesAsync(It.IsAny<TenantRequestContext>(), It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("database unavailable"));
        var entitlements = new Mock<ITenantFeatureEntitlementEvaluator>();
        entitlements.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
        var failing = new TenantAdminReportsService(failingRepository.Object, entitlements.Object, clock.Object, new Mock<ITenantAdminReportsAuditLogger>().Object);
        await Assert.ThrowsAsync<TimeoutException>(() => failing.GetSalesAsync(context, Fixture.Query("payments") with { Search = "unique-" + Guid.NewGuid() }, default));

        var disposed = f.Repository;
        await f.Db.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => disposed.GetSalesAsync(context, Fixture.Query("payments"), default));
    }

    [Fact]
    public async Task AC15_NonInventoryProductSale_AppearsInProductSales_ReturnHitsItsOwnPeriod_NoStockInvented()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var service = f.Product("Gift wrap service");
        var order = f.Order(55m, 5m);
        var line = f.Line(order, 1m, 55m, 5m, "Gift wrap service", service);
        f.Return(order, line, 50m, 5m, Noon.AddDays(5), disposition: "NOT_RESTOCKED");
        await f.Db.SaveChangesAsync();

        var saleDay = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("products"), default);
        var returnDay = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("products", Day.AddDays(5)), default);
        var stock = await f.Repository.GetStockAsync(f.Context(), Fixture.Query("current") with { From = null, To = null }, default);

        var sold = Assert.Single(saleDay.Records);
        Assert.Equal(1m, D(sold, "quantitySold"));
        Assert.Equal(0m, D(sold, "quantityReturned"));
        Assert.Equal(50m, D(sold, "netValueExTax"));
        var returned = Assert.Single(returnDay.Records);
        Assert.Equal(0m, D(returned, "quantitySold"));
        Assert.Equal(1m, D(returned, "quantityReturned"));
        Assert.Equal(-50m, D(returned, "netValueExTax"));
        Assert.Empty(stock.Records);
        Assert.Empty(f.Db.InventoryBalances);
        Assert.Empty(f.Db.StockMovements);
    }

    [Fact]
    public async Task AC16_LateSyncedActivityOnClosedTill_KeepsOriginalCountAndShowsTraceableCorrection()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var closed = Noon.AddHours(8);
        var (session, reconciliation) = await SeedClosedSessionAsync(f, closed);
        var lateOrder = f.Order(25m, 0m, closed.AddMinutes(-30), till: f.Till, session: session);
        var latePayment = f.Payment(lateOrder, f.Cash, 25m, at: closed.AddMinutes(-30), session: session);
        Set(f.Db.SalesPayments.Local.Single(x => x.Id == latePayment), "CreatedAt", closed.AddHours(2)); // synced after the close
        await f.Db.SaveChangesAsync();

        var result = await f.Repository.GetOutletsAsync(f.Context(), Fixture.Query("tills"), default);

        var row = Assert.Single(result.Records);
        Assert.Equal(340m, D(row, "expectedCashAmount"));
        Assert.Equal(335m, D(row, "countedCashAmount"));
        Assert.Equal(-5m, D(row, "cashDifference"));
        Assert.Equal(reconciliation.CreatedAt, row["closeSnapshotAt"]);
        Assert.Equal(true, row["requiresCorrectionReview"]);
        Assert.Contains(latePayment, (IEnumerable<Guid>)row["lateActivityPaymentIds"]!);
        // Revised figure comes from the current ledger (opening 100 + the one persisted 25 cash payment); original stays intact.
        Assert.Equal(125m, D(row, "revisedExpectedCashAmount"));
        Assert.Equal(-215m, D(row, "correctionDelta"));
        var stored = await f.Db.CashReconciliations.AsNoTracking().SingleAsync();
        Assert.Equal(335m, stored.CountedCashAmount);
        Assert.Equal(340m, stored.ExpectedCashAmount);
    }

    [Fact]
    public async Task ProvisionalMetadata_ReportsKnownPendingSyncForCallersOutletsOnly()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var client = Guid.NewGuid();
        f.Db.OfflineClients.Add(Entity<OfflineClient>(("Id", client), ("TenantId", f.Tenant), ("OutletId", f.Outlet), ("PosDeviceId", Guid.NewGuid()),
            ("ClientCode", "C1"), ("ClientName", "Till 1"), ("OfflineType", "POS"), ("Status", "ACTIVE")));
        f.Db.SyncItems.Add(Entity<SyncItem>(("Id", Guid.NewGuid()), ("TenantId", f.Tenant), ("OfflineClientId", client), ("Direction", "UPLOAD"),
            ("EntityName", "sales_order"), ("OperationType", "CREATE"), ("PayloadJson", "{}"), ("ItemStatus", "RECEIVED"), ("ReceivedAt", Noon)));
        await f.Db.SaveChangesAsync();

        var outlet = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions"), default);
        var otherOutlet = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("transactions") with { OutletId = f.OtherOutlet }, default);

        Assert.True(outlet.IsProvisional);
        Assert.Equal(1, outlet.KnownPendingSyncCount);
        Assert.Equal("PROVISIONAL", outlet.Completeness);
        Assert.NotNull(outlet.AsOf);
        Assert.False(otherOutlet.IsProvisional);
        Assert.Equal(0, otherOutlet.KnownPendingSyncCount);
    }

    [Fact]
    public async Task REP06_CategoryFilterUsesProductAssignment_IncludingSubcategories_AndSortsByNetValue()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        Guid parent = Guid.NewGuid(), child = Guid.NewGuid(), other = Guid.NewGuid();
        foreach (var (id, parentId, name) in new[] { (parent, (Guid?)null, "Drinks"), (child, (Guid?)parent, "Tea"), (other, (Guid?)null, "Food") })
            f.Db.Categories.Add(Entity<Category>(("Id", id), ("TenantId", f.Tenant), ("ParentCategoryId", parentId), ("CategoryName", name),
                ("CategoryCode", name), ("CategorySlug", name), ("Status", "ACTIVE")));
        Guid tea = f.Product("Tea"), juice = f.Product("Juice"), bread = f.Product("Bread");
        foreach (var (product, category) in new[] { (tea, child), (juice, parent), (bread, other) })
            f.Db.ProductCategories.Add(Entity<ProductCategory>(("Id", Guid.NewGuid()), ("TenantId", f.Tenant), ("ProductId", product), ("CategoryId", category)));
        var order = f.Order(90m, 0m);
        f.Line(order, 1m, 20m, 0m, "Tea", tea); f.Line(order, 1m, 40m, 0m, "Juice", juice); f.Line(order, 1m, 30m, 0m, "Bread", bread);
        await f.Db.SaveChangesAsync();

        var drinks = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("products") with { CategoryId = parent, SortBy = "netValueExTax", SortDirection = "desc" }, default);
        var teaOnly = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("products") with { SubcategoryId = child }, default);

        Assert.Equal(new[] { "Juice", "Tea" }, drinks.Records.Select(x => (string)x["productName"]!));
        Assert.Equal(60m, D(drinks.Summary, "netValueExTax"));
        Assert.Equal("Tea", Assert.Single(teaOnly.Records)["productName"]);
    }

    [Fact]
    public async Task REP01B_ReturnAdjustmentIsAllocatedToTheOriginalSaleChannel()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        var order = f.Order(110m, 10m, Noon.AddDays(-3));
        f.Return(order, f.Line(order, 5m, 110m, 10m), 20m, 2m, Noon);
        f.Order(55m, 5m);
        await f.Db.SaveChangesAsync();

        var result = await f.Repository.GetSalesAsync(f.Context(), Fixture.Query("channels"), default);

        var row = Assert.Single(result.Records);
        Assert.Equal("POS", row["channelCode"]);
        Assert.Equal(1, row["saleCount"]);
        Assert.Equal(50m, D(row, "salesExcludingTax"));
        Assert.Equal(20m, D(row, "returnAdjustment"));
        Assert.Equal(30m, D(row, "netSalesExcludingTax"));
        Assert.Equal(3m, D(row, "taxAmount"));
    }

    [Fact]
    public async Task REP00_Home_ShowsOnlyAuthorisedCardsWithRealTotals()
    {
        await using var f = new Fixture(); await f.SeedAsync();
        f.Payment(f.Order(110m, 10m), f.Cash, 110m); await f.Db.SaveChangesAsync();

        var result = await f.Service().GetDashboardAsync(f.Context(TenantAdminReportPermissions.PaymentsView), Fixture.Query("dashboard"), default);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("REP-00", result.Value!.ReportId);
        Assert.True(result.Value.Sections.ContainsKey("payments"));
        Assert.False(result.Value.Sections.ContainsKey("tills"));
        Assert.False(result.Value.Sections.ContainsKey("stock"));
        var metrics = (IReadOnlyDictionary<string, object?>)result.Value.Sections["payments"]!.GetType().GetProperty("metrics")!.GetValue(result.Value.Sections["payments"])!;
        Assert.Equal(110m, D(metrics, "successfulReceipts"));
    }

    [Fact]
    public void ReportRules_StatusMappingsAreExplicit()
    {
        Assert.True(ReportRules.IsSuccessfulPayment("PAID"));
        Assert.True(ReportRules.IsSuccessfulPayment("PARTIALLY_REFUNDED"));
        Assert.False(ReportRules.IsSuccessfulPayment("PENDING"));
        Assert.False(ReportRules.IsSuccessfulPayment("PAYMENT_SUBMITTED"));
        Assert.False(ReportRules.IsSuccessfulPayment("UNKNOWN_NEW_STATUS"));
        Assert.Equal("FAILED", ReportRules.PaymentOutcome("CANCELLED"));
        Assert.Equal("PENDING", ReportRules.RefundOutcome("REQUESTED"));
        Assert.False(ReportRules.IsSuccessfulRefund("COMPLETED", "PENDING"));
        Assert.Equal(StockBucket.Issue, ReportRules.MapStockMovement("SALE", -1m));
        Assert.Equal(StockBucket.TransferOut, ReportRules.MapStockMovement("TRANSFER", -1m));
        Assert.Equal("****", ReportRules.MaskReference("1234"));
    }
}
