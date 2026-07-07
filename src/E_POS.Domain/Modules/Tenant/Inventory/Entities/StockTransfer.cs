using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockTransfer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string TransferNumber { get; protected set; } = string.Empty;
    public Guid SourceLocationId { get; protected set; }
    public Guid DestinationLocationId { get; protected set; }
    public string TransferStatus { get; protected set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public DateTimeOffset? ShippedAt { get; protected set; }
    public DateTimeOffset? ReceivedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public Guid? ShippedByTenantUserId { get; protected set; }
    public Guid? ReceivedByTenantUserId { get; protected set; }
    public Guid? CancelledByTenantUserId { get; protected set; }
    public string? TransferNote { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected StockTransfer() { }

    public static StockTransfer Create(
        Guid id,
        Guid tenantId,
        string transferNumber,
        Guid sourceLocationId,
        Guid destinationLocationId,
        string transferNote,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new StockTransfer
        {
            Id = id,
            TenantId = tenantId,
            TransferNumber = transferNumber.Trim(),
            SourceLocationId = sourceLocationId,
            DestinationLocationId = destinationLocationId,
            TransferStatus = "REQUESTED",
            RequestedAt = now,
            TransferNote = transferNote?.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Approve(Guid approvedByTenantUserId, DateTimeOffset now)
    {
        TransferStatus = "APPROVED";
        ApprovedByTenantUserId = approvedByTenantUserId;
        ApprovedAt = now;
        UpdatedByTenantUserId = approvedByTenantUserId;
        UpdatedAt = now;
    }

    public void Ship(Guid shippedByTenantUserId, DateTimeOffset now)
    {
        TransferStatus = "SHIPPED";
        ShippedByTenantUserId = shippedByTenantUserId;
        ShippedAt = now;
        UpdatedByTenantUserId = shippedByTenantUserId;
        UpdatedAt = now;
    }

    public void Receive(Guid receivedByTenantUserId, DateTimeOffset now)
    {
        TransferStatus = "RECEIVED";
        ReceivedByTenantUserId = receivedByTenantUserId;
        ReceivedAt = now;
        UpdatedByTenantUserId = receivedByTenantUserId;
        UpdatedAt = now;
    }

    public void Cancel(string cancellationReason, Guid cancelledByTenantUserId, DateTimeOffset now)
    {
        TransferStatus = "CANCELLED";
        CancellationReason = cancellationReason.Trim();
        CancelledByTenantUserId = cancelledByTenantUserId;
        CancelledAt = now;
        UpdatedByTenantUserId = cancelledByTenantUserId;
        UpdatedAt = now;
    }
}
