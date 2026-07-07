using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;

public class FulfillmentMethod : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string MethodCode { get; protected set; } = string.Empty;
    public string MethodName { get; protected set; } = string.Empty;
    public string MethodType { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool RequiresSlot { get; protected set; }
    public bool RequiresPreparation { get; protected set; }
    public bool IsDefault { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static FulfillmentMethod Create(Guid id, Guid tenantId, string methodCode, string methodName, string? description, string status, string methodType, DateTimeOffset now)
    {
        return new FulfillmentMethod
        {
            Id = id,
            TenantId = tenantId,
            MethodCode = methodCode,
            MethodName = methodName,
            Description = description,
            Status = status,
            MethodType = methodType,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

