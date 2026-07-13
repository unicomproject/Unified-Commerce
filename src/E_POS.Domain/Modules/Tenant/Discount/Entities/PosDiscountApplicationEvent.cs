using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public sealed class PosDiscountApplicationEvent : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Guid PosDiscountApplicationId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;
    public Guid ActorTenantUserId { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public static PosDiscountApplicationEvent Record(
        Guid id, Guid tenantId, Guid applicationId, string eventType,
        string fromStatus, string toStatus, Guid actorId, string? note, DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            PosDiscountApplicationId = applicationId,
            EventType = eventType.Trim().ToUpperInvariant(),
            FromStatus = fromStatus.Trim().ToUpperInvariant(),
            ToStatus = toStatus.Trim().ToUpperInvariant(),
            ActorTenantUserId = actorId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            OccurredAt = now
        };
}
