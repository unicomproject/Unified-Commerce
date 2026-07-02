using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentMethodOutlet : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid FulfillmentMethodId { get; protected set; }

    public static FulfillmentMethodOutlet Create(Guid id, Guid fulfillmentMethodId, Guid outletId, string status, DateTimeOffset now)
    {
        return new FulfillmentMethodOutlet
        {
            Id = id,
            FulfillmentMethodId = fulfillmentMethodId,
            OutletId = outletId,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedAt = now;
    }
}