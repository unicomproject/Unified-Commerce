using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class PosDevice : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string DeviceCode { get; protected set; } = string.Empty;
    public string DeviceName { get; protected set; } = string.Empty;
    public string DeviceType { get; protected set; } = string.Empty;
    public string? Platform { get; protected set; }
    public string? AppVersion { get; protected set; }
    public string? DeviceFingerprintHash { get; protected set; }
    public bool IsTrusted { get; protected set; }
    public DateTimeOffset? PairedAt { get; protected set; }
    public Guid? PairedByTenantUserId { get; protected set; }
    public DateTimeOffset? UnpairedAt { get; protected set; }
    public Guid? UnpairedByTenantUserId { get; protected set; }
    public DateTimeOffset? LastSeenAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static PosDevice Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        string deviceCode,
        string deviceName,
        string deviceType,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new PosDevice
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            DeviceCode = PosDeviceConstants.NormalizeDeviceCode(deviceCode),
            DeviceName = deviceName.Trim(),
            DeviceType = PosDeviceConstants.NormalizeDeviceType(deviceType),
            Status = PosDeviceConstants.NormalizeStatus(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid outletId,
        string deviceName,
        string deviceType,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        OutletId = outletId;
        DeviceName = deviceName.Trim();
        DeviceType = PosDeviceConstants.NormalizeDeviceType(deviceType);
        Status = PosDeviceConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = PosDeviceConstants.DeletedStatus;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
