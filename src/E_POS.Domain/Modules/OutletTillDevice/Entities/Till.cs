using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class Till : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string TillCode { get; protected set; } = string.Empty;

    public static Till Create(Guid id, Guid tenantId, Guid outletId, string name, string tillCode, string status, DateTimeOffset now)
    {
        return new Till
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            Name = name.Trim(),
            TillCode = TillConstants.NormalizeTillCode(tillCode),
            Status = TillConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(Guid outletId, string name, string tillCode, string status, DateTimeOffset now)
    {
        OutletId = outletId;
        Name = name.Trim();
        TillCode = TillConstants.NormalizeTillCode(tillCode);
        Status = TillConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = TillConstants.DeletedStatus;
        UpdatedAt = now;
    }
}