using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class Till : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string TillCode { get; protected set; } = string.Empty;
    public string TillName { get; protected set; } = string.Empty;
    public string TillType { get; protected set; } = string.Empty;
    public decimal DefaultOpeningFloatAmount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public bool IsCashManaged { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Till Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        string tillName,
        string tillCode,
        string tillType,
        decimal defaultOpeningFloatAmount,
        string currencyCode,
        bool isCashManaged,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new Till
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TillName = tillName.Trim(),
            TillCode = TillConstants.NormalizeTillCode(tillCode),
            TillType = TillConstants.NormalizeTillType(tillType),
            DefaultOpeningFloatAmount = defaultOpeningFloatAmount,
            CurrencyCode = TillConstants.NormalizeCurrencyCode(currencyCode),
            IsCashManaged = isCashManaged,
            Status = TillConstants.NormalizeStatus(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid outletId,
        string tillName,
        string tillCode,
        string tillType,
        decimal defaultOpeningFloatAmount,
        string currencyCode,
        bool isCashManaged,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        OutletId = outletId;
        TillName = tillName.Trim();
        TillCode = TillConstants.NormalizeTillCode(tillCode);
        TillType = TillConstants.NormalizeTillType(tillType);
        DefaultOpeningFloatAmount = defaultOpeningFloatAmount;
        CurrencyCode = TillConstants.NormalizeCurrencyCode(currencyCode);
        IsCashManaged = isCashManaged;
        Status = TillConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = TillConstants.DeletedStatus;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
