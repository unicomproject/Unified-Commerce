using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class OfflineNumberBlock : AuditableEntity
{
    public Guid OfflineClientId { get; protected set; }
    public Guid DocumentNumberSequenceId { get; protected set; }
    public int NextValue { get; protected set; }
    public int PaddingLengthSnapshot { get; protected set; }
    public int RangeEnd { get; protected set; }
    public int RangeStart { get; protected set; }
}
