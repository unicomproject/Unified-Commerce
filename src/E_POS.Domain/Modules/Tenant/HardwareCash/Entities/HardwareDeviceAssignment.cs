using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class HardwareDeviceAssignment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid HardwareDeviceId { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public bool IsPrimary { get; protected set; }
    public DateTimeOffset AssignedAt { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public Guid? ReleasedByTenantUserId { get; protected set; }
    public string? ReleaseReason { get; protected set; }
    
    // We don't map CreatedBy/UpdatedBy from AuditableEntity since ERD doesn't have them for this table, 
    // it uses AssignedBy/ReleasedBy instead. We'll ignore CreatedBy/UpdatedBy in the EF config.

    public static HardwareDeviceAssignment Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid hardwareDeviceId,
        Guid? tillId,
        Guid? posDeviceId,
        bool isPrimary,
        Guid? assignedByTenantUserId,
        DateTimeOffset now)
    {
        if (tillId == null && posDeviceId == null)
            throw new ArgumentException("Either TillId or PosDeviceId must be provided.");
        if (tillId != null && posDeviceId != null)
            throw new ArgumentException("Only one of TillId or PosDeviceId can be provided.");

        return new HardwareDeviceAssignment
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            HardwareDeviceId = hardwareDeviceId,
            TillId = tillId,
            PosDeviceId = posDeviceId,
            IsPrimary = isPrimary,
            AssignedAt = now,
            AssignedByTenantUserId = assignedByTenantUserId,
            CreatedAt = now, // for AuditableEntity base
            UpdatedAt = now
        };
    }

    public void Release(string? releaseReason, Guid? releasedByTenantUserId, DateTimeOffset now)
    {
        ReleasedAt = now;
        ReleasedByTenantUserId = releasedByTenantUserId;
        ReleaseReason = releaseReason?.Trim();
        UpdatedAt = now;
    }
}

