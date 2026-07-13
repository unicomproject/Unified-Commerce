using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class TillActivationCode : BaseEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TillId { get; protected set; }
    public string ActivationCodeHash { get; protected set; } = string.Empty;
    public Guid IssuedByTenantUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public Guid? UsedByPosDeviceId { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }

    public static TillActivationCode Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        string activationCodeHash,
        Guid issuedByTenantUserId,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new TillActivationCode
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TillId = tillId,
            ActivationCodeHash = activationCodeHash,
            IssuedByTenantUserId = issuedByTenantUserId,
            Status = TillActivationCodeConstants.ActiveStatus,
            ExpiresAt = expiresAt,
            CreatedAt = now,
        };
    }

    public bool IsUsable(DateTimeOffset now) =>
        string.Equals(Status, TillActivationCodeConstants.ActiveStatus, StringComparison.Ordinal) &&
        ExpiresAt > now &&
        UsedAt is null &&
        UsedByPosDeviceId is null;

    public void MarkUsed(Guid posDeviceId, DateTimeOffset now)
    {
        Status = TillActivationCodeConstants.UsedStatus;
        UsedByPosDeviceId = posDeviceId;
        UsedAt = now;
    }
}
