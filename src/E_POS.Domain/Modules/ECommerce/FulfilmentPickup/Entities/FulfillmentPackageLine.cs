using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public sealed class FulfillmentPackageLine : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid FulfillmentPackageId { get; private set; }
    public Guid FulfillmentOrderLineId { get; private set; }
    public decimal Quantity { get; private set; }

    private FulfillmentPackageLine() { }

    public static FulfillmentPackageLine Create(Guid id, Guid tenantId, Guid packageId,
        Guid lineId, decimal quantity, DateTimeOffset now) => quantity <= 0
        ? throw new ArgumentOutOfRangeException(nameof(quantity))
        : new() { Id = id, TenantId = tenantId, FulfillmentPackageId = packageId,
            FulfillmentOrderLineId = lineId, Quantity = quantity, CreatedAt = now, UpdatedAt = now };
}
