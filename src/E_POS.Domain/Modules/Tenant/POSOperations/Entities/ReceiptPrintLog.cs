using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class ReceiptPrintLog : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReceiptId { get; protected set; }
    public int AttemptNumber { get; protected set; }
    public Guid? PrinterDeviceId { get; protected set; }
    public string PrintedCopyType { get; protected set; } = string.Empty;
    public string PrintStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? PrintedAt { get; protected set; }
    public Guid? OperatorTenantUserId { get; protected set; }
    public string? ErrorCode { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public string? PrintResultJson { get; protected set; }
    public Guid? PrintRequestId { get; protected set; }
    public Guid? ReprintOperationId { get; protected set; }
    public string? ClientCorrelationId { get; protected set; }
    public string ReceiptPurpose { get; protected set; } = "SALE_ORIGINAL";
    public int CopyIndex { get; protected set; } = 1;
    public Guid? PrinterConfigurationId { get; protected set; }
    public int? PrinterConfigurationVersion { get; protected set; }
    public string? PrinterName { get; protected set; }
    public string? PrinterTransport { get; protected set; }
    public string? RoutingPurpose { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? TillSessionId { get; protected set; }
    public string? AgentResult { get; protected set; }
    public string? FailureCategory { get; protected set; }
    public bool IsReprint { get; protected set; }
    public bool UnknownOutcome { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public Guid? RecoveryPrintRequestId { get; protected set; }

    public static ReceiptPrintLog Create(
        Guid id,
        Guid tenantId,
        Guid receiptId,
        int attemptNumber,
        string printedCopyType,
        string printStatus,
        DateTimeOffset? printedAt,
        Guid? operatorTenantUserId,
        Guid? printerDeviceId,
        string? errorCode,
        string? errorMessage,
        string? printResultJson,
        DateTimeOffset now)
    {
        return new ReceiptPrintLog
        {
            Id = id,
            TenantId = tenantId,
            ReceiptId = receiptId,
            AttemptNumber = attemptNumber,
            PrintedCopyType = printedCopyType.Trim().ToUpperInvariant(),
            PrintStatus = printStatus.Trim().ToUpperInvariant(),
            PrintedAt = printedAt,
            OperatorTenantUserId = operatorTenantUserId,
            PrinterDeviceId = printerDeviceId,
            ErrorCode = errorCode?.Trim(),
            ErrorMessage = errorMessage?.Trim(),
            PrintResultJson = printResultJson,
            CreatedAt = now
        };
    }

    public void SetOperationIdentity(
        Guid? printRequestId,
        Guid? reprintOperationId,
        string? clientCorrelationId)
    {
        PrintRequestId = printRequestId;
        ReprintOperationId = reprintOperationId;
        ClientCorrelationId = string.IsNullOrWhiteSpace(clientCorrelationId)
            ? null
            : clientCorrelationId.Trim();
    }

    public void SetProductionContext(
        string receiptPurpose,
        int copyIndex,
        Guid? printerConfigurationId,
        int? printerConfigurationVersion,
        string? printerName,
        string? printerTransport,
        string? routingPurpose,
        Guid? posDeviceId,
        Guid? tillId,
        Guid? tillSessionId,
        string? agentResult,
        string? failureCategory,
        bool isReprint,
        bool unknownOutcome,
        DateTimeOffset completedAt,
        Guid? recoveryPrintRequestId)
    {
        ReceiptPurpose = receiptPurpose.Trim().ToUpperInvariant();
        CopyIndex = copyIndex;
        PrinterConfigurationId = printerConfigurationId;
        PrinterConfigurationVersion = printerConfigurationVersion;
        PrinterName = printerName?.Trim();
        PrinterTransport = printerTransport?.Trim().ToUpperInvariant();
        RoutingPurpose = routingPurpose?.Trim().ToUpperInvariant();
        PosDeviceId = posDeviceId;
        TillId = tillId;
        TillSessionId = tillSessionId;
        AgentResult = agentResult?.Trim();
        FailureCategory = failureCategory?.Trim().ToUpperInvariant();
        IsReprint = isReprint;
        UnknownOutcome = unknownOutcome;
        CompletedAt = completedAt;
        RecoveryPrintRequestId = recoveryPrintRequestId;
    }

    public void CompleteAuthorizedReprint(
        string printStatus,
        DateTimeOffset? printedAt,
        Guid? printerDeviceId,
        string? errorCode,
        string? errorMessage,
        string printResultJson)
    {
        if (PrintedCopyType != "DUPLICATE_COPY" || PrintStatus != "PENDING")
            throw new InvalidOperationException("Only a pending authorized reprint can be completed.");

        PrintStatus = printStatus.Trim().ToUpperInvariant();
        PrintedAt = printedAt;
        PrinterDeviceId = printerDeviceId;
        ErrorCode = errorCode?.Trim();
        ErrorMessage = errorMessage?.Trim();
        PrintResultJson = printResultJson;
    }
}

