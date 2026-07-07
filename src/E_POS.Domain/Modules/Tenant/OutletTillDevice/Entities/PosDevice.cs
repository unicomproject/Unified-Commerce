using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class PosDevice : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string DeviceCode { get; protected set; } = string.Empty;
    public string DeviceSerialNumber { get; protected set; } = string.Empty;

    public static PosDevice Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        string name,
        string deviceCode,
        string deviceSerialNumber,
        string status,
        DateTimeOffset now)
    {
        return new PosDevice
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            Name = name.Trim(),
            DeviceCode = PosDeviceConstants.NormalizeDeviceCode(deviceCode),
            DeviceSerialNumber = PosDeviceConstants.NormalizeDeviceSerialNumber(deviceSerialNumber),
            Status = PosDeviceConstants.NormalizeStatus(status),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(Guid outletId, string name, string deviceSerialNumber, string status, DateTimeOffset now)
    {
        OutletId = outletId;
        Name = name.Trim();
        DeviceSerialNumber = PosDeviceConstants.NormalizeDeviceSerialNumber(deviceSerialNumber);
        Status = PosDeviceConstants.NormalizeStatus(status);
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = PosDeviceConstants.DeletedStatus;
        UpdatedAt = now;
    }
}
