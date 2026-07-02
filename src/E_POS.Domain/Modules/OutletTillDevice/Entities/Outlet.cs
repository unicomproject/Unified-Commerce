using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class Outlet : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string OutletCode { get; protected set; } = string.Empty;
    public string OutletType { get; protected set; } = string.Empty;
    public bool IsOnlineVisible { get; protected set; }
    public string? ContactPhone { get; protected set; }
    public string? ContactEmail { get; protected set; }

    public static Outlet Create(
        Guid id,
        Guid tenantId,
        string name,
        string outletCode,
        string status,
        string outletType,
        bool isOnlineVisible,
        string? contactPhone,
        string? contactEmail,
        DateTimeOffset now)
    {
        return new Outlet
        {
            Id = id,
            TenantId = tenantId,
            Name = name.Trim(),
            OutletCode = OutletConstants.NormalizeOutletCode(outletCode),
            Status = OutletConstants.NormalizeStatus(status),
            OutletType = OutletConstants.NormalizeOutletType(outletType),
            IsOnlineVisible = isOnlineVisible,
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string name,
        string outletCode,
        string status,
        string outletType,
        bool isOnlineVisible,
        string? contactPhone,
        string? contactEmail,
        DateTimeOffset now)
    {
        Name = name.Trim();
        OutletCode = OutletConstants.NormalizeOutletCode(outletCode);
        Status = OutletConstants.NormalizeStatus(status);
        OutletType = OutletConstants.NormalizeOutletType(outletType);
        IsOnlineVisible = isOnlineVisible;
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = OutletConstants.DeletedStatus;
        UpdatedAt = now;
    }
}
