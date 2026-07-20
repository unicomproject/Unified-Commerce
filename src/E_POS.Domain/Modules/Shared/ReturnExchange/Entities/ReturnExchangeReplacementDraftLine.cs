using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnExchangeReplacementDraftLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReturnInspectionDraftId { get; protected set; }
    public Guid ReturnedSaleLineId { get; protected set; }
    public Guid ReplacementProductId { get; protected set; }
    public Guid ReplacementVariantId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public Guid SelectedByTenantUserId { get; protected set; }
    public DateTimeOffset SelectedAt { get; protected set; }

    public static ReturnExchangeReplacementDraftLine Create(
        Guid id,
        Guid tenantId,
        Guid draftId,
        Guid returnedSaleLineId,
        Guid replacementProductId,
        Guid replacementVariantId,
        decimal quantity,
        Guid userId,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            ReturnInspectionDraftId = draftId,
            ReturnedSaleLineId = returnedSaleLineId,
            ReplacementProductId = replacementProductId,
            ReplacementVariantId = replacementVariantId,
            Quantity = quantity,
            SelectedByTenantUserId = userId,
            SelectedAt = now,
            CreatedAt = now,
        };

    public void Update(
        Guid replacementProductId,
        Guid replacementVariantId,
        decimal quantity,
        Guid userId,
        DateTimeOffset now)
    {
        ReplacementProductId = replacementProductId;
        ReplacementVariantId = replacementVariantId;
        Quantity = quantity;
        SelectedByTenantUserId = userId;
        SelectedAt = now;
    }
}
