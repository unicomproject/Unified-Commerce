using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ReturnPolicy : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string PolicyCode { get; protected set; } = string.Empty;
    public int? ReturnWindowDays { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static ReturnPolicy Create(Guid id, Guid tenantId, string policyCode, string name, int? returnWindowDays, string status, DateTimeOffset now)
    {
        return new ReturnPolicy
        {
            Id = id,
            TenantId = tenantId,
            PolicyCode = ReturnPolicyConstants.NormalizeCode(policyCode),
            Name = name.Trim(),
            ReturnWindowDays = returnWindowDays,
            Status = ReturnPolicyConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string policyCode, string name, int? returnWindowDays, string status, DateTimeOffset now)
    {
        PolicyCode = ReturnPolicyConstants.NormalizeCode(policyCode);
        Name = name.Trim();
        ReturnWindowDays = returnWindowDays;
        Status = ReturnPolicyConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = ReturnPolicyConstants.DeletedStatus;
        UpdatedAt = now;
    }
}