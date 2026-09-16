namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

/// <summary>Server-validated restriction for one HTTP request; never bound from query/body.</summary>
public sealed class HardwareQueryScope
{
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? OutletId { get; private set; }
    public Guid? TillId { get; private set; }
    public void Restrict(Guid tenantId, Guid userId, Guid outletId, Guid tillId)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty || outletId == Guid.Empty || tillId == Guid.Empty)
            throw new ArgumentException("Complete hardware scope is required.");
        TenantId = tenantId; UserId = userId; OutletId = outletId; TillId = tillId;
    }
}
