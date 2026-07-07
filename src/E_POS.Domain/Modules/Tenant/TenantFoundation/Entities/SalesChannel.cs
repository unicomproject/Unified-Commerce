using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class SalesChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ChannelCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string ChannelType { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
}

