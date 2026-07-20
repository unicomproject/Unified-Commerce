using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class DocumentNumberSequence : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string DocumentType { get; protected set; } = string.Empty;
    public string? DocumentSubtype { get; protected set; }
    public long CurrentValue { get; protected set; }
    public int PaddingLength { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public string Prefix { get; protected set; } = string.Empty;
    public string? Suffix { get; protected set; }
    public string ResetRule { get; protected set; } = string.Empty;
    public DateTimeOffset? LastResetAt { get; protected set; }
    public DateTimeOffset? LastGeneratedAt { get; protected set; }
    public long RowVersion { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public void Increment(DateTimeOffset now)
    {
        CurrentValue++;
        LastGeneratedAt = now;
    }
}

