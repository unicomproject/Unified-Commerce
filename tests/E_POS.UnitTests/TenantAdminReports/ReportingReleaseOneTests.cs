using System.Reflection;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Shared.Refund.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using Moq;
using System.Text;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.UnitTests.TenantAdminReports;

public sealed class ReportingReleaseOneTests
{
    private static T Entity<T>(params (string Name, object? Value)[] values) where T : new()
    {
        var entity = new T();
        typeof(T).GetProperty("UpdatedAt")!.SetValue(entity, DateTimeOffset.UtcNow);
        typeof(T).GetProperty("CreatedAt")!.SetValue(entity, DateTimeOffset.UtcNow);
        foreach (var (name, value) in values)
            (typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unknown {typeof(T).Name}.{name}")).SetValue(entity, value);
        return entity;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public readonly Guid Tenant = Guid.NewGuid(), User = Guid.NewGuid(), Outlet = Guid.NewGuid(), Method = Guid.NewGuid();
        public readonly EPosDbContext Db = new(new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public TenantRequestContext Context => new(Tenant, User, []);
        public TenantAdminReportsRepository Repository => new(Db);
        public ReportQueryRequest Query(string section) => new(new DateOnly(2026,9,20), new DateOnly(2026,9,20),
            null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,section);
        public async Task Seed()
        {
            Db.Tenants.Add(Entity<Tenant>(("Id",Tenant),("DefaultTimezone","Asia/Colombo"),("BaseCurrencyCode","LKR")));
            Db.TenantUsers.Add(Entity<TenantUser>(("Id",User),("TenantId",Tenant),("AccountStatus","ACTIVE"),("StaffCode","REPORT-QA"),
                ("OutletAccessScope","ALL_OUTLETS"),("TillAccessScope","ALL_ACCESSIBLE_TILLS")));
            Db.Outlets.Add(Entity<Outlet>(("Id",Outlet),("TenantId",Tenant),("OutletName","Store"),("Status","ACTIVE")));
            Db.SalesChannels.Add(Entity<SalesChannel>(("Id",Tenant),("TenantId",Tenant),("CustomName","POS")));
            Db.PaymentMethods.Add(Entity<PaymentMethod>(("Id",Method),("TenantId",Tenant),("MethodCode","CASH"),("MethodName","Cash"),("MethodType","CASH")));
            await Db.SaveChangesAsync();
        }
        public Guid Order(decimal total=110m)
        {
            var id=Guid.NewGuid();
            Db.SalesOrders.Add(Entity<SalesOrder>(("Id",id),("TenantId",Tenant),("ReportingOutletId",Outlet),
                ("OrderNumber",id.ToString()),("SalesChannelId",Tenant),("BusinessDate",new DateOnly(2026,9,1)),
                ("Status","COMPLETED"),("PaymentStatus","PAID"),("CompletedAt",new DateTimeOffset(2026,9,1,12,0,0,TimeSpan.Zero)),("TotalAmount",total),("SubtotalAmount",total-10m),
                ("TaxAmount",10m),("CurrencyCode","LKR")));
            return id;
        }
        public Guid Payment(Guid order, decimal amount, string status="PAID", decimal change=0m)
        {
            var id=Guid.NewGuid();
            Db.SalesPayments.Add(Entity<SalesPayment>(("Id",id),("TenantId",Tenant),("SalesOrderId",order),
                ("PaymentMethodId",Method),("PaymentStatus",status),("PaidAmount",amount),("ChangeAmount",change),
                ("TenderedAmount",amount+change),("CurrencyCode","LKR"),
                ("PaidAt",new DateTimeOffset(2026,9,19,19,0,0,TimeSpan.Zero))));
            return id;
        }
        public ValueTask DisposeAsync()=>Db.DisposeAsync();
    }

    [Fact]
    public async Task PaymentEvents_UseBusinessLocalEventDate_AndAllocatedCashAfterChange()
    {
        await using var f=new Fixture(); await f.Seed();
        f.Payment(f.Order(30m),30m,change:20m); await f.Db.SaveChangesAsync();
        var result=await f.Repository.GetSalesAsync(f.Context,f.Query("payment-transactions"),default);
        Assert.Single(result.Records); Assert.Equal(30m,result.Summary["successfulReceipts"]);
        Assert.Equal(20m,result.Records[0]["changeAmount"]);
        var previous=await f.Repository.GetSalesAsync(f.Context,f.Query("payments") with { From=new(2026,9,19),To=new(2026,9,19)},default);
        Assert.Empty(previous.Records);
    }

    [Fact]
    public async Task SplitTender_StatusFilterChangesDetailOnly_RefundsUseSeparateEvents()
    {
        await using var f=new Fixture(); await f.Seed(); var order=f.Order();
        var payment=f.Payment(order,40m,"PARTIALLY_REFUNDED"); f.Payment(order,70m); f.Payment(order,900m,"PENDING");
        var refund=Guid.NewGuid();
        f.Db.SalesRefunds.Add(SalesRefund.CreateCompleted(refund,f.Tenant,order,Guid.NewGuid(),"R-1","ORIGINAL","LKR",22m,"Return",f.User,new DateTimeOffset(2026,9,20,1,0,0,TimeSpan.Zero)));
        f.Db.SalesRefundPaymentAllocations.Add(SalesRefundPaymentAllocation.CreateCompleted(Guid.NewGuid(),f.Tenant,refund,payment,f.Method,22m,null,new DateTimeOffset(2026,9,20,1,0,0,TimeSpan.Zero)));
        await f.Db.SaveChangesAsync();
        var result=await f.Repository.GetSalesAsync(f.Context,f.Query("payment-transactions") with { PaymentStatus="PENDING" },default);
        Assert.Single(result.Records); Assert.Equal(0m,result.Records[0]["signedAmount"]);
        Assert.Equal(110m,result.Summary["successfulReceipts"]); Assert.Equal(22m,result.Summary["successfulRefunds"]);
        Assert.Equal(88m,result.Summary["netReceipts"]);
    }

    [Fact]
    public async Task TransactionSummary_CoversAllPages()
    {
        await using var f=new Fixture(); await f.Seed();
        for(var i=0;i<26;i++) f.Order(); await f.Db.SaveChangesAsync();
        var request=f.Query("transactions") with { From=null,To=null };
        var first=await f.Repository.GetSalesAsync(f.Context,request,default);
        var last=await f.Repository.GetSalesAsync(f.Context,request with { Page=2 },default);
        Assert.Equal(25,first.Records.Count); Assert.Single(last.Records);
        Assert.Equal(26,first.Summary["transactionCount"]); Assert.Equal(first.Summary["netSales"],last.Summary["netSales"]);
    }
    [Fact]
    public async Task ReturnPosting_ReducesItsOwnPeriod_WithoutSuccessfulRefund_OrInventedStock()
    {
        await using var f=new Fixture(); await f.Seed(); var order=f.Order();
        var lineId=Guid.NewGuid(); var productId=Guid.NewGuid();
        f.Db.SalesOrderLines.Add(Entity<SalesOrderLine>(("Id",lineId),("TenantId",f.Tenant),("SalesOrderId",order),
            ("ProductId",productId),("ProductNameSnapshot","Original service name"),("Quantity",5m),
            ("LineTotalAmount",110m),("LineTaxAmount",10m),("LineSubtotalAmount",100m),("UomCodeSnapshot","EA")));
        var returned=Guid.NewGuid(); var now=new DateTimeOffset(2026,9,20,1,0,0,TimeSpan.Zero);
        f.Db.SalesReturns.Add(SalesReturn.CreateCompleted(returned,f.Tenant,order,null,f.Outlet,Guid.NewGuid(),"R-22",1m,22m,null,"return-22",f.User,now));
        f.Db.SalesReturnLines.Add(SalesReturnLine.CreateReceived(Guid.NewGuid(),f.Tenant,returned,lineId,Guid.NewGuid(),1m,20m,2m,20m,2m,false,null,now));
        await f.Db.SaveChangesAsync();
        var original=await f.Repository.GetSalesAsync(f.Context,f.Query("transactions") with { From=new(2026,9,1),To=new(2026,9,1)},default);
        Assert.Equal(100m,original.Summary["salesExcludingTax"]); Assert.Equal(10m,original.Summary["netTax"]);
        Assert.Equal(110m,original.Summary["netSalesIncludingTax"]); Assert.Equal(1,original.Summary["completedSaleCount"]);
        var period=await f.Repository.GetSalesAsync(f.Context,f.Query("transactions"),default);
        Assert.Equal(-20m,period.Summary["netSalesExcludingTax"]); Assert.Equal(-2m,period.Summary["netTax"]);
        Assert.Equal(-22m,period.Summary["netSalesIncludingTax"]); Assert.Null(period.Summary["averageSaleValue"]);
        var payments=await f.Repository.GetSalesAsync(f.Context,f.Query("payments"),default);
        Assert.Equal(0m,payments.Summary["successfulRefunds"]);
        var products=await f.Repository.GetSalesAsync(f.Context,f.Query("products"),default);
        Assert.Single(products.Records); Assert.Equal(1m,products.Records[0]["quantityReturned"]);
        Assert.Equal(-20m,products.Records[0]["netValueExTax"]); Assert.Empty(f.Db.InventoryBalances);
    }

    [Fact]
    public async Task SnapshotExport_PreservesCapturedRows_AndDeniesAfterScopeRevocation()
    {
        await using var f=new Fixture(); await f.Seed(); f.Order(); await f.Db.SaveChangesAsync();
        var ent=new Mock<ITenantFeatureEntitlementEvaluator>();
        ent.Setup(x=>x.IsEnabledAsync(It.IsAny<Guid>(),It.IsAny<string>(),It.IsAny<DateTimeOffset>(),It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var clock=new Mock<IDateTimeProvider>(); clock.SetupGet(x=>x.UtcNow).Returns(DateTimeOffset.UtcNow);
        var svc=new TenantAdminReportsService(f.Repository,ent.Object,clock.Object,new Mock<ITenantAdminReportsAuditLogger>().Object);
        var ctx=f.Context with { Permissions=new[]{"tenant.reports.sales.view","tenant.reports.export"} };
        var query=f.Query("transactions") with { From=null,To=null };
        var captured=await svc.GetSalesAsync(ctx,query,default); Assert.True(captured.IsSuccess);
        Assert.NotNull(captured.Value!.SnapshotId);
        var later=f.Order(); await f.Db.SaveChangesAsync();
        var export=await svc.CreateExportAsync(ctx,new("sales","transactions","csv",query with { SnapshotId=captured.Value.SnapshotId }),default);
        Assert.True(export.IsSuccess,export.Error?.Message);
        var download=await svc.DownloadExportAsync(ctx,export.Value!.JobId,default); Assert.True(download.IsSuccess);
        Assert.DoesNotContain(later.ToString(),Encoding.UTF8.GetString(download.Value!));
        typeof(TenantUser).GetProperty("AccountStatus")!.SetValue(await f.Db.TenantUsers.SingleAsync(),"INACTIVE");
        await f.Db.SaveChangesAsync();
        var denied=await svc.DownloadExportAsync(ctx,export.Value.JobId,default); Assert.True(denied.IsFailure);
    }

    [Fact]
    public async Task Closing_UsesOriginalReconciliation_AndFlagsLaterPayment()
    {
        await using var f=new Fixture(); await f.Seed(); var till=Guid.NewGuid(); var session=Guid.NewGuid();
        var closed=new DateTimeOffset(2026,9,20,1,0,0,TimeSpan.Zero);
        f.Db.Tills.Add(Entity<Till>(("Id",till),("TenantId",f.Tenant),("OutletId",f.Outlet),("Status","ACTIVE")));
        f.Db.TillSessions.Add(Entity<TillSession>(("Id",session),("TenantId",f.Tenant),("OutletId",f.Outlet),("TillId",till),
            ("BusinessDate",new DateOnly(2026,9,20)),("OpenedByTenantUserId",f.User),("OpeningFloatAmount",100m),
            ("Status","CLOSED"),("OpenedAt",closed.AddHours(-9)),("ClosedAt",closed),("CurrencyCode","LKR")));
        var reconciliation=CashReconciliation.Create(Guid.NewGuid(),f.Tenant,session,"REC-1",340m,335m,-5m,"LKR","SHORTAGE",
            "{\"OpeningFloat\":100,\"CashPayments\":300,\"CashIn\":10,\"CashOut\":50,\"ExpectedCash\":340}",closed);
        reconciliation.Submit(f.User,closed); f.Db.CashReconciliations.Add(reconciliation);
        var payment=f.Payment(f.Order(),10m);
        typeof(SalesPayment).GetProperty("TillSessionId")!.SetValue(f.Db.SalesPayments.Local.Single(x=>x.Id==payment),session);
        await f.Db.SaveChangesAsync();
        var result=await f.Repository.GetOutletsAsync(f.Context,f.Query("tills"),default); var row=Assert.Single(result.Records);
        Assert.Equal(340m,row["expectedCashAmount"]); Assert.Equal(335m,row["countedCashAmount"]); Assert.Equal(-5m,row["cashDifference"]);
        Assert.Equal("SHORTAGE",row["varianceReason"]); Assert.Equal(true,row["requiresCorrectionReview"]);
        Assert.Equal(335m,(await f.Db.CashReconciliations.SingleAsync()).CountedCashAmount);
    }

}
