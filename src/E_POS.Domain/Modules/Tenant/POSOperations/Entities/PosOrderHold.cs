using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class PosOrderHold : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string HoldNumber { get; protected set; } = string.Empty;
    public Guid SalesOrderId { get; protected set; }
    public string HoldStatus { get; protected set; } = string.Empty;
    public string? HoldReason { get; protected set; }
    public Guid HeldByTenantUserId { get; protected set; }
    public DateTimeOffset HeldAt { get; protected set; }
    public Guid? ReleasedByTenantUserId { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }

    /// <summary>Client-supplied park idempotency key (tenant-unique when present).</summary>
    public string? IdempotencyKey { get; protected set; }

    /// <summary>Hash of the create request payload used with <see cref="IdempotencyKey"/>.</summary>
    public string? RequestFingerprint { get; protected set; }

    public static PosOrderHold Create(
        Guid id,
        Guid tenantId,
        string holdNumber,
        Guid salesOrderId,
        string? reason,
        Guid heldByTenantUserId,
        DateTimeOffset heldAt,
        DateTimeOffset? expiresAt,
        string? idempotencyKey = null,
        string? requestFingerprint = null)
    {
        return new PosOrderHold
        {
            Id = id,
            TenantId = tenantId,
            HoldNumber = holdNumber.Trim(),
            SalesOrderId = salesOrderId,
            HoldStatus = "HELD",
            HoldReason = reason?.Trim(),
            HeldByTenantUserId = heldByTenantUserId,
            HeldAt = heldAt,
            ExpiresAt = expiresAt,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : idempotencyKey.Trim(),
            RequestFingerprint = string.IsNullOrWhiteSpace(requestFingerprint)
                ? null
                : requestFingerprint.Trim(),
            CreatedAt = heldAt,
            UpdatedAt = heldAt
        };
    }
}
