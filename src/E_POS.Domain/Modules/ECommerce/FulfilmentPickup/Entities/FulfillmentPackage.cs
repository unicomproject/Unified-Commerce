using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public sealed class FulfillmentPackage : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid FulfillmentOrderId { get; private set; }
    public string PackageNumber { get; private set; } = string.Empty;
    public Guid? StagingInventoryLocationId { get; private set; }
    public string PackageStatus { get; private set; } = string.Empty;
    public Guid PackedByTenantUserId { get; private set; }
    public DateTimeOffset PackedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public string? PackingNote { get; private set; }
    public long RowVersion { get; private set; }

    private FulfillmentPackage() { }

    public static FulfillmentPackage Create(Guid id, Guid tenantId, Guid fulfillmentOrderId,
        string packageNumber, Guid userId, string? packingNote, DateTimeOffset now) => new()
    {
        Id = id, TenantId = tenantId, FulfillmentOrderId = fulfillmentOrderId,
        PackageNumber = packageNumber, PackageStatus = "PACKED",
        PackedByTenantUserId = userId, PackedAt = now,
        PackingNote = string.IsNullOrWhiteSpace(packingNote) ? null : packingNote.Trim(),
        CreatedAt = now, UpdatedAt = now
    };

    public void MarkReady(DateTimeOffset now)
    {
        PackageStatus = "READY";
        ReadyAt ??= now;
        UpdatedAt = now;
        RowVersion++;
    }
}
