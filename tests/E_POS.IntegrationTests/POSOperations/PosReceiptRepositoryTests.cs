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
        var request = new PosReceiptPrintRequestDto("success", 1, null);

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
        Assert.Equal(1, await dbContext.ReceiptPrintLogs.CountAsync());
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

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
