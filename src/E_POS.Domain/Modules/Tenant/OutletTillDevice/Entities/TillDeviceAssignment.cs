using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class TillDeviceAssignment : BaseEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TillId { get; protected set; }
    public Guid PosDeviceId { get; protected set; }
    public DateTimeOffset AssignedAt { get; protected set; }
    public Guid? AssignedByTenantUserId { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public Guid? ReleasedByTenantUserId { get; protected set; }
    public string? ReleaseReason { get; protected set; }

    public bool IsActive => ReleasedAt is null;

    public static TillDeviceAssignment Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid posDeviceId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now)
    {
        return new TillDeviceAssignment
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TillId = tillId,
            PosDeviceId = posDeviceId,
            AssignedAt = now,
            AssignedByTenantUserId = assignedByTenantUserId
        };
    }

    public void Release(Guid? releasedByTenantUserId, string? releaseReason, DateTimeOffset now)
    {
        ReleasedAt = now;
        ReleasedByTenantUserId = releasedByTenantUserId;
        ReleaseReason = string.IsNullOrWhiteSpace(releaseReason) ? null : releaseReason.Trim();
    }
}
