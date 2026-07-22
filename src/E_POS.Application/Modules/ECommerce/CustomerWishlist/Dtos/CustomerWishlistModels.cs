namespace E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;

public sealed class AddCustomerWishlistItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
}

public sealed class CustomerWishlistReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ItemCount => Items.Count;
    public IReadOnlyList<CustomerWishlistItemReadModel> Items { get; set; } = [];
}

public sealed class CustomerWishlistItemReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsInStock { get; set; }
    public bool IsAvailable { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
