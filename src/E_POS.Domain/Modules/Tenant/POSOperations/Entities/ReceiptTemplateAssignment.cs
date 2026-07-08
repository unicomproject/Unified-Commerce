using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class ReceiptTemplateAssignment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReceiptTemplateVersionId { get; protected set; }
    public string AssignmentScope { get; protected set; } = string.Empty;
    public Guid? OutletId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public bool IsDefault { get; protected set; }
    public DateTimeOffset EffectiveFrom { get; protected set; }
    public DateTimeOffset? EffectiveTo { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}

