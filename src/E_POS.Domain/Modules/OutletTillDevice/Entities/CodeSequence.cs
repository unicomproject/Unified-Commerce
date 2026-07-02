using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class CodeSequence : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string SequenceKey { get; protected set; } = string.Empty;
    public string Prefix { get; protected set; } = string.Empty;
    public int CurrentValue { get; protected set; }
    public int PaddingLength { get; protected set; }

    public static CodeSequence Create(Guid id, Guid tenantId, string sequenceKey, string prefix, int currentValue, int paddingLength, DateTimeOffset now)
    {
        return new CodeSequence
        {
            Id = id,
            TenantId = tenantId,
            SequenceKey = NormalizeSequenceKey(sequenceKey),
            Prefix = NormalizePrefix(prefix),
            CurrentValue = currentValue,
            PaddingLength = Math.Max(1, paddingLength),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public int Advance(string prefix, int paddingLength, DateTimeOffset now)
    {
        Prefix = NormalizePrefix(prefix);
        PaddingLength = Math.Max(1, paddingLength);
        CurrentValue++;
        UpdatedAt = now;
        return CurrentValue;
    }

    private static string NormalizeSequenceKey(string sequenceKey) => sequenceKey.Trim().ToUpperInvariant();
    private static string NormalizePrefix(string prefix) => prefix.Trim().ToUpperInvariant();
}