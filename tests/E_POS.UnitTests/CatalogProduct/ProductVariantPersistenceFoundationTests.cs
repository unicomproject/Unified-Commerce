using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductVariantPersistenceFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Recommendation_create_normalizes_supported_values()
    {
        var link = CreateLink();

        Assert.Equal(ProductRecommendationConstants.FrequentlyBoughtTogetherType, link.RecommendationType);
        Assert.Equal(ProductRecommendationConstants.ActiveStatus, link.Status);
        Assert.Equal(0, link.SortOrder);
    }

    [Fact]
    public void Recommendation_create_rejects_self_reference()
    {
        var productId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() => CreateLink(productId, productId));
    }

    [Fact]
    public void Recommendation_create_rejects_negative_sort_order_and_invalid_dates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLink(sortOrder: -1));
        Assert.Throws<ArgumentException>(() => CreateLink(validFrom: Now.AddDays(1), validUntil: Now));
    }

    [Fact]
    public void Cart_note_is_trimmed_and_copied_to_checkout_snapshot()
    {
        var cartItem = ShoppingCartItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(),
            "SKU", "Product", "VARIABLE", 1, 10, 0, false, Now);
        cartItem.SetLineNote("  gift wrap  ", Now.AddMinutes(1));

        var checkoutLine = CheckoutSessionLine.CreateFromCartItem(
            Guid.NewGuid(), cartItem.TenantId, Guid.NewGuid(), cartItem, Now.AddMinutes(2));

        Assert.Equal("gift wrap", cartItem.LineNote);
        Assert.Equal("gift wrap", checkoutLine.LineNote);
    }

    [Fact]
    public void Ef_model_maps_nullable_line_notes_with_500_character_limit()
    {
        using var dbContext = CreateDbContext();

        AssertLineNote(dbContext, typeof(ShoppingCartItem));
        AssertLineNote(dbContext, typeof(CheckoutSessionLine));
        AssertLineNote(dbContext, typeof(SalesOrderLine));
    }

    private static ProductRecommendationLink CreateLink(
        Guid? sourceProductId = null,
        Guid? recommendedProductId = null,
        int sortOrder = 0,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null) =>
        ProductRecommendationLink.Create(
            Guid.NewGuid(), Guid.NewGuid(), sourceProductId ?? Guid.NewGuid(), null,
            recommendedProductId ?? Guid.NewGuid(), null,
            " frequently_bought_together ", null, null, sortOrder,
            validFrom, validUntil, " active ", null, Now);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase($"variant-persistence-{Guid.NewGuid()}")
            .Options;
        return new EPosDbContext(options);
    }

    private static void AssertLineNote(EPosDbContext dbContext, Type entityType)
    {
        var property = dbContext.Model.FindEntityType(entityType)?.FindProperty("LineNote");
        Assert.NotNull(property);
        Assert.True(property.IsNullable);
        Assert.Equal(500, property.GetMaxLength());
    }
}
