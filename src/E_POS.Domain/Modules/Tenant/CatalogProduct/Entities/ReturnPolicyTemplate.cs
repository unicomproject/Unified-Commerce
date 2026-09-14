using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ReturnPolicyTemplate : AuditableEntity
{
    public string TemplateCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int? ReturnWindowDays { get; protected set; }
    public int? ExchangeWindowDays { get; protected set; }
    public bool RequiresReceipt { get; protected set; } = true;
    public bool AllowDefectiveReturn { get; protected set; } = true;
    public bool RequiresManagerApproval { get; protected set; } = false;
    public bool IsPlatformDefault { get; protected set; } = false;
    public int VersionNumber { get; protected set; } = 1;
    public string LifecycleStatus { get; protected set; } = ReturnPolicyTemplateConstants.LifecycleStatusDraft;
    public Guid ConcurrencyToken { get; protected set; } = Guid.NewGuid();
    public string Status { get; protected set; } = ReturnPolicyTemplateConstants.ActiveStatus;

    public static ReturnPolicyTemplate Create(
        Guid id,
        string templateCode,
        string name,
        string? description,
        int? returnWindowDays,
        int? exchangeWindowDays,
        bool requiresReceipt,
        bool allowDefectiveReturn,
        bool requiresManagerApproval,
        bool isPlatformDefault,
        string status,
        DateTimeOffset now)
    {
        return new ReturnPolicyTemplate
        {
            Id = id,
            TemplateCode = ReturnPolicyTemplateConstants.NormalizeCode(templateCode),
            Name = name.Trim(),
            Description = description?.Trim(),
            ReturnWindowDays = returnWindowDays,
            ExchangeWindowDays = exchangeWindowDays,
            RequiresReceipt = requiresReceipt,
            AllowDefectiveReturn = allowDefectiveReturn,
            RequiresManagerApproval = requiresManagerApproval,
            IsPlatformDefault = isPlatformDefault,
            VersionNumber = 1,
            LifecycleStatus = ReturnPolicyTemplateConstants.LifecycleStatusDraft,
            ConcurrencyToken = Guid.NewGuid(),
            Status = ReturnPolicyTemplateConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ReturnPolicyTemplate Create(Guid id, string templateCode, string name, int? returnWindowDays, string status, DateTimeOffset now)
    {
        return Create(id, templateCode, name, null, returnWindowDays, null, true, true, false, false, status, now);
    }

    public void UpdateProfile(
        string templateCode,
        string name,
        string? description,
        int? returnWindowDays,
        int? exchangeWindowDays,
        bool requiresReceipt,
        bool allowDefectiveReturn,
        bool requiresManagerApproval,
        bool isPlatformDefault,
        string status,
        DateTimeOffset now)
    {
        if (LifecycleStatus != ReturnPolicyTemplateConstants.LifecycleStatusDraft)
        {
            throw new InvalidOperationException("Published or archived templates cannot be edited in place. Create a new version/draft.");
        }

        TemplateCode = ReturnPolicyTemplateConstants.NormalizeCode(templateCode);
        Name = name.Trim();
        Description = description?.Trim();
        ReturnWindowDays = returnWindowDays;
        ExchangeWindowDays = exchangeWindowDays;
        RequiresReceipt = requiresReceipt;
        AllowDefectiveReturn = allowDefectiveReturn;
        RequiresManagerApproval = requiresManagerApproval;
        IsPlatformDefault = isPlatformDefault;
        Status = ReturnPolicyTemplateConstants.NormalizeStatus(status);
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAt = now;
    }

    public void UpdateProfile(string templateCode, string name, int? returnWindowDays, string status, DateTimeOffset now)
    {
        UpdateProfile(templateCode, name, Description, returnWindowDays, ExchangeWindowDays, RequiresReceipt, AllowDefectiveReturn, RequiresManagerApproval, IsPlatformDefault, status, now);
    }

    public void Publish(DateTimeOffset now)
    {
        if (LifecycleStatus == ReturnPolicyTemplateConstants.LifecycleStatusArchived)
        {
            throw new InvalidOperationException("Archived template cannot be published directly.");
        }

        LifecycleStatus = ReturnPolicyTemplateConstants.LifecycleStatusPublished;
        Status = ReturnPolicyTemplateConstants.ActiveStatus;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (IsPlatformDefault)
        {
            throw new InvalidOperationException("Cannot archive a template that is designated as the platform default.");
        }

        LifecycleStatus = ReturnPolicyTemplateConstants.LifecycleStatusArchived;
        Status = ReturnPolicyTemplateConstants.InactiveStatus;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAt = now;
    }

    public void SetAsDefault(DateTimeOffset now)
    {
        if (LifecycleStatus == ReturnPolicyTemplateConstants.LifecycleStatusArchived)
        {
            throw new InvalidOperationException("Cannot set an archived template as platform default.");
        }

        IsPlatformDefault = true;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAt = now;
    }

    public void UnsetDefault(DateTimeOffset now)
    {
        IsPlatformDefault = false;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAt = now;
    }

    public ReturnPolicyTemplate CloneAsDraft(Guid newId, string newCode, string newName, DateTimeOffset now)
    {
        return new ReturnPolicyTemplate
        {
            Id = newId,
            TemplateCode = ReturnPolicyTemplateConstants.NormalizeCode(newCode),
            Name = newName.Trim(),
            Description = Description,
            ReturnWindowDays = ReturnWindowDays,
            ExchangeWindowDays = ExchangeWindowDays,
            RequiresReceipt = RequiresReceipt,
            AllowDefectiveReturn = AllowDefectiveReturn,
            RequiresManagerApproval = RequiresManagerApproval,
            IsPlatformDefault = false,
            VersionNumber = VersionNumber + 1,
            LifecycleStatus = ReturnPolicyTemplateConstants.LifecycleStatusDraft,
            ConcurrencyToken = Guid.NewGuid(),
            Status = ReturnPolicyTemplateConstants.ActiveStatus,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = ReturnPolicyTemplateConstants.DeletedStatus;
        LifecycleStatus = ReturnPolicyTemplateConstants.LifecycleStatusArchived;
        IsPlatformDefault = false;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAt = now;
    }
}
