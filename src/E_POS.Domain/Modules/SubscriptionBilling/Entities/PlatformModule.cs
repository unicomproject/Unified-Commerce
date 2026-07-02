using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class PlatformModule : AuditableEntity
{
    public string ModuleCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }

    public static PlatformModule Create(
        Guid id,
        string moduleCode,
        string name,
        string? description,
        string status,
        int sortOrder,
        DateTimeOffset now)
    {
        return new PlatformModule
        {
            Id = id,
            ModuleCode = moduleCode,
            Name = name,
            Description = description,
            Status = status,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
