using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Payment.Entities;

public class PaymentMethod : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string MethodCode { get; protected set; } = string.Empty;
    public string MethodName { get; protected set; } = string.Empty;
    public string MethodType { get; protected set; } = string.Empty;
    public bool IsActiveForPos { get; protected set; }
    public bool IsActiveForOnline { get; protected set; }
    public bool RequiresManualConfirmation { get; protected set; }
    public bool SupportsRefund { get; protected set; }
    public bool RequiresReference { get; protected set; }
    public bool AllowsChange { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
