using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.IntegrationTests.ECommerce.CustomerWishlist;

public sealed class CustomerWishlistRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_NoWishlist_ReturnsNonPersistedEmptyDefaultWishlist()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var repository = new CustomerWishlistRepository(dbContext);

        var result = await repository.GetAsync(
            tenantId,
            customerId,
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Wishlist!.Id);
        Assert.Equal(customerId, result.Wishlist.CustomerId);
        Assert.Equal("My Wishlist", result.Wishlist.Name);
        Assert.Empty(result.Wishlist.Items);
        Assert.Empty(dbContext.CustomerWishlists);
    }

    [Fact]
    public async Task AddItemAsync_CreatesWishlistMapsCatalogDataAndIsIdempotent()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Customers.Add(CreateCustomer(tenantId, customerId));
        dbContext.Products.Add(CreateProduct(tenantId, productId));
        dbContext.PriceListItems.Add(PriceListItem.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), productId, null, null,
            74.99m, null, 1m, null, null, "ACTIVE", null, Now));
        dbContext.ProductImages.Add(ProductImage.Create(
            Guid.NewGuid(), tenantId, productId, null, null,
            "jersey-main", "/images/jersey.jpg", "Jersey", "MAIN", "image/jpeg",
            null, null, null, null, 0, true, "ACTIVE", null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerWishlistRepository(dbContext);
        var request = new AddCustomerWishlistItemRequest { ProductId = productId };

        var first = await repository.AddItemAsync(
            tenantId, customerId, request, Now, CancellationToken.None);
        var second = await repository.AddItemAsync(
            tenantId, customerId, request, Now.AddMinutes(1), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var item = Assert.Single(second.Wishlist!.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Home Jersey", item.ProductName);
        Assert.Equal("home-jersey", item.ProductSlug);
        Assert.Equal(74.99m, item.Price);
        Assert.Equal("/images/jersey.jpg", item.ImageUrl);
        Assert.True(item.IsInStock);
        Assert.True(item.IsAvailable);
        Assert.Single(dbContext.CustomerWishlists);
        Assert.Single(dbContext.CustomerWishlistItems);
    }

    [Fact]
    public async Task AddItemAsync_RejectsProductFromAnotherTenant()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var otherTenantProductId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Customers.Add(CreateCustomer(tenantId, customerId));
        dbContext.Products.Add(CreateProduct(Guid.NewGuid(), otherTenantProductId));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerWishlistRepository(dbContext);

        var result = await repository.AddItemAsync(
            tenantId,
            customerId,
            new AddCustomerWishlistItemRequest { ProductId = otherTenantProductId },
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_wishlist.product_not_found", result.ErrorCode);
        Assert.Empty(dbContext.CustomerWishlists);
    }

    [Fact]
    public async Task RemoveAndClear_OperateOnlyOnAuthenticatedCustomersWishlist()
    {
        var tenantId = Guid.NewGuid();
        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Customers.AddRange(
            CreateCustomer(tenantId, firstCustomerId, "C-001"),
            CreateCustomer(tenantId, secondCustomerId, "C-002"));
        dbContext.Products.AddRange(
            CreateProduct(tenantId, firstProductId, "HOME"),
            CreateProduct(tenantId, secondProductId, "AWAY"));
        await dbContext.SaveChangesAsync();
        var repository = new CustomerWishlistRepository(dbContext);
        var firstAdd = await repository.AddItemAsync(
            tenantId,
            firstCustomerId,
            new AddCustomerWishlistItemRequest { ProductId = firstProductId },
            Now,
            CancellationToken.None);
        await repository.AddItemAsync(
            tenantId,
            firstCustomerId,
            new AddCustomerWishlistItemRequest { ProductId = secondProductId },
            Now.AddMinutes(1),
            CancellationToken.None);
        var firstItemId = firstAdd.Wishlist!.Items.Single().Id;

        var crossCustomerRemove = await repository.RemoveItemAsync(
            tenantId,
            secondCustomerId,
            firstItemId,
            Now.AddMinutes(2),
            CancellationToken.None);
        var ownRemove = await repository.RemoveItemAsync(
            tenantId,
            firstCustomerId,
            firstItemId,
            Now.AddMinutes(3),
            CancellationToken.None);
        var cleared = await repository.ClearAsync(
            tenantId,
            firstCustomerId,
            Now.AddMinutes(4),
            CancellationToken.None);

        Assert.Equal("customer_wishlist.item_not_found", crossCustomerRemove.ErrorCode);
        Assert.True(ownRemove.IsSuccess);
        Assert.Single(ownRemove.Wishlist!.Items);
        Assert.True(cleared.IsSuccess);
        Assert.Empty(cleared.Wishlist!.Items);
        Assert.Empty(dbContext.CustomerWishlistItems);
    }

    private static CustomerEntity CreateCustomer(
        Guid tenantId,
        Guid customerId,
        string customerCode = "C-001") =>
        CustomerEntity.CreatePosCustomer(
            customerId,
            tenantId,
            customerCode,
            "Test Customer",
            "+94770000000",
            $"{customerId:N}@example.com",
            Guid.NewGuid(),
            Now);

    private static Product CreateProduct(
        Guid tenantId,
        Guid productId,
        string code = "HOME") =>
        Product.Create(
            productId,
            tenantId,
            code,
            code == "HOME" ? "Home Jersey" : "Away Jersey",
            $"{code.ToLowerInvariant()}-jersey",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
