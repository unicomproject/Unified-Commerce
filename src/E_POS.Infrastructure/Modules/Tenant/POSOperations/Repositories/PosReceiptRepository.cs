using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;

public sealed class PosReceiptRepository : IPosReceiptRepository
{
    private const string CustomerCopyType = "CUSTOMER_COPY";

    private readonly EPosDbContext _dbContext;

    public PosReceiptRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosReceiptPrintRepositoryResult> RecordPrintAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid saleId,
        PosReceiptPrintRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (request.Copies < 1)
        {
            return new PosReceiptPrintRepositoryResult("pos_receipts.invalid_copies", null);
        }

        if (!PosReceiptService.TryNormalizePrintStatus(request.Status, out var printStatus))
        {
            return new PosReceiptPrintRepositoryResult("pos_receipts.invalid_print_status", null);
        }

        var receipt = await _dbContext.Receipts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == saleId)
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new { x.Id, x.ReceiptNumber })
            .FirstOrDefaultAsync(cancellationToken);

        if (receipt is null)
        {
            return new PosReceiptPrintRepositoryResult("pos_receipts.receipt_not_found", null);
        }

        var nextAttemptNumber = await _dbContext.ReceiptPrintLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ReceiptId == receipt.Id)
            .Select(x => (int?)x.AttemptNumber)
            .MaxAsync(cancellationToken) ?? 0;
        nextAttemptNumber += 1;

        var printedAt = string.Equals(printStatus, "PRINTED", StringComparison.Ordinal) ? now : (DateTimeOffset?)null;
        var printResultJson = JsonSerializer.Serialize(new
        {
            status = request.Status?.Trim().ToLowerInvariant() ?? "success",
            copies = request.Copies
        });

        var printLog = ReceiptPrintLog.Create(
            Guid.NewGuid(),
            tenantId,
            receipt.Id,
            nextAttemptNumber,
            CustomerCopyType,
            printStatus,
            printedAt,
            tenantUserId,
            request.PrinterDeviceId,
            string.Equals(printStatus, "FAILED", StringComparison.Ordinal) ? "print_failed" : null,
            string.Equals(printStatus, "FAILED", StringComparison.Ordinal)
                ? "Receipt print attempt failed."
                : null,
            printResultJson,
            now);

        _dbContext.ReceiptPrintLogs.Add(printLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new PosReceiptPrintResponseDto(
            saleId,
            receipt.Id,
            receipt.ReceiptNumber,
            nextAttemptNumber,
            printStatus.ToLowerInvariant(),
            request.Copies,
            printedAt);

        return new PosReceiptPrintRepositoryResult(null, response);
    }
}
