using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class ReceiptTemplateVersion : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReceiptTemplateId { get; protected set; }
    public int VersionNumber { get; protected set; }
    public string TemplateData { get; protected set; } = string.Empty;
    public string? PageSize { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset? EffectiveFrom { get; protected set; }
    public DateTimeOffset? EffectiveTo { get; protected set; }
}

