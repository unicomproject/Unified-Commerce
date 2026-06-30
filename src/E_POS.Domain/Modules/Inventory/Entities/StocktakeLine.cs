using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StocktakeLine : AuditableEntity
{
    public Guid ProductBatchId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid ProductVariantId { get; protected set; }
    public Guid StocktakeSessionId { get; protected set; }
}
