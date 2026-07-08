using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ReturnPolicyTemplate : AuditableEntity
{
    public string TemplateCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public int? ReturnWindowDays { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static ReturnPolicyTemplate Create(Guid id, string templateCode, string name, int? returnWindowDays, string status, DateTimeOffset now)
    {
        return new ReturnPolicyTemplate
        {
            Id = id,
            TemplateCode = ReturnPolicyTemplateConstants.NormalizeCode(templateCode),
            Name = name.Trim(),
            ReturnWindowDays = returnWindowDays,
            Status = ReturnPolicyTemplateConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string templateCode, string name, int? returnWindowDays, string status, DateTimeOffset now)
    {
        TemplateCode = ReturnPolicyTemplateConstants.NormalizeCode(templateCode);
        Name = name.Trim();
        ReturnWindowDays = returnWindowDays;
        Status = ReturnPolicyTemplateConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = ReturnPolicyTemplateConstants.DeletedStatus;
        UpdatedAt = now;
    }
}
