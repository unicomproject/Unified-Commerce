using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class SerialNumber : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
    public Guid? CurrentInventoryBalanceId { get; protected set; }
    public string SerialNumberValue { get; protected set; } = string.Empty;
    public string SerialStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ReceivedAt { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected SerialNumber() { }

    public static SerialNumber Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        Guid? productBatchId,
        Guid? currentInventoryBalanceId,
        string serialNumberValue,
        string serialStatus,
        DateTimeOffset? receivedAt,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new SerialNumber
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductBatchId = productBatchId,
            CurrentInventoryBalanceId = currentInventoryBalanceId,
            SerialNumberValue = serialNumberValue.Trim(),
            SerialStatus = serialStatus.Trim(),
            ReceivedAt = receivedAt,
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(
        string serialStatus,
        Guid? currentInventoryBalanceId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        SerialStatus = serialStatus.Trim();
        CurrentInventoryBalanceId = currentInventoryBalanceId;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
