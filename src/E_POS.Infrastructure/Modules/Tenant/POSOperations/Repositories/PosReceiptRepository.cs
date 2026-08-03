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

    public async Task<PosReceiptSearchResponseDto> SearchAsync(
        Guid tenantId,
        PosReceiptSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var query =
            from receipt in _dbContext.Receipts.AsNoTracking()
            join order in _dbContext.SalesOrders.AsNoTracking()
                on receipt.SalesOrderId equals order.Id
            join outlet in _dbContext.Outlets.AsNoTracking()
                on receipt.OutletId equals outlet.Id
            join till in _dbContext.Tills.AsNoTracking()
                on receipt.TillId equals till.Id
            join cashier in _dbContext.TenantUsers.AsNoTracking()
                on receipt.IssuedByTenantUserId equals cashier.Id
            where receipt.TenantId == tenantId
            select new
            {
                Receipt = receipt,
                OrderNumber = order.OrderNumber,
                OutletName = outlet.OutletName,
                TillName = till.TillName,
                CashierName = cashier.DisplayName ?? cashier.FullName,
                PaymentMethod = (
                    from payment in _dbContext.SalesPayments
                    join method in _dbContext.PaymentMethods on payment.PaymentMethodId equals method.Id
                    where payment.TenantId == tenantId && payment.SalesOrderId == receipt.SalesOrderId
                    orderby payment.PaidAt descending
                    select method.MethodName).FirstOrDefault() ?? "Unknown"
            };

        var text = request.Query?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.Receipt.ReceiptNumber, $"%{text}%") ||
                EF.Functions.ILike(x.OrderNumber, $"%{text}%"));
        }
        if (request.DateFrom.HasValue) query = query.Where(x => x.Receipt.BusinessDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(x => x.Receipt.BusinessDate <= request.DateTo.Value);
        if (request.CashierUserId.HasValue) query = query.Where(x => x.Receipt.IssuedByTenantUserId == request.CashierUserId.Value);
        if (request.TillId.HasValue) query = query.Where(x => x.Receipt.TillId == request.TillId.Value);
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            query = query.Where(x => EF.Functions.ILike(x.PaymentMethod, request.PaymentMethod.Trim()));
        if (!string.IsNullOrWhiteSpace(request.ReceiptType))
            query = query.Where(x => x.Receipt.ReceiptType == request.ReceiptType.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(request.ReceiptStatus))
            query = query.Where(x => x.Receipt.ReceiptStatus == request.ReceiptStatus.Trim().ToUpper());
        if (request.MinAmount.HasValue) query = query.Where(x => x.Receipt.TotalAmount >= request.MinAmount.Value);
        if (request.MaxAmount.HasValue) query = query.Where(x => x.Receipt.TotalAmount <= request.MaxAmount.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.Receipt.IssuedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Receipt.Id,
                SaleId = x.Receipt.SalesOrderId,
                x.Receipt.ReceiptNumber,
                x.OrderNumber,
                x.Receipt.ReceiptType,
                x.Receipt.ReceiptStatus,
                x.Receipt.IssuedAt,
                x.CashierName,
                x.TillName,
                x.OutletName,
                x.PaymentMethod,
                x.Receipt.CurrencyCode,
                x.Receipt.TotalAmount,
                ReprintCount = _dbContext.ReceiptPrintLogs.Count(log =>
                    log.TenantId == tenantId &&
                    log.ReceiptId == x.Receipt.Id &&
                    log.IsReprint &&
                    log.PrintStatus == "PRINTED")
            })
            .ToListAsync(cancellationToken);

        return new PosReceiptSearchResponseDto(
            rows.Select(x => new PosReceiptHistoryItemDto(
                x.Id, x.SaleId, x.ReceiptNumber, x.OrderNumber, x.ReceiptType,
                x.ReceiptStatus, x.IssuedAt, x.CashierName, x.TillName,
                x.OutletName, x.PaymentMethod, x.CurrencyCode, x.TotalAmount,
                x.ReprintCount)).ToList(),
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<PosReceiptDetailDto?> GetDetailAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from receipt in _dbContext.Receipts.AsNoTracking()
            join order in _dbContext.SalesOrders.AsNoTracking() on receipt.SalesOrderId equals order.Id
            join outlet in _dbContext.Outlets.AsNoTracking() on receipt.OutletId equals outlet.Id
            join till in _dbContext.Tills.AsNoTracking() on receipt.TillId equals till.Id
            join cashier in _dbContext.TenantUsers.AsNoTracking() on receipt.IssuedByTenantUserId equals cashier.Id
            join tenant in _dbContext.Tenants.AsNoTracking() on receipt.TenantId equals tenant.Id
            where receipt.TenantId == tenantId && receipt.Id == receiptId
            select new
            {
                Receipt = receipt,
                SaleNumber = order.OrderNumber,
                OutletName = outlet.OutletName,
                TillName = till.TillName,
                CashierName = cashier.DisplayName ?? cashier.FullName,
                MerchantName = tenant.DisplayName,
                PaymentMethod = (
                    from payment in _dbContext.SalesPayments
                    join method in _dbContext.PaymentMethods on payment.PaymentMethodId equals method.Id
                    where payment.TenantId == tenantId && payment.SalesOrderId == receipt.SalesOrderId
                    orderby payment.PaidAt descending
                    select method.MethodName).FirstOrDefault() ?? "Unknown"
            }).SingleOrDefaultAsync(cancellationToken);

        if (row is null) return null;

        var logs = await _dbContext.ReceiptPrintLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ReceiptId == receiptId &&
                        x.IsReprint && x.PrintStatus == "PRINTED")
            .Select(x => x.PrintedAt)
            .ToListAsync(cancellationToken);

        var snapshot = ParseReceiptSnapshot(row.Receipt.ReceiptDataJson);
        return new PosReceiptDetailDto(
            row.Receipt.Id,
            row.Receipt.SalesOrderId,
            row.Receipt.ReceiptNumber,
            row.SaleNumber,
            row.Receipt.ReceiptType,
            row.Receipt.ReceiptStatus,
            row.Receipt.IssuedAt,
            row.CashierName,
            row.Receipt.IssuedByTenantUserId,
            row.TillName,
            row.Receipt.TillId,
            row.OutletName,
            row.Receipt.OutletId,
            row.PaymentMethod,
            row.Receipt.CurrencyCode,
            row.Receipt.SubtotalAmount,
            row.Receipt.DiscountAmount,
            row.Receipt.TaxAmount,
            row.Receipt.ChargeAmount,
            row.Receipt.RoundingAmount,
            row.Receipt.TotalAmount,
            row.Receipt.PaidAmount,
            row.Receipt.ChangeAmount,
            ParseLines(row.Receipt.ReceiptDataJson),
            logs.Count,
            logs.Max(),
            row.MerchantName,
            snapshot.Tenders,
            snapshot.DiscountLines,
            snapshot.TaxLines,
            snapshot.CopyPolicy,
            snapshot.TaxRegistrationNumber,
            snapshot.TaxInvoiceLabel,
            ParseHistoricalSnapshot(row.Receipt.ReceiptDataJson));
    }

    public async Task<PosReceiptReprintAuthorizationResponseDto?> AuthorizeReprintAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid receiptId,
        string reasonCode,
        string? reasonNote,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Receipts.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId && x.Id == receiptId &&
                 x.ReceiptStatus == "ISSUED",
            cancellationToken);
        if (!exists) return null;

        var attempt = (await _dbContext.ReceiptPrintLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ReceiptId == receiptId)
            .MaxAsync(x => (int?)x.AttemptNumber, cancellationToken) ?? 0) + 1;
        var operationId = Guid.NewGuid();
        var resultJson = JsonSerializer.Serialize(new
        {
            reprintOperationId = operationId,
            reprintReasonCode = reasonCode,
            reprintReasonNote = SafeText(reasonNote, 500),
            authorizationStatus = "AUTHORIZED",
            authorizedAt = now
        });
        var authorizationLog = ReceiptPrintLog.Create(
            Guid.NewGuid(), tenantId, receiptId, attempt, "DUPLICATE_COPY",
            "PENDING", null, tenantUserId, null, null, null, resultJson, now);
        authorizationLog.SetOperationIdentity(null, operationId, operationId.ToString());
        _dbContext.ReceiptPrintLogs.Add(authorizationLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PosReceiptReprintAuthorizationResponseDto(
            receiptId, operationId, true, false, "AUTHORIZED",
            "Reprint authorized.", now);
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

        var receiptPurpose = NormalizeReceiptPurpose(request.ReceiptPurpose, request.IsReprint);
        if (receiptPurpose is null)
            return new PosReceiptPrintRepositoryResult("pos_receipts.invalid_receipt_purpose", null);

        var receipts = await _dbContext.Receipts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SalesOrderId == saleId &&
                        (request.ReceiptId == null || x.Id == request.ReceiptId))
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new
            {
                x.Id, x.ReceiptNumber, x.ReceiptStatus, x.ReceiptType,
                x.TillId, x.TillSessionId
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        var expectedReceiptType = receiptPurpose switch
        {
            "REFUND" or "RETURN" => "REFUND",
            "EXCHANGE" => "EXCHANGE",
            _ => "SALE"
        };
        var receipt = receipts.FirstOrDefault(x =>
            string.Equals(x.ReceiptType, expectedReceiptType, StringComparison.OrdinalIgnoreCase));

        if (receipt is null)
        {
            return new PosReceiptPrintRepositoryResult("pos_receipts.receipt_not_found", null);
        }

        if (!string.Equals(receipt.ReceiptStatus, "ISSUED", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(receipt.ReceiptStatus, "PRINTED", StringComparison.OrdinalIgnoreCase))
        {
            return new PosReceiptPrintRepositoryResult("pos_receipts.receipt_not_completed", null);
        }
        var requestedCopyType = request.CopyType?.Trim().ToUpperInvariant() ?? "CUSTOMER";
        if (requestedCopyType is not ("CUSTOMER" or "MERCHANT") ||
            request.CopyIndex is < 1 or > 5)
            return new PosReceiptPrintRepositoryResult("pos_receipts.invalid_copy_identity", null);

        if (request.PrinterConfigurationVersion is < 1)
            return new PosReceiptPrintRepositoryResult("pos_receipts.invalid_printer_configuration", null);
        if (request.PrinterConfigurationId is { } printerConfigurationId)
        {
            var validConfiguration = await (
                from hardware in _dbContext.HardwareDevices.AsNoTracking()
                join assignment in _dbContext.HardwareDeviceAssignments.AsNoTracking()
                    on hardware.Id equals assignment.HardwareDeviceId
                where hardware.TenantId == tenantId &&
                      hardware.Id == printerConfigurationId &&
                      hardware.HardwareDeviceType == "RECEIPTPRINTER" &&
                      hardware.Status == "ACTIVE" &&
                      hardware.ConfigurationVersion == request.PrinterConfigurationVersion &&
                      assignment.ReleasedAt == null &&
                      assignment.PosDeviceId == request.DeviceId
                select hardware.Id).AnyAsync(cancellationToken);
            if (!validConfiguration)
                return new PosReceiptPrintRepositoryResult(
                    "pos_receipts.invalid_printer_configuration", null);
        }

        if (request.PrintRequestId is { } requestId)
        {
            var existing = await _dbContext.ReceiptPrintLogs.AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.ReceiptId == receipt.Id &&
                            x.PrintRequestId == requestId)
                .OrderByDescending(x => x.AttemptNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null)
            {
                var legacyLogs = await _dbContext.ReceiptPrintLogs.AsNoTracking()
                    .Where(x => x.TenantId == tenantId &&
                                x.ReceiptId == receipt.Id &&
                                x.PrintRequestId == null &&
                                x.PrintResultJson != null)
                    .OrderByDescending(x => x.AttemptNumber)
                    .ToListAsync(cancellationToken);
                existing = legacyLogs.FirstOrDefault(
                    x => JsonGuidEquals(x.PrintResultJson, "printRequestId", requestId));
            }

            if (existing is not null)
            {
                return new PosReceiptPrintRepositoryResult(null,
                    new PosReceiptPrintResponseDto(
                        saleId,
                        receipt.Id,
                        receipt.ReceiptNumber,
                        existing.AttemptNumber,
                        existing.PrintStatus.ToLowerInvariant(),
                        request.Copies,
                        existing.PrintedAt));
            }
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
            copies = request.Copies,
            deviceId = request.DeviceId,
            tillId = request.TillId,
            cashierUserId = request.CashierUserId,
            printerTransport = SafeText(request.PrinterTransport, 40),
            configuredPrinterName = SafeText(request.ConfiguredPrinterName, 160),
            printRequestId = request.PrintRequestId,
            requestedAt = request.RequestedAt,
            agentResult = SafeText(request.AgentResult, 160),
            failureCategory = SafeText(request.FailureCategory, 80),
            request.IsRetry,
            request.IsReprint,
            clientCorrelationId = SafeText(request.ClientCorrelationId, 160),
            reprintOperationId = request.ReprintOperationId,
            reprintReasonCode = SafeText(request.ReprintReasonCode, 80),
            reprintReasonNote = SafeText(request.ReprintReasonNote, 500),
            copyType = requestedCopyType,
            request.CopyIndex
            ,
            receiptId = request.ReceiptId,
            receiptPurpose,
            printerConfigurationId = request.PrinterConfigurationId,
            printerConfigurationVersion = request.PrinterConfigurationVersion,
            routingPurpose = SafeText(request.RoutingPurpose, 40),
            request.UnknownOutcome,
            recoveryPrintRequestId = request.RecoveryPrintRequestId
        });

        if (request.IsReprint)
        {
            var authorizedLog = await _dbContext.ReceiptPrintLogs.SingleOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ReceiptId == receipt.Id &&
                     x.ReprintOperationId == request.ReprintOperationId &&
                     x.PrintedCopyType == "DUPLICATE_COPY" &&
                     x.PrintStatus == "PENDING",
                cancellationToken);
            if (authorizedLog is null)
            {
                var legacyLogs = await _dbContext.ReceiptPrintLogs
                    .Where(x => x.TenantId == tenantId &&
                                x.ReceiptId == receipt.Id &&
                                x.ReprintOperationId == null &&
                                x.PrintResultJson != null)
                    .ToListAsync(cancellationToken);
                authorizedLog = legacyLogs.SingleOrDefault(
                    x => JsonGuidEquals(
                        x.PrintResultJson,
                        "reprintOperationId",
                        request.ReprintOperationId!.Value));
            }

            if (authorizedLog is null)
                return new PosReceiptPrintRepositoryResult("pos_receipts.reprint_not_authorized", null);
        }

        var printLog = ReceiptPrintLog.Create(
            Guid.NewGuid(),
            tenantId,
            receipt.Id,
            nextAttemptNumber,
            request.IsReprint
                ? requestedCopyType == "MERCHANT"
                    ? "DUPLICATE_MERCHANT_COPY"
                    : "DUPLICATE_CUSTOMER_COPY"
                : requestedCopyType == "MERCHANT"
                    ? "MERCHANT_COPY"
                    : CustomerCopyType,
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
        printLog.SetOperationIdentity(
            request.PrintRequestId,
            request.ReprintOperationId,
            request.ClientCorrelationId);
        printLog.SetProductionContext(
            receiptPurpose, request.CopyIndex,
            request.PrinterConfigurationId, request.PrinterConfigurationVersion,
            SafeText(request.ConfiguredPrinterName, 160),
            SafeText(request.PrinterTransport, 40),
            SafeText(request.RoutingPurpose, 40),
            request.DeviceId, request.TillId ?? receipt.TillId, receipt.TillSessionId,
            SafeText(request.AgentResult, 160),
            SafeText(request.FailureCategory, 80),
            request.IsReprint, request.UnknownOutcome, now,
            request.RecoveryPrintRequestId);

        _dbContext.ReceiptPrintLogs.Add(printLog);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (request.PrintRequestId is not null)
        {
            _dbContext.ChangeTracker.Clear();
            var concurrent = await _dbContext.ReceiptPrintLogs.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.TenantId == tenantId &&
                         x.ReceiptId == receipt.Id &&
                         x.PrintRequestId == request.PrintRequestId,
                    cancellationToken);
            if (concurrent is null) throw;
            return new PosReceiptPrintRepositoryResult(null,
                new PosReceiptPrintResponseDto(
                    saleId,
                    receipt.Id,
                    receipt.ReceiptNumber,
                    concurrent.AttemptNumber,
                    concurrent.PrintStatus.ToLowerInvariant(),
                    request.Copies,
                    concurrent.PrintedAt));
        }

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

    private static bool JsonGuidEquals(string? json, string propertyName, Guid expected)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind == JsonValueKind.String &&
                   Guid.TryParse(property.GetString(), out var actual) &&
                   actual == expected;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? SafeText(string? value, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string? NormalizeReceiptPurpose(string? value, bool isReprint)
    {
        var normalized = value?.Trim().Replace("_", string.Empty)
            .Replace("-", string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return isReprint ? "SALE_REPRINT" : "SALE_ORIGINAL";
        return normalized switch
        {
            "SALEORIGINAL" => "SALE_ORIGINAL",
            "SALEREPRINT" => "SALE_REPRINT",
            "RETURN" => "RETURN",
            "EXCHANGE" => "EXCHANGE",
            "REFUND" => "REFUND",
            _ => null
        };
    }

    private static IReadOnlyList<PosReceiptDetailLineDto> ParseLines(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<PosReceiptDetailLineDto>();
            }

            return items.EnumerateArray().Select(item => new PosReceiptDetailLineDto(
                ReadText(item, "name") ?? "Item",
                ReadText(item, "sku"),
                ReadDecimal(item, "quantity"),
                ReadDecimal(item, "unitPrice"),
                ReadDecimal(item, "lineTotal"),
                ReadGuid(item, "saleLineId"))).ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<PosReceiptDetailLineDto>();
        }
    }

    private static ReceiptContractSnapshot ParseReceiptSnapshot(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReceiptContractSnapshot>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
                   ReceiptContractSnapshot.Empty;
        }
        catch (JsonException)
        {
            return ReceiptContractSnapshot.Empty;
        }
    }

    private static JsonElement? ParseHistoricalSnapshot(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ReceiptContractSnapshot(
        IReadOnlyList<PosReceiptTenderLineDto>? Tenders,
        IReadOnlyList<PosReceiptDiscountLineDto>? DiscountLines,
        IReadOnlyList<PosReceiptTaxLineDto>? TaxLines,
        PosReceiptCopyPolicyDto? CopyPolicy,
        string? TaxRegistrationNumber,
        string? TaxInvoiceLabel)
    {
        public static ReceiptContractSnapshot Empty { get; } =
            new([], [], [], null, null, null);
    }

    private static string? ReadText(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal ReadDecimal(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.TryGetDecimal(out var result)
            ? result
            : 0m;

    private static Guid ReadGuid(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        Guid.TryParse(value.GetString(), out var result)
            ? result
            : Guid.Empty;
}
