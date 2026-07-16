using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class CheckoutSessionLineOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CheckoutSessionLineId { get; protected set; }
    public Guid ChoiceGroupId { get; protected set; }
    public Guid ChoiceOptionId { get; protected set; }
    public string? ChoiceGroupNameSnapshot { get; protected set; }
    public string? ChoiceOptionNameSnapshot { get; protected set; }
    public decimal Quantity { get; protected set; }
    public decimal PriceAdjustment { get; protected set; }
    public int? SortOrder { get; protected set; }

    protected CheckoutSessionLineOption() { }

    public static CheckoutSessionLineOption CreateFromCartItemOption(
        Guid id,
        Guid tenantId,
        Guid checkoutSessionLineId,
        ShoppingCartItemOption option,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (option.TenantId != tenantId)
            throw new InvalidOperationException("Cart option tenant does not match checkout tenant.");

        return new CheckoutSessionLineOption
        {
            Id = id,
            TenantId = tenantId,
            CheckoutSessionLineId = checkoutSessionLineId,
            ChoiceGroupId = option.ChoiceGroupId,
            ChoiceOptionId = option.ChoiceOptionId,
            ChoiceGroupNameSnapshot = option.ChoiceGroupNameSnapshot,
            ChoiceOptionNameSnapshot = option.ChoiceOptionNameSnapshot,
            Quantity = 1m,
            PriceAdjustment = option.PriceAdjustment,
            SortOrder = option.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

