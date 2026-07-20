using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerWishlist;

public sealed class CustomerWishlistServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_InvalidCustomerContext_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetAsync(
            Guid.Empty,
            CustomerId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("customer_wishlist.invalid_customer_context", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task AddItemAsync_ValidRequest_ForwardsAuthenticatedContextAndClock()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var wishlist = CreateWishlist(productId, variantId);
        var repository = new FakeRepository
        {
            AddResult = CustomerWishlistRepositoryResult.Success(wishlist)
        };
        var service = CreateService(repository);
        var request = new AddCustomerWishlistItemRequest
        {
            ProductId = productId,
            ProductVariantId = variantId
        };

        var result = await service.AddItemAsync(
            TenantId,
            CustomerId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(wishlist, result.Value);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(CustomerId, repository.CustomerId);
        Assert.Equal(Now, repository.Now);
        Assert.Same(request, repository.AddRequest);
    }

    [Fact]
    public async Task AddItemAsync_EmptyProductId_ReturnsValidationError()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.AddItemAsync(
            TenantId,
            CustomerId,
            new AddCustomerWishlistItemRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("customer_wishlist.invalid_product_id", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotFound_MapsRepositoryError()
    {
        var repository = new FakeRepository
        {
            RemoveResult = CustomerWishlistRepositoryResult.Failure(
                "customer_wishlist.item_not_found")
        };
        var service = CreateService(repository);

        var result = await service.RemoveItemAsync(
            TenantId,
            CustomerId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("customer_wishlist.item_not_found", result.Error.Code);
        Assert.Equal("Wishlist item not found or access denied.", result.Error.Message);
    }

    private static CustomerWishlistService CreateService(FakeRepository repository) =>
        new(repository, new FakeDateTimeProvider());

    private static CustomerWishlistReadModel CreateWishlist(
        Guid productId,
        Guid? variantId = null) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = CustomerId,
        Name = "My Wishlist",
        Items =
        [
            new CustomerWishlistItemReadModel
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductVariantId = variantId,
                ProductName = "Jersey",
                ProductSlug = "jersey",
                Price = 74.99m,
                IsAvailable = true,
                IsInStock = true,
                AddedAt = Now
            }
        ]
    };

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeRepository : ICustomerWishlistRepository
    {
        public CustomerWishlistRepositoryResult GetResult { get; init; } =
            CustomerWishlistRepositoryResult.Success(new CustomerWishlistReadModel());
        public CustomerWishlistRepositoryResult AddResult { get; init; } =
            CustomerWishlistRepositoryResult.Success(new CustomerWishlistReadModel());
        public CustomerWishlistRepositoryResult RemoveResult { get; init; } =
            CustomerWishlistRepositoryResult.Success(new CustomerWishlistReadModel());
        public CustomerWishlistRepositoryResult ClearResult { get; init; } =
            CustomerWishlistRepositoryResult.Success(new CustomerWishlistReadModel());
        public int CallCount { get; private set; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public DateTimeOffset? Now { get; private set; }
        public AddCustomerWishlistItemRequest? AddRequest { get; private set; }

        public Task<CustomerWishlistRepositoryResult> GetAsync(
            Guid tenantId,
            Guid customerId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, now);
            return Task.FromResult(GetResult);
        }

        public Task<CustomerWishlistRepositoryResult> AddItemAsync(
            Guid tenantId,
            Guid customerId,
            AddCustomerWishlistItemRequest request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, now);
            AddRequest = request;
            return Task.FromResult(AddResult);
        }

        public Task<CustomerWishlistRepositoryResult> RemoveItemAsync(
            Guid tenantId,
            Guid customerId,
            Guid itemId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, now);
            return Task.FromResult(RemoveResult);
        }

        public Task<CustomerWishlistRepositoryResult> ClearAsync(
            Guid tenantId,
            Guid customerId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId, now);
            return Task.FromResult(ClearResult);
        }

        private void Capture(Guid tenantId, Guid customerId, DateTimeOffset now)
        {
            CallCount++;
            TenantId = tenantId;
            CustomerId = customerId;
            Now = now;
        }
    }
}
