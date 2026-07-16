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

    public static CustomerWishlist Create(
        Guid tenantId,
        Guid customerId,
        string name,
        DateTimeOffset now)
    {
        return new CustomerWishlist
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            Name = name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public CustomerWishlistItem AddItem(
        Guid productId,
        Guid? productVariantId,
        DateTimeOffset now)
    {
        var existingItem = _items.FirstOrDefault(
            i => i.ProductId == productId && i.ProductVariantId == productVariantId);
        if (existingItem is not null)
        {
            return existingItem;
        }

        var item = CustomerWishlistItem.Create(
            TenantId,
            Id,
            productId,
            productVariantId,
            now);
        _items.Add(item);
        UpdatedAt = now;
        return item;
    }

    public bool RemoveItem(Guid itemId, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return false;

        _items.Remove(item);
        UpdatedAt = now;
        return true;
    }

    public void Clear(DateTimeOffset now)
    {
        if (_items.Count == 0)
            return;

        _items.Clear();
        UpdatedAt = now;
    }
}
