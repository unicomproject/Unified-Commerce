using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OfflineSync.Entities;

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

    protected OfflineNumberBlock() { }

    public static OfflineNumberBlock Create(
        Guid id,
        Guid tenantId,
        Guid offlineClientId,
        Guid? documentNumberSequenceId,
        string documentType,
        string? prefixSnapshot,
        string? suffixSnapshot,
        int paddingLengthSnapshot,
        long rangeStart,
        long rangeEnd,
        string blockStatus,
        DateTimeOffset now)
    {
        return new OfflineNumberBlock
        {
            Id = id,
            TenantId = tenantId,
            OfflineClientId = offlineClientId,
            DocumentNumberSequenceId = documentNumberSequenceId,
            DocumentType = documentType.Trim(),
            PrefixSnapshot = prefixSnapshot?.Trim(),
            SuffixSnapshot = suffixSnapshot?.Trim(),
            PaddingLengthSnapshot = paddingLengthSnapshot,
            RangeStart = rangeStart,
            RangeEnd = rangeEnd,
            NextValue = rangeStart,
            BlockStatus = blockStatus.Trim(),
            AllocatedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

