using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public sealed class HardwareConfigurationChangeAudit : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid OutletId { get; private set; }
    public Guid PosDeviceId { get; private set; }
    public Guid HardwareDeviceId { get; private set; }
    public Guid? TillId { get; private set; }
    public Guid? TillSessionId { get; private set; }
    public int OldVersion { get; private set; }
    public int NewVersion { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public string? ChangeReason { get; private set; }
    public string SafeBeforeJson { get; private set; } = "{}";
    public string SafeAfterJson { get; private set; } = "{}";
    public Guid ChangedByTenantUserId { get; private set; }

    public static HardwareConfigurationChangeAudit Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid posDeviceId,
        Guid hardwareDeviceId,
        Guid? tillId,
        Guid? tillSessionId,
        int oldVersion,
        int newVersion,
        string changeType,
        string? changeReason,
        string safeBeforeJson,
        string safeAfterJson,
        Guid changedByTenantUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            PosDeviceId = posDeviceId,
            HardwareDeviceId = hardwareDeviceId,
            TillId = tillId,
            TillSessionId = tillSessionId,
            OldVersion = oldVersion,
            NewVersion = newVersion,
            ChangeType = changeType.Trim().ToUpperInvariant(),
            ChangeReason = changeReason?.Trim(),
            SafeBeforeJson = safeBeforeJson,
            SafeAfterJson = safeAfterJson,
            ChangedByTenantUserId = changedByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
}
