using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class HardwareDevice : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid? HardwareProfileId { get; protected set; }
    public string HardwareDeviceCode { get; protected set; } = string.Empty;
    public string HardwareDeviceName { get; protected set; } = string.Empty;
    public string HardwareDeviceType { get; protected set; } = string.Empty;
    public string ConnectionType { get; protected set; } = string.Empty;
    public string? Manufacturer { get; protected set; }
    public string? Model { get; protected set; }
    public string? SerialNumber { get; protected set; }
    public string? AssetTag { get; protected set; }
    public string? FirmwareVersion { get; protected set; }
    public string? ConfigJson { get; protected set; }
    public DateTimeOffset? LastSeenAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static HardwareDevice Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid? hardwareProfileId,
        string hardwareDeviceCode,
        string hardwareDeviceName,
        string hardwareDeviceType,
        string connectionType,
        string? manufacturer,
        string? model,
        string? serialNumber,
        string? assetTag,
        string? firmwareVersion,
        string? configJson,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new HardwareDevice
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            HardwareProfileId = hardwareProfileId,
            HardwareDeviceCode = hardwareDeviceCode.Trim().ToUpperInvariant(),
            HardwareDeviceName = hardwareDeviceName.Trim(),
            HardwareDeviceType = hardwareDeviceType.Trim().ToUpperInvariant(),
            ConnectionType = connectionType.Trim().ToUpperInvariant(),
            Manufacturer = manufacturer?.Trim(),
            Model = model?.Trim(),
            SerialNumber = serialNumber?.Trim(),
            AssetTag = assetTag?.Trim(),
            FirmwareVersion = firmwareVersion?.Trim(),
            ConfigJson = configJson,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid? hardwareProfileId,
        string hardwareDeviceCode,
        string hardwareDeviceName,
        string hardwareDeviceType,
        string connectionType,
        string? manufacturer,
        string? model,
        string? serialNumber,
        string? assetTag,
        string? firmwareVersion,
        string? configJson,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        HardwareProfileId = hardwareProfileId;
        HardwareDeviceCode = hardwareDeviceCode.Trim().ToUpperInvariant();
        HardwareDeviceName = hardwareDeviceName.Trim();
        HardwareDeviceType = hardwareDeviceType.Trim().ToUpperInvariant();
        ConnectionType = connectionType.Trim().ToUpperInvariant();
        Manufacturer = manufacturer?.Trim();
        Model = model?.Trim();
        SerialNumber = serialNumber?.Trim();
        AssetTag = assetTag?.Trim();
        FirmwareVersion = firmwareVersion?.Trim();
        ConfigJson = configJson;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void RecordHeartbeat(DateTimeOffset lastSeenAt)
    {
        LastSeenAt = lastSeenAt;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}

