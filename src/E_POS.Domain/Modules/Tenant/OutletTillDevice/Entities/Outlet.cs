using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class Outlet : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string OutletCode { get; protected set; } = string.Empty;
    public string OutletName { get; protected set; } = string.Empty;
    public string OutletType { get; protected set; } = string.Empty;
    public string? Phone { get; protected set; }
    public string? Email { get; protected set; }
    public string Timezone { get; protected set; } = string.Empty;
    public bool IsDefaultOutlet { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Outlet Create(
        Guid id,
        Guid tenantId,
        string outletName,
        string outletCode,
        string status,
        string outletType,
        string timezone,
        bool isDefaultOutlet,
        string? phone,
        string? email,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new Outlet
        {
            Id = id,
            TenantId = tenantId,
            OutletName = outletName.Trim(),
            OutletCode = OutletConstants.NormalizeOutletCode(outletCode),
            Status = OutletConstants.NormalizeStatus(status),
            OutletType = OutletConstants.NormalizeOutletType(outletType),
            Timezone = NormalizeTimezone(timezone),
            IsDefaultOutlet = isDefaultOutlet,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string outletName,
        string outletCode,
        string status,
        string outletType,
        string timezone,
        bool isDefaultOutlet,
        string? phone,
        string? email,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        OutletName = outletName.Trim();
        OutletCode = OutletConstants.NormalizeOutletCode(outletCode);
        Status = OutletConstants.NormalizeStatus(status);
        OutletType = OutletConstants.NormalizeOutletType(outletType);
        Timezone = NormalizeTimezone(timezone);
        IsDefaultOutlet = isDefaultOutlet;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        IsDefaultOutlet = false;
        Status = OutletConstants.DeletedStatus;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    private static string NormalizeTimezone(string timezone) =>
        string.IsNullOrWhiteSpace(timezone) ? OutletConstants.DefaultTimezone : timezone.Trim();
}
