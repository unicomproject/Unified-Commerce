using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Modules.Shared.ReturnExchange.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosReturnRepositoryTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompleteReturnAsync_WithCashRefund_PersistsReturnRefundAndReceipt()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-004", "Return Product", "return-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-000004", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false,
            1200m, 200m, 100m, 1100m, 1100m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000004", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 200m, 100m, 1100m, 1100m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-004", "Return Product", null, "EA",
            "Each", "STANDARD", "SIMPLE", 2m, 600m, 1200m, 200m, 100m, false, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            paymentId, tenantId, saleId, "PAY-000004", paymentMethodId,
            tillId, tillSessionId, "LKR", 1100m, 1100m, 1100m, 0,
            "return-complete-payment", "hash", userId, Now));
        dbContext.TillSessions.Add(TillSession.Open(
            tillSessionId, tenantId, outletId, tillId, "TS-000004",
            DateOnly.FromDateTime(Now.UtcDateTime), userId, deviceId,
            5000m, "LKR", null, Now));

        var reason = new ReturnReason();
        dbContext.ReturnReasons.Add(reason);
        dbContext.Entry(reason).Property(x => x.Id).CurrentValue = Guid.NewGuid();
        dbContext.Entry(reason).Property(x => x.TenantId).CurrentValue = tenantId;
        dbContext.Entry(reason).Property(x => x.ReasonCode).CurrentValue = "DAMAGED";
        dbContext.Entry(reason).Property(x => x.ReasonName).CurrentValue = "Damaged";
        dbContext.Entry(reason).Property(x => x.AppliesTo).CurrentValue = "RETURN";
        dbContext.Entry(reason).Property(x => x.IsActive).CurrentValue = true;
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext);
        var result = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "DAMAGED",
                "CASH_REFUND",
                "Package opened",
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)]),
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(result.ErrorCode);
        var receipt = Assert.IsType<PosReturnReceiptDto>(result.Receipt);
        Assert.Equal("COMPLETED", receipt.ReturnStatus);
        Assert.Equal("CASH_REFUND", receipt.SettlementMethodCode);
        Assert.Equal(550m, receipt.RefundAmount);
        Assert.Equal(1, await dbContext.SalesReturns.CountAsync());
        Assert.Equal(1, await dbContext.SalesReturnLines.CountAsync());
        Assert.Equal(1, await dbContext.SalesRefunds.CountAsync());
        Assert.Equal(1, await dbContext.SalesRefundLines.CountAsync());
        Assert.Equal(1, await dbContext.SalesRefundPaymentAllocations.CountAsync());
        Assert.Equal(1, await dbContext.TillCashMovements.CountAsync());
        Assert.Equal(1, await dbContext.Receipts.CountAsync(x => x.ReceiptType == "REFUND"));
        Assert.Equal(
            1m,
            await dbContext.SalesOrderLines
                .Where(x => x.Id == saleLineId)
                .Select(x => x.ReturnedQuantity)
                .SingleAsync());
        Assert.Equal(
            550m,
            await dbContext.SalesOrders
                .Where(x => x.Id == saleId)
                .Select(x => x.RefundedAmount)
                .SingleAsync());
        Assert.Equal(
            550m,
            await dbContext.SalesPayments
                .Where(x => x.Id == paymentId)
                .Select(x => x.RefundedAmount)
                .SingleAsync());

        var duplicateResult = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "DAMAGED",
                "CASH_REFUND",
                null,
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 2m)]),
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(duplicateResult.Receipt);
        Assert.Equal("quantity_exceeds_available", duplicateResult.ErrorCode);
        Assert.Equal(1, await dbContext.SalesReturns.CountAsync());
        Assert.Equal(1, await dbContext.SalesRefunds.CountAsync());
    }

    [Fact]
    public async Task PreviewCreditAsync_WithEligibleQuantity_ReturnsProratedCredit()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-003", "Preview Product", "preview-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-000003", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false,
            1200m, 200m, 100m, 1100m, 1100m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000003", saleId, Guid.NewGuid(),
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 200m, 100m, 1100m, 1100m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-003", "Preview Product", "Large", "EA",
            "Each", "STANDARD", "VARIABLE", 2m, 600m, 1200m, 200m, 100m, false, Now));

        var reason = new ReturnReason();
        dbContext.ReturnReasons.Add(reason);
        dbContext.Entry(reason).Property(x => x.TenantId).CurrentValue = tenantId;
        dbContext.Entry(reason).Property(x => x.ReasonCode).CurrentValue = "DAMAGED";
        dbContext.Entry(reason).Property(x => x.ReasonName).CurrentValue = "Damaged";
        dbContext.Entry(reason).Property(x => x.AppliesTo).CurrentValue = "RETURN";
        dbContext.Entry(reason).Property(x => x.IsActive).CurrentValue = true;
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext);
        var result = await repository.PreviewCreditAsync(
            tenantId,
            saleId,
            "DAMAGED",
            [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(result.ErrorCode);
        var preview = Assert.IsType<PosReturnCreditPreviewDto>(result.Preview);
        Assert.Equal("RCP-000003", preview.InvoiceNo);
        Assert.Equal("Damaged", preview.ReasonLabel);
        Assert.Equal(600m, preview.Calculation.ItemValue);
        Assert.Equal(100m, preview.Calculation.DiscountAdjustment);
        Assert.Equal(50m, preview.Calculation.TaxAdjustment);
        Assert.Equal(550m, preview.Calculation.NetCreditAmount);
        var item = Assert.Single(preview.Items);
        Assert.Equal(1m, item.ReturnQty);
        Assert.Equal(550m, item.LineAmount);
    }

    [Fact]
    public async Task GetSaleEligibilityAsync_WithActivePolicy_ReturnsEligibleLine()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-001", "Test Product", "test-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-000002", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false,
            1200m, 0m, 0m, 1200m, 1200m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000002", saleId, Guid.NewGuid(),
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 0m, 0m, 1200m, 1200m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-002", "Test Product", "Large", "EA",
            "Each", "STANDARD", "VARIABLE", 2m, 600m, 1200m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext);
        var result = await repository.GetSaleEligibilityAsync(
            tenantId,
            saleId,
            Now.AddDays(5),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("RCP-000002", result.InvoiceNo);
        var item = Assert.Single(result.Items);
        Assert.True(item.IsReturnable);
        Assert.Equal("ELIGIBLE", item.EligibilityStatus);
        Assert.Equal(2m, item.SoldQty);
        Assert.Equal(2m, item.AvailableReturnQty);
        Assert.Contains(result.PolicyChecks, check =>
            check.Label == "Return window" && check.Passed);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_Recent_ReturnsCompletedPaidTenantSaleSummary()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-000001", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false,
            1000m, 0m, 0m, 1000m, 1000m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000001", saleId, Guid.NewGuid(),
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1000m, 0m, 0m, 1000m, 1000m, 0m, "{}", Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, "PAY-000001", paymentMethodId,
            tillId, tillSessionId, "LKR", 1000m, 1000m, 1000m, 0m,
            "return-search-payment", "hash", userId, Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-001", "Test Product", null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 1000m, 1000m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext);
        var result = await repository.SearchOriginalSalesAsync(
            tenantId, "recent", null, 1, 20, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        var sale = Assert.Single(result.Items);
        Assert.Equal(saleId, sale.SaleId);
        Assert.Equal("RCP-000001", sale.InvoiceNo);
        Assert.Equal("Walk-in Customer", sale.CustomerName);
        Assert.Equal("Cash", sale.PaymentMethod);
        Assert.Equal(1, sale.ItemCount);
        Assert.Equal(1000m, sale.Total);
        Assert.Equal("LKR", sale.Currency);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}

