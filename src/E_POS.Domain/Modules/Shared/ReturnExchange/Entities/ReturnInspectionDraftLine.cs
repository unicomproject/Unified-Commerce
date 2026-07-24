using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnInspectionDraftLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReturnInspectionDraftId { get; protected set; }
    public Guid SaleLineId { get; protected set; }
    public Guid? ConditionId { get; protected set; }
    public string ConditionCodeSnapshot { get; protected set; } = string.Empty;
    public string? InspectionNotes { get; protected set; }
    public string InspectionStatus { get; protected set; } = "PENDING";
    public Guid? InspectedByTenantUserId { get; protected set; }
    public DateTimeOffset? InspectedAt { get; protected set; }

    public static ReturnInspectionDraftLine Create(
        Guid id, Guid tenantId, Guid draftId, Guid saleLineId, Guid? conditionId,
        string conditionCode, string? notes, Guid userId, DateTimeOffset now) =>
        new()
        {
            Id = id, TenantId = tenantId, ReturnInspectionDraftId = draftId, SaleLineId = saleLineId,
            ConditionId = conditionId, ConditionCodeSnapshot = conditionCode.Trim().ToUpperInvariant(),
            InspectionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            InspectionStatus = string.IsNullOrWhiteSpace(conditionCode) ? "PENDING" : "INSPECTED",
            InspectedByTenantUserId = userId, InspectedAt = now, CreatedAt = now
        };

    public void Upsert(Guid? conditionId, string conditionCode, string? notes, Guid userId, DateTimeOffset now)
    {
        ConditionId = conditionId; ConditionCodeSnapshot = conditionCode.Trim().ToUpperInvariant();
        InspectionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        InspectionStatus = string.IsNullOrWhiteSpace(conditionCode) ? "PENDING" : "INSPECTED";
        InspectedByTenantUserId = userId; InspectedAt = now;
    }
}
