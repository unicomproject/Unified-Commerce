using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerWishlist : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private readonly List<CustomerWishlistItem> _items = new();
    public IReadOnlyCollection<CustomerWishlistItem> Items => _items.AsReadOnly();

    protected CustomerWishlist() { } // EF Core

    public static CustomerWishlist Create(Guid tenantId, Guid customerId, string name)
    {
        return new CustomerWishlist
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            Name = name
        };
    }

    public void AddItem(Guid productId, Guid? productVariantId = null)
    {
        if (_items.Any(i => i.ProductId == productId && i.ProductVariantId == productVariantId))
        {
            return; // Already exists
        }

        _items.Add(CustomerWishlistItem.Create(TenantId, Id, productId, productVariantId));
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
        }
    }
}
