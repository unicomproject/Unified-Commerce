using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class FulfillmentMethodOutlet : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid FulfillmentMethodId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public int? PreparationLeadMinutes { get; protected set; }
    public int? PickupWindowMinutes { get; protected set; }
    public TimeOnly? CutoffTime { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static FulfillmentMethodOutlet Create(Guid id, Guid tenantId, Guid fulfillmentMethodId, Guid outletId, string status, DateTimeOffset now)
    {
        return new FulfillmentMethodOutlet
        {
            Id = id,
            TenantId = tenantId,
            FulfillmentMethodId = fulfillmentMethodId,
            OutletId = outletId,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }
}

