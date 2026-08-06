using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosReceiptRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordPrintAsync_WithExistingReceipt_CreatesPrintLog()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Receipts.Add(Receipt.CreateForSale(
            receiptId,
            tenantId,
            "RCP-000001",
            saleId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            userId,
            "LKR",
            1000m,
            0m,
            0m,
            1000m,
            1000m,
            0m,
            "{}",
            Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReceiptRepository(dbContext);
        var printRequestId = Guid.NewGuid();
        var request = new PosReceiptPrintRequestDto(
            Status: "success",
            Copies: 1,
            PrinterDeviceId: null,
            DeviceId: Guid.NewGuid(),
            TillId: Guid.NewGuid(),
            PrinterTransport: "localPrintAgent",
            ConfiguredPrinterName: "Configured printer",
            PrintRequestId: printRequestId,
            AgentResult: "printed",
            CopyType: "CUSTOMER",
            CopyIndex: 1,
            ReceiptId: receiptId,
            ReceiptPurpose: "saleOriginal",
            RoutingPurpose: "customerReceipt");

        var result = await repository.RecordPrintAsync(
            tenantId,
            userId,
            saleId,
            request,
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Print);
        Assert.Equal(saleId, result.Print!.SaleId);
        Assert.Equal(receiptId, result.Print.ReceiptId);
        Assert.Equal(1, result.Print.AttemptNumber);
        Assert.Equal("printed", result.Print.PrintStatus);
        var log = await dbContext.ReceiptPrintLogs.SingleAsync();
        Assert.Equal("SALE_ORIGINAL", log.ReceiptPurpose);
        Assert.Equal(1, log.CopyIndex);
        Assert.Equal("CUSTOMERRECEIPT", log.RoutingPurpose);
        Assert.Equal("LOCALPRINTAGENT", log.PrinterTransport);
        Assert.Equal(printRequestId, log.PrintRequestId);
    }

    [Fact]
    public async Task RecordPrintAsync_WhenReceiptMissing_ReturnsFailure()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PosReceiptRepository(dbContext);

        var result = await repository.RecordPrintAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosReceiptPrintRequestDto("success", 1, null),
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_receipts.receipt_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task RecordPrintAsync_OnRepeatedPrint_IncrementsAttemptNumber()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Receipts.Add(Receipt.CreateForSale(
            receiptId,
            tenantId,
            "RCP-000002",
            saleId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            userId,
            "LKR",
            500m,
            0m,
            0m,
            500m,
            500m,
            0m,
            "{}",
            Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReceiptRepository(dbContext);
        var request = new PosReceiptPrintRequestDto("success", 1, null);

        var first = await repository.RecordPrintAsync(
            tenantId,
            userId,
            saleId,
            request,
            Now,
            CancellationToken.None);
        var second = await repository.RecordPrintAsync(
            tenantId,
            userId,
            saleId,
            request,
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.Equal(1, first.Print!.AttemptNumber);
        Assert.Equal(2, second.Print!.AttemptNumber);
        Assert.Equal(2, await dbContext.ReceiptPrintLogs.CountAsync());
    }

    [Fact]
    public async Task RecordPrintAsync_ForAuthorizedReprint_UsesOperationIdentityColumn()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Receipts.Add(CreateReceipt(receiptId, tenantId, saleId, userId, "RCP-REPRINT-001"));
        await dbContext.SaveChangesAsync();

        var repository = new PosReceiptRepository(dbContext);
        var authorization = await repository.AuthorizeReprintAsync(
            tenantId,
            userId,
            receiptId,
            "CUSTOMER_REQUEST",
            null,
            Now,
            CancellationToken.None);

        Assert.NotNull(authorization);

        var result = await repository.RecordPrintAsync(
            tenantId,
            userId,
            saleId,
            new PosReceiptPrintRequestDto(
                Status: "success",
                Copies: 1,
                PrinterDeviceId: null,
                IsReprint: true,
                ReprintOperationId: authorization!.OperationId,
                PrintRequestId: Guid.NewGuid(),
                CopyType: "CUSTOMER",
                ReceiptPurpose: "saleReprint"),
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("printed", result.Print!.PrintStatus);
        var logs = await dbContext.ReceiptPrintLogs.OrderBy(x => x.AttemptNumber).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Equal("PENDING", logs[0].PrintStatus);
        Assert.Equal("DUPLICATE_COPY", logs[0].PrintedCopyType);
        Assert.Equal("PRINTED", logs[1].PrintStatus);
        Assert.Equal("DUPLICATE_CUSTOMER_COPY", logs[1].PrintedCopyType);
        Assert.Equal(authorization.OperationId, logs[1].ReprintOperationId);
    }

    [Fact]
    public async Task RecordPrintAsync_ForLegacyAuthorizedReprint_ReadsOperationFromJson()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Receipts.Add(CreateReceipt(receiptId, tenantId, saleId, userId, "RCP-REPRINT-002"));
        dbContext.ReceiptPrintLogs.Add(ReceiptPrintLog.Create(
            Guid.NewGuid(),
            tenantId,
            receiptId,
            1,
            "DUPLICATE_COPY",
            "PENDING",
            null,
            userId,
            null,
            null,
            null,
            $$"""{"reprintOperationId":"{{operationId}}","authorizationStatus":"AUTHORIZED"}""",
            Now));
        await dbContext.SaveChangesAsync();

        var repository = new PosReceiptRepository(dbContext);
        var result = await repository.RecordPrintAsync(
            tenantId,
            userId,
            saleId,
            new PosReceiptPrintRequestDto(
                Status: "success",
                Copies: 1,
                PrinterDeviceId: null,
                IsReprint: true,
                ReprintOperationId: operationId,
                PrintRequestId: Guid.NewGuid(),
                CopyType: "CUSTOMER",
                ReceiptPurpose: "saleReprint"),
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("printed", result.Print!.PrintStatus);
        var logs = await dbContext.ReceiptPrintLogs.OrderBy(x => x.AttemptNumber).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Null(logs[0].ReprintOperationId);
        Assert.Equal(operationId, logs[1].ReprintOperationId);
        Assert.Equal("PRINTED", logs[1].PrintStatus);
    }

    [Fact]
    public async Task RecordPrintAsync_ForAuthorizedReprint_CreatesIndependentCopyAudits()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.Receipts.Add(CreateReceipt(receiptId, tenantId, saleId, userId, "RCP-REPRINT-003"));
        await dbContext.SaveChangesAsync();
        var repository = new PosReceiptRepository(dbContext);
        var authorization = await repository.AuthorizeReprintAsync(
            tenantId, userId, receiptId, "CUSTOMER_REQUEST", null, Now,
            CancellationToken.None);

        var customerRequestId = Guid.NewGuid();
        var merchantRequestId = Guid.NewGuid();
        foreach (var copy in new[]
                 {
                     (Type: "CUSTOMER", Index: 1, RequestId: customerRequestId),
                     (Type: "MERCHANT", Index: 1, RequestId: merchantRequestId)
                 })
        {
            var result = await repository.RecordPrintAsync(
                tenantId,
                userId,
                saleId,
                new PosReceiptPrintRequestDto(
                    Status: "success",
                    Copies: 1,
                    PrinterDeviceId: null,
                    PrintRequestId: copy.RequestId,
                    IsReprint: true,
                    ReprintOperationId: authorization!.OperationId,
                    CopyType: copy.Type,
                    CopyIndex: copy.Index,
                    ReceiptId: receiptId,
                    ReceiptPurpose: "saleReprint"),
                Now.AddSeconds(copy.Type == "CUSTOMER" ? 1 : 2),
                CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        var physicalLogs = await dbContext.ReceiptPrintLogs
            .Where(x => x.IsReprint)
            .OrderBy(x => x.AttemptNumber)
            .ToListAsync();
        Assert.Equal(2, physicalLogs.Count);
        Assert.Equal(
            new[] { "DUPLICATE_CUSTOMER_COPY", "DUPLICATE_MERCHANT_COPY" },
            physicalLogs.Select(x => x.PrintedCopyType));
        Assert.Equal(
            new Guid?[] { customerRequestId, merchantRequestId },
            physicalLogs.Select(x => x.PrintRequestId));
        Assert.All(physicalLogs,
            log => Assert.Equal(authorization!.OperationId, log.ReprintOperationId));
    }

    [Theory]
    [InlineData("RETURN", "REFUND")]
    [InlineData("REFUND", "REFUND")]
    [InlineData("EXCHANGE", "EXCHANGE")]
    public async Task RecordPrintAsync_AuthorizesEachNonSaleHistoricalPurpose(
        string purpose,
        string receiptType)
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Receipts.Add(CreateNonSaleReceipt(
            receiptType, receiptId, tenantId, saleId, userId));
        await dbContext.SaveChangesAsync();
        var repository = new PosReceiptRepository(dbContext);
        var authorization = await repository.AuthorizeReprintAsync(
            tenantId, userId, receiptId, "CUSTOMER_REQUEST", null, Now,
            CancellationToken.None);

        var result = await repository.RecordPrintAsync(
            tenantId,
            userId,
            saleId,
            new PosReceiptPrintRequestDto(
                Status: "success",
                Copies: 1,
                PrinterDeviceId: null,
                PrintRequestId: Guid.NewGuid(),
                IsReprint: true,
                ReprintOperationId: authorization!.OperationId,
                CopyType: "CUSTOMER",
                ReceiptId: receiptId,
                ReceiptPurpose: purpose),
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var physical = await dbContext.ReceiptPrintLogs.SingleAsync(x => x.IsReprint);
        Assert.Equal(purpose, physical.ReceiptPurpose);
        Assert.Equal("DUPLICATE_CUSTOMER_COPY", physical.PrintedCopyType);
    }

    private static Receipt CreateReceipt(
        Guid receiptId,
        Guid tenantId,
        Guid saleId,
        Guid userId,
        string receiptNumber) =>
        Receipt.CreateForSale(
            receiptId,
            tenantId,
            receiptNumber,
            saleId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            userId,
            "LKR",
            1000m,
            0m,
            0m,
            1000m,
            1000m,
            0m,
            "{}",
            Now);

    private static Receipt CreateNonSaleReceipt(
        string receiptType,
        Guid receiptId,
        Guid tenantId,
        Guid saleId,
        Guid userId)
    {
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var businessDate = DateOnly.FromDateTime(Now.UtcDateTime);
        const string snapshot =
            """{"originalSaleId":"00000000-0000-0000-0000-000000000001","originalInvoiceNo":"SALE-1","items":[{"name":"Item","quantity":1,"unitPrice":100,"lineTotal":100}]}""";
        return receiptType == "EXCHANGE"
            ? Receipt.CreateForExchange(
                receiptId, tenantId, "EX-1", saleId, outletId, tillId,
                sessionId, businessDate, userId, "LKR", 100m, 0m, 0m,
                100m, 0m, 0m, snapshot, Now)
            : Receipt.CreateForRefund(
                receiptId, tenantId, "RF-1", saleId, outletId, tillId,
                sessionId, businessDate, userId, "LKR", 100m, 0m, 0m,
                100m, snapshot, Now);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
