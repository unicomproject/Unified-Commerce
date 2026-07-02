using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentMethod : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string MethodCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string MethodType { get; protected set; } = string.Empty;

    public static FulfillmentMethod Create(Guid id, Guid tenantId, string methodCode, string name, string? description, string status, string methodType, DateTimeOffset now)
    {
        return new FulfillmentMethod
        {
            Id = id,
            TenantId = tenantId,
            MethodCode = methodCode.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = status.Trim().ToUpperInvariant(),
            MethodType = methodType.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}