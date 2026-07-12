using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class ShoppingCartItemOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ShoppingCartItemId { get; protected set; }
    public Guid ChoiceGroupId { get; protected set; }
    public Guid ChoiceOptionId { get; protected set; }
    public string? ChoiceGroupNameSnapshot { get; protected set; }
    public string? ChoiceOptionNameSnapshot { get; protected set; }
    public decimal PriceAdjustment { get; protected set; }
    public int? SortOrder { get; protected set; }
}

