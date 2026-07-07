using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class TillSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public decimal ClosingCashAmount { get; protected set; }
    public Guid OpenedByTenantUserId { get; protected set; }
    public string SessionNumber { get; protected set; } = string.Empty;
}

