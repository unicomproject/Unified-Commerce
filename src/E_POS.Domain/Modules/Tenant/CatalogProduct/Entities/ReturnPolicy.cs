using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

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
    public Guid? SourceTemplateId { get; protected set; }
    public int? SourceTemplateVersion { get; protected set; }
    public int VersionNumber { get; protected set; } = 1;
    public string LifecycleStatus { get; protected set; } = "PUBLISHED";
    public bool ReviewRequired { get; protected set; } = false;
    public DateTimeOffset? SeededAt { get; protected set; }
    public Guid ConcurrencyToken { get; protected set; } = Guid.NewGuid();
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
            VersionNumber = 1,
            LifecycleStatus = "PUBLISHED",
            ReviewRequired = false,
            ConcurrencyToken = Guid.NewGuid(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ReturnPolicy CreateSeededFromPlatformTemplate(
        Guid id,
        Guid tenantId,
        Guid platformTemplateId,
        int platformTemplateVersion,
        string policyCode,
        string policyName,
        string? description,
        int returnWindowDays,
        int exchangeWindowDays,
        bool requiresReceipt,
        bool allowDefectiveReturn,
        bool requiresManagerApproval,
        DateTimeOffset seededAt)
    {
        return new ReturnPolicy
        {
            Id = id,
            TenantId = tenantId,
            ReturnPolicyCode = policyCode.Trim().ToUpperInvariant(),
            ReturnPolicyName = policyName.Trim(),
            Description = description?.Trim(),
            ReturnWindowDays = returnWindowDays,
            ExchangeWindowDays = exchangeWindowDays,
            RequiresReceipt = requiresReceipt,
            AllowDefectiveReturn = allowDefectiveReturn,
            RequiresManagerApproval = requiresManagerApproval,
            IsDefaultPolicy = true,
            Status = "ACTIVE",
            SourceTemplateId = platformTemplateId,
            SourceTemplateVersion = platformTemplateVersion,
            VersionNumber = 1,
            LifecycleStatus = "PUBLISHED",
            ReviewRequired = true,
            SeededAt = seededAt,
            ConcurrencyToken = Guid.NewGuid(),
            CreatedByTenantUserId = null,
            UpdatedByTenantUserId = null,
            CreatedAt = seededAt,
            UpdatedAt = seededAt
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
        ConcurrencyToken = Guid.NewGuid();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void MarkReviewCompleted(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        ReviewRequired = false;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        LifecycleStatus = "ARCHIVED";
        ConcurrencyToken = Guid.NewGuid();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
