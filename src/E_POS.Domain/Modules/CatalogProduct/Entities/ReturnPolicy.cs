using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ReturnPolicy : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReturnPolicyCode { get; protected set; } = string.Empty;
    public string ReturnPolicyName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int ReturnWindowDays { get; protected set; }
    public int ExchangeWindowDays { get; protected set; }
    public bool RequiresReceipt { get; protected set; }
    public bool AllowDefectiveReturn { get; protected set; }
    public bool RequiresManagerApproval { get; protected set; }
    public bool IsDefaultPolicy { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ReturnPolicy Create(
        Guid id, 
        Guid tenantId, 
        string returnPolicyCode, 
        string returnPolicyName, 
        string? description,
        int returnWindowDays, 
        int exchangeWindowDays,
        bool requiresReceipt,
        bool allowDefectiveReturn,
        bool requiresManagerApproval,
        bool isDefaultPolicy,
        string status, 
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ReturnPolicy
        {
            Id = id,
            TenantId = tenantId,
            ReturnPolicyCode = returnPolicyCode.Trim().ToUpperInvariant(),
            ReturnPolicyName = returnPolicyName.Trim(),
            Description = description?.Trim(),
            ReturnWindowDays = returnWindowDays,
            ExchangeWindowDays = exchangeWindowDays,
            RequiresReceipt = requiresReceipt,
            AllowDefectiveReturn = allowDefectiveReturn,
            RequiresManagerApproval = requiresManagerApproval,
            IsDefaultPolicy = isDefaultPolicy,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string returnPolicyCode, 
        string returnPolicyName, 
        string? description,
        int returnWindowDays, 
        int exchangeWindowDays,
        bool requiresReceipt,
        bool allowDefectiveReturn,
        bool requiresManagerApproval,
        bool isDefaultPolicy,
        string status, 
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ReturnPolicyCode = returnPolicyCode.Trim().ToUpperInvariant();
        ReturnPolicyName = returnPolicyName.Trim();
        Description = description?.Trim();
        ReturnWindowDays = returnWindowDays;
        ExchangeWindowDays = exchangeWindowDays;
        RequiresReceipt = requiresReceipt;
        AllowDefectiveReturn = allowDefectiveReturn;
        RequiresManagerApproval = requiresManagerApproval;
        IsDefaultPolicy = isDefaultPolicy;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}