using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public sealed class TenantUserCodeSequence : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string SequenceType { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public long CurrentValue { get; private set; }
}
