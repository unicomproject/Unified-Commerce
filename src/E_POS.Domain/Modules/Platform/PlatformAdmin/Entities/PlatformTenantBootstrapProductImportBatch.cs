using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public sealed class PlatformTenantBootstrapProductImportBatch : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string Status { get; private set; } = "VALIDATED";
    public string TemplateVersion { get; private set; } = "OVZ-ST-PRODUCT-IMPORT-v1";
    public string SourceFileName { get; private set; } = string.Empty;
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int InvalidRows { get; private set; }
    public int CommittedRows { get; private set; }
    public int SkippedRows { get; private set; }
    public string? IdempotencyKeyHash { get; private set; }
    public Guid? ActorPlatformUserId { get; private set; }

    public static PlatformTenantBootstrapProductImportBatch CreateValidated(
        Guid id,
        Guid tenantId,
        string sourceFileName,
        int totalRows,
        int validRows,
        int invalidRows,
        Guid actorPlatformUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Status = "VALIDATED",
            SourceFileName = sourceFileName,
            TotalRows = totalRows,
            ValidRows = validRows,
            InvalidRows = invalidRows,
            ActorPlatformUserId = actorPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void MarkCommitting(DateTimeOffset now)
    {
        Status = "COMMITTING";
        UpdatedAt = now;
    }

    public void MarkCommitted(int committedRows, int skippedRows, string idempotencyKeyHash, DateTimeOffset now)
    {
        Status = "COMMITTED";
        CommittedRows = committedRows;
        SkippedRows = skippedRows;
        IdempotencyKeyHash = idempotencyKeyHash;
        UpdatedAt = now;
    }
}

public sealed class PlatformTenantBootstrapProductImportRow : AuditableEntity
{
    public Guid ImportBatchId { get; private set; }
    public Guid TenantId { get; private set; }
    public int RowNumber { get; private set; }
    public string RawRowJson { get; private set; } = string.Empty;
    public bool IsValid { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorDetail { get; private set; }
    public Guid? CommittedProductId { get; private set; }

    public static PlatformTenantBootstrapProductImportRow Create(
        Guid id,
        Guid importBatchId,
        Guid tenantId,
        int rowNumber,
        string rawRowJson,
        bool isValid,
        string? errorCode,
        string? errorDetail,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            ImportBatchId = importBatchId,
            TenantId = tenantId,
            RowNumber = rowNumber,
            RawRowJson = rawRowJson,
            IsValid = isValid,
            ErrorCode = errorCode,
            ErrorDetail = errorDetail,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void MarkCommitted(Guid productId, DateTimeOffset now)
    {
        CommittedProductId = productId;
        UpdatedAt = now;
    }
}

public sealed class PlatformTenantBootstrapIdempotencyRecord : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string OperationType { get; private set; } = string.Empty;
    public string IdempotencyKeyHash { get; private set; } = string.Empty;
    public string? RequestHash { get; private set; }
    public string ResponseJson { get; private set; } = string.Empty;

    public static PlatformTenantBootstrapIdempotencyRecord Create(
        Guid id,
        Guid tenantId,
        string operationType,
        string idempotencyKeyHash,
        string responseJson,
        DateTimeOffset now,
        string? requestHash = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            OperationType = operationType,
            IdempotencyKeyHash = idempotencyKeyHash,
            RequestHash = requestHash,
            ResponseJson = responseJson,
            CreatedAt = now,
            UpdatedAt = now
        };
}
