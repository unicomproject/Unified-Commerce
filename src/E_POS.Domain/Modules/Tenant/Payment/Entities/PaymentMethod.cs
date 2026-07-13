using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Payment.Entities;

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

    public static PaymentMethod Create(
        Guid id,
        Guid tenantId,
        string methodCode,
        string methodName,
        string methodType,
        bool isActiveForPos,
        bool allowsChange,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new PaymentMethod
        {
            Id = id,
            TenantId = tenantId,
            MethodCode = methodCode.Trim().ToUpperInvariant(),
            MethodName = methodName.Trim(),
            MethodType = methodType.Trim().ToUpperInvariant(),
            IsActiveForPos = isActiveForPos,
            IsActiveForOnline = false,
            RequiresManualConfirmation = false,
            SupportsRefund = true,
            RequiresReference = false,
            AllowsChange = allowsChange,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

