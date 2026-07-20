using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Modules.Shared.ReturnExchange.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
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
            false, 1200m, 200m, 100m, 1100m, 1100m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000004", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 200m, 100m, 1100m, 1100m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-004", null, "Return Product", null, null, null, null, null, "EA",
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

        var draftVersion = SeedValidatedInspectionDraft(
            dbContext,
            tenantId,
            outletId,
            saleId,
            saleLineId,
            userId,
            validatedAt: Now.AddDays(5));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "REFUND",
                "DAMAGED",
                "CASH_REFUND",
                "Package opened",
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
                draftVersion,
                "test-complete-cash-refund-1"),
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

        var sameKeyReplay = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "REFUND",
                "DAMAGED",
                "CASH_REFUND",
                "Package opened",
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
                draftVersion,
                "test-complete-cash-refund-1"),
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(sameKeyReplay.ErrorCode);
        Assert.NotNull(sameKeyReplay.Receipt);
        Assert.Equal(receipt.ReturnId, sameKeyReplay.Receipt!.ReturnId);
        Assert.Equal(receipt.ReceiptNumber, sameKeyReplay.Receipt.ReceiptNumber);
        Assert.Equal(1, await dbContext.SalesReturns.CountAsync());
        Assert.Equal(1, await dbContext.SalesRefunds.CountAsync());
        Assert.Equal(1, await dbContext.TillCashMovements.CountAsync());
        Assert.Equal(1, await dbContext.Receipts.CountAsync(x => x.ReceiptType == "REFUND"));
        Assert.Equal(
            "test-complete-cash-refund-1",
            await dbContext.SalesReturns
                .Where(x => x.Id == receipt.ReturnId)
                .Select(x => x.IdempotencyKey)
                .SingleAsync());

        var conflictResult = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                Guid.NewGuid(),
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "REFUND",
                "DAMAGED",
                "CASH_REFUND",
                null,
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
                draftVersion,
                "test-complete-cash-refund-1"),
            Now.AddDays(5),
            CancellationToken.None);
        Assert.Null(conflictResult.Receipt);
        Assert.Equal("idempotency_conflict", conflictResult.ErrorCode);

        var duplicateResult = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "REFUND",
                "DAMAGED",
                "CASH_REFUND",
                null,
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 2m)],
                draftVersion,
                "test-complete-cash-refund-2"),
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(duplicateResult.Receipt);
        Assert.Equal("inspection_draft_consumed", duplicateResult.ErrorCode);
        Assert.Equal(1, await dbContext.SalesReturns.CountAsync());
        Assert.Equal(1, await dbContext.SalesRefunds.CountAsync());
    }

    [Fact]
    public async Task GetCompletionAsync_CompletedCashRefund_ReturnsTillOutletAndLineDetails()
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

        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Completion Outlet",
            "OUT-COMP",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            userId,
            Now));
        dbContext.Tills.Add(Till.Create(
            tillId,
            tenantId,
            outletId,
            "Front Till",
            "Front",
            1,
            "TILL-COMP",
            "STANDARD",
            0m,
            "LKR",
            true,
            "ACTIVE",
            userId,
            Now));
        dbContext.PosDevices.Add(PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "DEV-COMP",
            "POS Tablet",
            "TABLET",
            "ACTIVE",
            userId,
            Now));
        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-COMP", "Return Product", "return-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-COMP-001", Guid.NewGuid(), null,
            "Walk-in Snapshot", tillId, tillSessionId, null, "LKR",
            false, 1200m, 200m, 100m, 1100m, 1100m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-COMP-001", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 200m, 100m, 1100m, 1100m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-COMP", null, "Return Product", "Red", null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 2m, 600m, 1200m, 200m, 100m, false, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            paymentId, tenantId, saleId, "PAY-COMP-001", paymentMethodId,
            tillId, tillSessionId, "LKR", 1100m, 1100m, 1100m, 0,
            "cash-ref", "hash", userId, Now));
        dbContext.TillSessions.Add(TillSession.Open(
            tillSessionId, tenantId, outletId, tillId, "TS-COMP-001",
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

        var draftVersion = SeedValidatedInspectionDraft(
            dbContext, tenantId, outletId, saleId, saleLineId, userId, validatedAt: Now.AddDays(5));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(
            dbContext,
            new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var complete = await repository.CompleteReturnAsync(
            tenantId,
            userId,
            new PosReturnCompleteCommand(
                saleId,
                deviceId,
                tillSessionId,
                outletId,
                tillId,
                "REFUND",
                "DAMAGED",
                "CASH_REFUND",
                "Package opened",
                [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
                draftVersion,
                "test-get-completion-cash-1"),
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(complete.ErrorCode);
        Assert.NotNull(complete.Receipt);

        var loaded = await repository.GetCompletionAsync(
            tenantId, outletId, complete.Receipt!.ReturnId, CancellationToken.None);

        Assert.Null(loaded.ErrorCode);
        var receipt = Assert.IsType<PosReturnReceiptDto>(loaded.Receipt);
        Assert.Equal("COMPLETED", receipt.ReturnStatus);
        Assert.Equal("CASH_REFUND", receipt.SettlementMethodCode);
        Assert.Equal("Front Till", receipt.TillName);
        Assert.Equal("Completion Outlet", receipt.OutletName);
        Assert.Equal(deviceId, receipt.DeviceId);
        Assert.Equal("POS Tablet", receipt.DeviceName);
        Assert.Equal("Walk-in Snapshot", receipt.CustomerName);
        Assert.Equal("Walk-in Snapshot", receipt.CustomerDisplayName);
        Assert.Null(receipt.MaskedCard);
        Assert.Null(receipt.CardBrand);
        Assert.NotEqual("STORE_CREDIT", receipt.SettlementMethodCode);
        Assert.NotEmpty(receipt.ReturnedItems!);
        Assert.Equal("DAMAGED", receipt.ReturnedItems![0].ReasonCode);
        Assert.Equal("Damaged", receipt.ReturnedItems[0].ReasonDisplay);
        Assert.True(receipt.ReturnedItems[0].Subtotal.HasValue);
        Assert.True(receipt.ReturnedItems[0].Tax.HasValue);
        Assert.True(receipt.ReturnedItems[0].Total.HasValue);
        Assert.True(receipt.ReturnSubtotal.HasValue);
        Assert.True(receipt.ReturnTax.HasValue);
        Assert.True(receipt.ReturnTotal.HasValue);
        Assert.Equal(0, receipt.PrintCount);

        var incomplete = await repository.GetCompletionAsync(
            tenantId, outletId, Guid.NewGuid(), CancellationToken.None);
        Assert.Equal("completion_not_found", incomplete.ErrorCode);

        var crossOutlet = await repository.GetCompletionAsync(
            tenantId, Guid.NewGuid(), complete.Receipt.ReturnId, CancellationToken.None);
        Assert.Equal("completion_not_found", crossOutlet.ErrorCode);

        var crossTenant = await repository.GetCompletionAsync(
            Guid.NewGuid(), outletId, complete.Receipt.ReturnId, CancellationToken.None);
        Assert.Equal("completion_not_found", crossTenant.ErrorCode);
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
        var outletId = Guid.NewGuid();
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
            false, 1200m, 200m, 100m, 1100m, 1100m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000003", saleId, Guid.NewGuid(),
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 200m, 100m, 1100m, 1100m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-003", null, "Preview Product", "Large", null, null, null, null, "EA",
            "Each", "STANDARD", "VARIABLE", 2m, 600m, 1200m, 200m, 100m, false, Now));

        var reason = new ReturnReason();
        dbContext.ReturnReasons.Add(reason);
        dbContext.Entry(reason).Property(x => x.TenantId).CurrentValue = tenantId;
        dbContext.Entry(reason).Property(x => x.ReasonCode).CurrentValue = "DAMAGED";
        dbContext.Entry(reason).Property(x => x.ReasonName).CurrentValue = "Damaged";
        dbContext.Entry(reason).Property(x => x.AppliesTo).CurrentValue = "RETURN";
        dbContext.Entry(reason).Property(x => x.IsActive).CurrentValue = true;
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
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
        var outletId = Guid.NewGuid();
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
            false, 1200m, 0m, 0m, 1200m, 1200m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000002", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 0m, 0m, 1200m, 1200m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-002", null, "Test Product", "Large", null, null, null, null, "EA",
            "Each", "STANDARD", "VARIABLE", 2m, 600m, 1200m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.GetSaleEligibilityAsync(
            tenantId,
            outletId,
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
    public async Task GetSaleEligibilityAsync_WithoutProductPolicy_UsesTenantDefaultPolicy()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.Parse(DevelopmentPosReturnPolicySeedData.PolicyId);
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId,
            tenantId,
            DevelopmentPosReturnPolicySeedData.PolicyCode,
            DevelopmentPosReturnPolicySeedData.PolicyName,
            "Default policy",
            DevelopmentPosReturnPolicySeedData.ReturnWindowDays,
            DevelopmentPosReturnPolicySeedData.ExchangeWindowDays,
            true,
            true,
            false,
            true,
            "ACTIVE",
            userId,
            Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "MER-006", "Casual Sneakers", "casual-sneakers",
            "STANDARD", "SIMPLE", null, null, null, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-MER-006", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 6500m, 0m, 0m, 6500m, 6500m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-MER-006", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 6500m, 0m, 0m, 6500m, 6500m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "MER-006-SIZE-8", null, "Casual Sneakers", "Size 8", null, null, null, null, "EA",
            "Each", "STANDARD", "VARIABLE", 1m, 6500m, 6500m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(
            dbContext,
            new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.GetSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            Now.AddDays(4),
            CancellationToken.None);

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal("MER-006-SIZE-8", item.Sku);
        Assert.True(item.IsReturnable);
        Assert.Equal("ELIGIBLE", item.EligibilityStatus);
        Assert.Null(item.IneligibilityReason);
        Assert.Equal(1m, item.AvailableReturnQty);
    }

    [Fact]
    public async Task GetSaleEligibilityAsync_WithoutAnyPolicy_ReturnsNoPolicyReason()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.Products.Add(Product.Create(
            productId, tenantId, "MER-006", "Casual Sneakers", "casual-sneakers",
            "STANDARD", "SIMPLE", null, null, null, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-NO-POLICY", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 6500m, 0m, 0m, 6500m, 6500m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-NO-POLICY", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 6500m, 0m, 0m, 6500m, 6500m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "MER-006-SIZE-8", null, "Casual Sneakers", "Size 8", null, null, null, null, "EA",
            "Each", "STANDARD", "VARIABLE", 1m, 6500m, 6500m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(
            dbContext,
            new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.GetSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            Now,
            CancellationToken.None);

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.False(item.IsReturnable);
        Assert.Equal("NO_POLICY", item.EligibilityStatus);
        Assert.Equal("No active return policy is assigned to this product.", item.IneligibilityReason);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithEligibleLine_ReturnsSummaryAndChecks()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-002", "Check Product", "check-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-000005", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 1200m, 0m, 0m, 1200m, 1200m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000005", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 0m, 0m, 1200m, 1200m, 0m, "{}", Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, "PAY-000005", paymentMethodId,
            tillId, tillSessionId, "LKR", 1200m, 1200m, 1200m, 0m,
            "eligibility-check-payment", "hash", userId, Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-005", null, "Check Product", "Large", null, null, null, null, "EA",
            "Each", "STANDARD", "VARIABLE", 2m, 600m, 1200m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.CheckSelectedSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
            Now.AddDays(5),
            CancellationToken.None);

        Assert.Null(result.ErrorCode);
        var eligibility = result.Eligibility;
        Assert.NotNull(eligibility);
        Assert.Equal("ELIGIBLE", eligibility.OverallStatus);
        Assert.True(eligibility.CanContinue);
        Assert.Equal(1, eligibility.SelectedItemCount);
        Assert.Equal(1, eligibility.EligibleItemCount);
        Assert.Single(eligibility.Items);
        Assert.Equal(1m, eligibility.Items[0].RequestedReturnQty);
        Assert.Contains(eligibility.PolicyChecks, check => check.Code == "RETURN_WINDOW");
        Assert.Contains(eligibility.PolicyChecks, check =>
            check.Code == "ORIGINAL_RECEIPT" && check.Status == "PASSED");
        Assert.Contains(eligibility.PolicyChecks, check =>
            check.Code == "PAYMENT_VERIFICATION" && check.Status == "PASSED");
        Assert.Contains(eligibility.PolicyChecks, check =>
            check.Code == "PRODUCT_RETURN_POLICY" && check.Status == "PASSED");
        Assert.Contains(eligibility.PolicyChecks, check =>
            check.Code == "INSPECTION_REQUIRED" && check.Status == "NOT_APPLICABLE");
        Assert.Contains(eligibility.PolicyChecks, check =>
            check.Code == "MANAGER_APPROVAL_REQUIRED" && check.Status == "NOT_APPLICABLE");
        Assert.False(eligibility.RequiresInspection);
        Assert.False(eligibility.RequiresManagerApproval);
        Assert.DoesNotContain(
            "original packaging",
            eligibility.PolicyNote ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WhenReceiptNotRequired_ReturnsNotApplicable()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "NO-RCPT", "No Receipt Required", "Optional receipt policy",
            30, 30, false, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-NR", "No Receipt Product", "no-receipt-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-NR-001", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 500m, 0m, 0m, 500m, 500m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-NR-001", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 500m, 0m, 0m, 500m, 500m, 0m, "{}", Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, "PAY-NR-001", paymentMethodId,
            tillId, tillSessionId, "LKR", 500m, 500m, 500m, 0m,
            "eligibility-no-receipt", "hash", userId, Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-NR", null, "No Receipt Product", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 500m, 500m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.CheckSelectedSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
            Now.AddDays(1),
            CancellationToken.None);

        Assert.Null(result.ErrorCode);
        var eligibility = Assert.IsType<PosReturnSaleEligibilityDto>(result.Eligibility);
        Assert.True(eligibility.CanContinue);
        var receiptCheck = Assert.Single(
            eligibility.PolicyChecks,
            check => check.Code == "ORIGINAL_RECEIPT");
        Assert.Equal("NOT_APPLICABLE", receiptCheck.Status);
        Assert.Equal("Optional receipt policy", eligibility.PolicyNote);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WithoutSuccessfulPayment_FailsPaymentCheck()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-NP", "No Payment Product", "no-payment-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-NP-001", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 400m, 0m, 0m, 400m, 400m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-NP-001", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 400m, 0m, 0m, 400m, 400m, 0m, "{}", Now));
        // Intentionally omit SalesPayments — order status alone must not verify payment.
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-NP", null, "No Payment Product", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 400m, 400m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.CheckSelectedSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
            Now.AddDays(1),
            CancellationToken.None);

        Assert.Null(result.ErrorCode);
        var eligibility = Assert.IsType<PosReturnSaleEligibilityDto>(result.Eligibility);
        Assert.False(eligibility.CanContinue);
        Assert.Equal("NOT_ELIGIBLE", eligibility.OverallStatus);
        var paymentCheck = Assert.Single(
            eligibility.PolicyChecks,
            check => check.Code == "PAYMENT_VERIFICATION");
        Assert.Equal("FAILED", paymentCheck.Status);
    }

    [Fact]
    public async Task CheckSelectedSaleEligibilityAsync_WhenManagerApprovalRequired_ReturnsReviewState()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "MGR-30", "Manager Approval", null,
            30, 30, true, true, true, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-MGR", "Manager Product", "manager-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-MGR-001", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 900m, 0m, 0m, 900m, 900m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-MGR-001", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 900m, 0m, 0m, 900m, 900m, 0m, "{}", Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, "PAY-MGR-001", paymentMethodId,
            tillId, tillSessionId, "LKR", 900m, 900m, 900m, 0m,
            "eligibility-manager", "hash", userId, Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            saleLineId, tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-MGR", null, "Manager Product", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 900m, 900m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.CheckSelectedSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            [new PosReturnCreditPreviewLineRequestDto(saleLineId, 1m)],
            Now.AddDays(1),
            CancellationToken.None);

        Assert.Null(result.ErrorCode);
        var eligibility = Assert.IsType<PosReturnSaleEligibilityDto>(result.Eligibility);
        Assert.True(eligibility.CanContinue);
        Assert.Equal("ELIGIBLE_WITH_WARNINGS", eligibility.OverallStatus);
        Assert.True(eligibility.RequiresManagerApproval);
        Assert.False(eligibility.RequiresInspection);
        var approvalCheck = Assert.Single(
            eligibility.PolicyChecks,
            check => check.Code == "MANAGER_APPROVAL_REQUIRED");
        Assert.Equal("REQUIRES_REVIEW", approvalCheck.Status);
        var inspectionCheck = Assert.Single(
            eligibility.PolicyChecks,
            check => check.Code == "INSPECTION_REQUIRED");
        Assert.Equal("NOT_APPLICABLE", inspectionCheck.Status);
        Assert.DoesNotContain(
            "original packaging",
            eligibility.PolicyNote ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_Recent_ReturnsCompletedPaidTenantSaleSummary()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        var otherSaleId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-000001", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 1000m, 0m, 0m, 1000m, 1000m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-000001", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1000m, 0m, 0m, 1000m, 1000m, 0m, "{}", Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            otherSaleId, tenantId, "SO-OTHER-001", Guid.NewGuid(), null,
            "Other Outlet Customer", tillId, tillSessionId, null, "LKR",
            false, 500m, 0m, 0m, 500m, 500m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-OTHER-001", otherSaleId, otherOutletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 500m, 0m, 0m, 500m, 500m, 0m, "{}", Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, "PAY-000001", paymentMethodId,
            tillId, tillSessionId, "LKR", 1000m, 1000m, 1000m, 0m,
            "return-search-payment", "hash", userId, Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-001", null, "Test Product", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 1000m, 1000m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.SearchOriginalSalesAsync(
            tenantId,
            outletId,
            "recent",
            null,
            new PosReturnSaleSearchFilterDto(null, null, null, null, null),
            1,
            20,
            CancellationToken.None);

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

    [Fact]
    public async Task SearchOriginalSalesAsync_FiltersByCurrentOutletForEverySearchType()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        var matchingSaleId = Guid.NewGuid();
        var otherOutletSaleId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            paymentMethodId, tenantId, "CARD", "Visa", "CARD",
            true, false, 1, "ACTIVE", userId, Now));
        AddSaleForSearch(
            dbContext,
            tenantId,
            matchingSaleId,
            outletId,
            tillId,
            tillSessionId,
            userId,
            paymentMethodId,
            "SO-MATCH-001",
            "RCP-MATCH-001",
            "Outlet Customer",
            "0771234567");
        AddSaleForSearch(
            dbContext,
            tenantId,
            otherOutletSaleId,
            otherOutletId,
            tillId,
            tillSessionId,
            userId,
            paymentMethodId,
            "SO-OTHER-002",
            "RCP-OTHER-002",
            "Outlet Customer",
            "0771234567");
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        foreach (var (type, search) in new[]
                 {
                     ("invoice", "RCP"),
                     ("mobile", "077"),
                     ("customer", "Outlet Customer"),
                     ("recent", null)
                 })
        {
            var result = await repository.SearchOriginalSalesAsync(
                tenantId,
                outletId,
                type,
                search,
                new PosReturnSaleSearchFilterDto(null, null, null, null, null),
                1,
                20,
                CancellationToken.None);

            Assert.Contains(result.Items, item => item.SaleId == matchingSaleId);
            Assert.DoesNotContain(result.Items, item => item.SaleId == otherOutletSaleId);
        }
    }

    [Fact]
    public async Task GetSaleEligibilityAsync_SameTenantCrossOutlet_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var currentOutletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-X", "Cross Outlet", "cross-outlet",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-CROSS-001", Guid.NewGuid(), null,
            "Walk-in Customer", tillId, tillSessionId, null, "LKR",
            false, 1000m, 0m, 0m, 1000m, 1000m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-CROSS-001", saleId, otherOutletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1000m, 0m, 0m, 1000m, 1000m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, productId, Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-CROSS", null, "Cross Outlet", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 1000m, 1000m, 0m, 0m, false, Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.GetSaleEligibilityAsync(
            tenantId,
            currentOutletId,
            saleId,
            Now,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_AppliesFiltersAndMasksCardReference()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cashMethodId = Guid.NewGuid();
        var cardMethodId = Guid.NewGuid();
        var cashSaleId = Guid.NewGuid();
        var cardSaleId = Guid.NewGuid();
        var businessDate = DateOnly.FromDateTime(Now.UtcDateTime);
        await using var dbContext = CreateDbContext();

        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cashMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cardMethodId, tenantId, "CARD", "Visa", "CARD",
            true, false, 2, "ACTIVE", userId, Now));

        AddSaleForSearch(
            dbContext, tenantId, cashSaleId, outletId, tillId, tillSessionId,
            userId, cashMethodId, "SO-FILTER-CASH", "RCP-FILTER-CASH",
            "Cash Customer", "0771111111");
        AddSaleForSearch(
            dbContext, tenantId, cardSaleId, outletId, tillId, tillSessionId,
            userId, cardMethodId, "SO-FILTER-CARD", "RCP-FILTER-CARD",
            "Card Customer", "0772222222");

        var cardPayment = dbContext.SalesPayments.Local
            .Single(x => x.SalesOrderId == cardSaleId);
        dbContext.SalesPaymentTransactions.Add(
            SalesPaymentTransaction.CreateCompletedProviderCapture(
                Guid.NewGuid(),
                tenantId,
                cardPayment.Id,
                1000m,
                "LKR",
                "TEST_PROVIDER",
                "txn_provider_abc123",
                "VISA",
                "4242",
                $"txn-{cardSaleId:N}",
                userId,
                Now));
        dbContext.Entry(cardPayment).Property(x => x.ExternalReference)
            .CurrentValue = "txn_provider_abc123";
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var cardResult = await repository.SearchOriginalSalesAsync(
            tenantId,
            outletId,
            "recent",
            null,
            new PosReturnSaleSearchFilterDto(
                businessDate,
                businessDate,
                "CARD",
                500m,
                1500m),
            1,
            20,
            CancellationToken.None);

        var sale = Assert.Single(cardResult.Items);
        Assert.Equal(cardSaleId, sale.SaleId);
        Assert.Equal("Visa", sale.PaymentMethod);
        Assert.Equal("•••• 4242", sale.MaskedCard);
        Assert.DoesNotContain("txn_provider", sale.MaskedCard ?? string.Empty);
        Assert.Contains(cardResult.PaymentMethods!, method => method.Code == "CARD");
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_ProviderTokenWithoutLast4_DoesNotMaskRawReference()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cardMethodId = Guid.NewGuid();
        var cardSaleId = Guid.NewGuid();
        var businessDate = DateOnly.FromDateTime(Now.UtcDateTime);
        await using var dbContext = CreateDbContext();

        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cardMethodId, tenantId, "CARD", "Visa", "CARD",
            true, false, 2, "ACTIVE", userId, Now));
        AddSaleForSearch(
            dbContext, tenantId, cardSaleId, outletId, tillId, tillSessionId,
            userId, cardMethodId, "SO-TOKEN-ONLY", "RCP-TOKEN-ONLY",
            "Card Customer", "0772222222");

        var cardPayment = dbContext.SalesPayments.Local
            .Single(x => x.SalesOrderId == cardSaleId);
        dbContext.Entry(cardPayment).Property(x => x.ExternalReference)
            .CurrentValue = "tok_live_4242424242424242";
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var cardResult = await repository.SearchOriginalSalesAsync(
            tenantId,
            outletId,
            "recent",
            null,
            new PosReturnSaleSearchFilterDto(
                businessDate,
                businessDate,
                "CARD",
                null,
                null),
            1,
            20,
            CancellationToken.None);

        var sale = Assert.Single(cardResult.Items);
        Assert.Equal("Visa", sale.PaymentMethod);
        Assert.True(string.IsNullOrEmpty(sale.MaskedCard));
        Assert.DoesNotContain("tok_live", sale.MaskedCard ?? string.Empty);
        Assert.DoesNotContain("4242", sale.MaskedCard ?? string.Empty);
    }

    [Fact]
    public async Task GetSaleEligibilityAsync_CardPayment_ReturnsSameSafeMaskedReferenceAsSearch()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cardMethodId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.ReturnPolicies.Add(ReturnPolicy.Create(
            policyId, tenantId, "DEFAULT-30", "30 Day Returns", null,
            30, 30, true, true, false, true, "ACTIVE", userId, Now));
        dbContext.Products.Add(Product.Create(
            productId, tenantId, "PROD-CARD-ELIG", "Card Product", "card-product",
            "STANDARD", "SIMPLE", null, null, policyId, null, null,
            true, true, "ACTIVE", userId, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cardMethodId, tenantId, "CARD", "Visa", "CARD",
            true, false, 1, "ACTIVE", userId, Now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, "SO-CARD-ELIG", Guid.NewGuid(), null,
            "Card Customer", tillId, tillSessionId, null, "LKR",
            false, 1200m, 0m, 0m, 1200m, 1200m, userId, Now));
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, "RCP-CARD-ELIG", saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1200m, 0m, 0m, 1200m, 1200m, 0m, "{}", Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, productId, variantId,
            Guid.NewGuid(), null, "SKU-CARD", null, "Card Product", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 1200m, 1200m, 0m, 0m, false, Now));

        var records = PosCompletedPaymentPersistence.CreateProviderCapture(
            paymentId,
            tenantId,
            saleId,
            "PAY-CARD-ELIG",
            cardMethodId,
            tillId,
            tillSessionId,
            "LKR",
            1200m,
            1200m,
            "idem-card-elig",
            "hash",
            userId,
            Now,
            new PosCompletedPaymentPersistence.ProviderCaptureOutcome(
                "TEST_TERMINAL",
                "txn_elig_ref_001",
                "MASTERCARD",
                "1288"));
        dbContext.SalesPayments.Add(records.Payment);
        dbContext.SalesPaymentTransactions.Add(records.Transaction);
        dbContext.SalesPaymentEvents.Add(records.Event);
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var eligibility = await repository.GetSaleEligibilityAsync(
            tenantId,
            outletId,
            saleId,
            Now.AddDays(1),
            CancellationToken.None);
        var search = await repository.SearchOriginalSalesAsync(
            tenantId,
            outletId,
            "invoice",
            "RCP-CARD-ELIG",
            new PosReturnSaleSearchFilterDto(null, null, null, null, null),
            1,
            20,
            CancellationToken.None);

        Assert.NotNull(eligibility);
        Assert.Equal("Mastercard", eligibility.PaymentMethod);
        Assert.Equal("•••• 1288", eligibility.MaskedCard);
        Assert.DoesNotContain("txn_elig", eligibility.MaskedCard);
        var searchSale = Assert.Single(search.Items);
        Assert.Equal(eligibility.PaymentMethod, searchSale.PaymentMethod);
        Assert.Equal(eligibility.MaskedCard, searchSale.MaskedCard);
    }

    [Fact]
    public async Task SearchOriginalSalesAsync_SplitPayments_ReturnsDeterministicMultiple()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var tillSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cashMethodId = Guid.NewGuid();
        var cardMethodId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cashMethodId, tenantId, "CASH", "Cash", "CASH",
            true, true, 1, "ACTIVE", userId, Now));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cardMethodId, tenantId, "CARD", "Visa", "CARD",
            true, false, 2, "ACTIVE", userId, Now));
        AddSaleForSearch(
            dbContext, tenantId, saleId, outletId, tillId, tillSessionId,
            userId, cashMethodId, "SO-SPLIT-001", "RCP-SPLIT-001",
            "Split Customer", "0773333333");
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, "PAY-SPLIT-2", cardMethodId,
            tillId, tillSessionId, "LKR", 500m, null, 500m, 0m,
            "split-card", "hash2", userId, Now.AddMinutes(1)));
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var result = await repository.SearchOriginalSalesAsync(
            tenantId,
            outletId,
            "recent",
            null,
            new PosReturnSaleSearchFilterDto(null, null, null, null, null),
            1,
            20,
            CancellationToken.None);

        var sale = Assert.Single(result.Items);
        Assert.Equal("Multiple", sale.PaymentMethod);
        Assert.Equal(string.Empty, sale.MaskedCard);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private static int SeedValidatedInspectionDraft(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid saleLineId,
        Guid userId,
        string conditionCode = "GOOD",
        DateTimeOffset? validatedAt = null,
        string resolutionType = "REFUND")
    {
        var draftId = Guid.NewGuid();
        var conditionId = Guid.NewGuid();
        var draftLineId = Guid.NewGuid();
        var validated = validatedAt ?? Now;

        var condition = new ReturnInspectionCondition();
        dbContext.ReturnInspectionConditions.Add(condition);
        dbContext.Entry(condition).Property(x => x.Id).CurrentValue = conditionId;
        dbContext.Entry(condition).Property(x => x.TenantId).CurrentValue = tenantId;
        dbContext.Entry(condition).Property(x => x.ConditionCode).CurrentValue = conditionCode;
        dbContext.Entry(condition).Property(x => x.DisplayName).CurrentValue = "Good";
        dbContext.Entry(condition).Property(x => x.StatusCategory).CurrentValue = "GOOD";
        dbContext.Entry(condition).Property(x => x.IsResellable).CurrentValue = true;
        dbContext.Entry(condition).Property(x => x.RefundImpact).CurrentValue = "NONE";
        dbContext.Entry(condition).Property(x => x.RequiresNotes).CurrentValue = false;
        dbContext.Entry(condition).Property(x => x.RequiresPhoto).CurrentValue = false;
        dbContext.Entry(condition).Property(x => x.RequiresApproval).CurrentValue = false;
        dbContext.Entry(condition).Property(x => x.IsActive).CurrentValue = true;
        dbContext.Entry(condition).Property(x => x.SortOrder).CurrentValue = 0;
        dbContext.Entry(condition).Property(x => x.CreatedAt).CurrentValue = Now;
        dbContext.Entry(condition).Property(x => x.UpdatedAt).CurrentValue = Now;

        var draft = ReturnInspectionDraft.Create(draftId, tenantId, outletId, saleId, userId, validated);
        draft.MarkValidated(userId, validated);
        draft.SetResolution(resolutionType, userId, validated);
        dbContext.ReturnInspectionDrafts.Add(draft);

        dbContext.ReturnInspectionDraftLines.Add(
            ReturnInspectionDraftLine.Create(
                draftLineId,
                tenantId,
                draftId,
                saleLineId,
                conditionId,
                conditionCode,
                null,
                userId,
                validated));

        return draft.Version;
    }

    private static void AddSaleForSearch(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid saleId,
        Guid outletId,
        Guid tillId,
        Guid tillSessionId,
        Guid userId,
        Guid paymentMethodId,
        string orderNumber,
        string receiptNumber,
        string customerName,
        string phone)
    {
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            saleId, tenantId, orderNumber, Guid.NewGuid(), null,
            customerName, tillId, tillSessionId, null, "LKR",
            false, 1000m, 0m, 0m, 1000m, 1000m, userId, Now));
        dbContext.Entry(dbContext.SalesOrders.Local.Single(x => x.Id == saleId))
            .Property("CustomerPhoneSnapshot")
            .CurrentValue = phone;
        dbContext.Receipts.Add(Receipt.CreateForSale(
            Guid.NewGuid(), tenantId, receiptNumber, saleId, outletId,
            tillId, tillSessionId, DateOnly.FromDateTime(Now.UtcDateTime),
            userId, "LKR", 1000m, 0m, 0m, 1000m, 1000m, 0m, "{}", Now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, saleId, $"PAY-{saleId:N}"[..16],
            paymentMethodId, tillId, tillSessionId, "LKR", 1000m, 1000m,
            1000m, 0m, $"search-{saleId:N}", "hash", userId, Now));
        dbContext.SalesOrderLines.Add(SalesOrderLine.CreateForPosSale(
            Guid.NewGuid(), tenantId, saleId, 1, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, "SKU-SEARCH", null, "Search Product", null, null, null, null, null, "EA",
            "Each", "STANDARD", "SIMPLE", 1m, 1000m, 1000m, 0m, 0m, false, Now));
    }

    [Fact]
    public async Task GetActiveReturnReasonsAsync_ReturnsDatabaseDrivenFlagsAndExcludesInactiveExchangeOnly()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        void AddReason(
            Guid ownerTenantId,
            string code,
            string name,
            string appliesTo,
            bool requiresNote,
            bool requiresInspection,
            bool requiresManagerApproval,
            bool isActive,
            int sortOrder,
            string? description)
        {
            var reason = new ReturnReason();
            dbContext.ReturnReasons.Add(reason);
            dbContext.Entry(reason).Property(x => x.Id).CurrentValue = Guid.NewGuid();
            dbContext.Entry(reason).Property(x => x.TenantId).CurrentValue = ownerTenantId;
            dbContext.Entry(reason).Property(x => x.ReasonCode).CurrentValue = code;
            dbContext.Entry(reason).Property(x => x.ReasonName).CurrentValue = name;
            dbContext.Entry(reason).Property(x => x.Description).CurrentValue = description;
            dbContext.Entry(reason).Property(x => x.AppliesTo).CurrentValue = appliesTo;
            dbContext.Entry(reason).Property(x => x.RequiresNote).CurrentValue = requiresNote;
            dbContext.Entry(reason).Property(x => x.RequiresInspection).CurrentValue = requiresInspection;
            dbContext.Entry(reason).Property(x => x.RequiresManagerApproval).CurrentValue = requiresManagerApproval;
            dbContext.Entry(reason).Property(x => x.IsActive).CurrentValue = isActive;
            dbContext.Entry(reason).Property(x => x.SortOrder).CurrentValue = sortOrder;
            dbContext.Entry(reason).Property(x => x.CreatedAt).CurrentValue = Now;
        }

        AddReason(tenantId, "OTHER", "Other", "BOTH", true, false, false, true, 2, "Provide a note");
        AddReason(tenantId, "DAMAGED", "Damaged Item", "RETURN", false, true, true, true, 0, "Damaged packaging");
        AddReason(tenantId, "EXCHANGE_ONLY", "Exchange Only", "EXCHANGE", false, false, false, true, 1, null);
        AddReason(tenantId, "INACTIVE", "Inactive", "RETURN", false, false, false, false, 3, null);
        AddReason(otherTenantId, "FOREIGN", "Foreign", "RETURN", false, false, false, true, 0, null);
        await dbContext.SaveChangesAsync();

        var repository = new PosReturnRepository(dbContext, new E_POS.Infrastructure.Modules.Tenant.POSOperations.Services.PosSaleLinePricingCalculator(dbContext));
        var reasons = await repository.GetActiveReturnReasonsAsync(tenantId, CancellationToken.None);

        Assert.Equal(2, reasons.Count);
        Assert.Equal("DAMAGED", reasons[0].Code);
        Assert.Equal("Damaged packaging", reasons[0].Description);
        Assert.False(reasons[0].RequiresNotes);
        Assert.True(reasons[0].RequiresInspection);
        Assert.True(reasons[0].RequiresManagerApproval);
        Assert.Equal("OTHER", reasons[1].Code);
        Assert.True(reasons[1].RequiresNotes);
        Assert.False(reasons[1].RequiresManagerApproval);
        Assert.DoesNotContain(reasons, x => x.Code == "EXCHANGE_ONLY");
        Assert.DoesNotContain(reasons, x => x.Code == "INACTIVE");
        Assert.DoesNotContain(reasons, x => x.Code == "FOREIGN");
    }
}
