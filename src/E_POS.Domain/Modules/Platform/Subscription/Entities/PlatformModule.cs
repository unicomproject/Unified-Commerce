using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class PlatformModule : AuditableEntity
{
    public string ModuleCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
    public string ModuleKey { get; protected set; } = string.Empty;
    public string ModuleName { get; protected set; } = string.Empty;
    public bool IsCoreModule { get; protected set; }

    public static PlatformModule Create(
        Guid id,
        string moduleCode,
        string name,
        string? description,
        string status,
        int sortOrder,
        DateTimeOffset now,
        bool isCoreModule = false)
    {
        return new PlatformModule
        {
            Id = id,
            ModuleCode = moduleCode,
            Name = name,
            Description = description,
            Status = status,
            SortOrder = sortOrder,
            ModuleKey = moduleCode,
            ModuleName = name,
            IsCoreModule = isCoreModule,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
