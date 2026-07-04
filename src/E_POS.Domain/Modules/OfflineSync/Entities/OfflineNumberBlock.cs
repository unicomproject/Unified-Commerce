using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class OfflineNumberBlock : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public string DocumentType { get; protected set; } = string.Empty;
    public string? PrefixSnapshot { get; protected set; }
    public string? SuffixSnapshot { get; protected set; }
    public int PaddingLengthSnapshot { get; protected set; }
    public long RangeStart { get; protected set; }
    public long RangeEnd { get; protected set; }
    public long NextValue { get; protected set; }
    public string BlockStatus { get; protected set; } = string.Empty;
    public DateTimeOffset AllocatedAt { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? ExhaustedAt { get; protected set; }
}
