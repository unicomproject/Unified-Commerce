using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Application.Modules.ECommerce.CartCheckout.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CartCheckout;

public sealed class StorefrontCartServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddItemAsync_ValidRequest_ForwardsNormalizedContext()
    {
        var repository = new FakeRepository();
        var service = new StorefrontCartService(repository, new FakeClock());
        var request = new AddStorefrontCartItemRequest
        {
            ProductId = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            Quantity = 2m
        };

        var result = await service.AddItemAsync(TenantId, " session-1 ", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal("session-1", repository.SessionId);
        Assert.Same(request, repository.AddRequest);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task AddItemAsync_InvalidQuantity_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = new StorefrontCartService(repository, new FakeClock());

        var result = await service.AddItemAsync(
            TenantId,
            "session",
            new AddStorefrontCartItemRequest { ProductId = Guid.NewGuid(), Quantity = 0m },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_cart.invalid_quantity", result.Error.Code);
        Assert.Null(repository.AddRequest);
    }

    [Fact]
    public async Task GetAsync_MissingSession_ReturnsValidationError()
    {
        var service = new StorefrontCartService(new FakeRepository(), new FakeClock());

        var result = await service.GetAsync(TenantId, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_cart.invalid_session", result.Error.Code);
    }

    [Fact]
    public async Task UpdateItemAsync_RepositoryStockFailure_ReturnsSafeBusinessError()
    {
        var repository = new FakeRepository { ErrorCode = "storefront_cart.insufficient_stock" };
        var service = new StorefrontCartService(repository, new FakeClock());

        var result = await service.UpdateItemAsync(
            TenantId,
            "session",
            Guid.NewGuid(),
            new UpdateStorefrontCartItemRequest { Quantity = 5m },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_cart.insufficient_stock", result.Error.Code);
        Assert.Equal("The requested quantity is not available.", result.Error.Message);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeRepository : IStorefrontCartRepository
    {
        public string? ErrorCode { get; init; }
        public Guid? TenantId { get; private set; }
        public string? SessionId { get; private set; }
        public AddStorefrontCartItemRequest? AddRequest { get; private set; }
        public DateTimeOffset? Now { get; private set; }

        public Task<StorefrontCartRepositoryResult> GetAsync(Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken)
            => Result(tenantId, sessionId, now);

        public Task<StorefrontCartRepositoryResult> AddItemAsync(Guid tenantId, string sessionId, AddStorefrontCartItemRequest request, DateTimeOffset now, CancellationToken cancellationToken)
        {
            AddRequest = request;
            return Result(tenantId, sessionId, now);
        }

        public Task<StorefrontCartRepositoryResult> UpdateItemAsync(Guid tenantId, string sessionId, Guid itemId, decimal quantity, DateTimeOffset now, CancellationToken cancellationToken)
            => Result(tenantId, sessionId, now);

        public Task<StorefrontCartRepositoryResult> RemoveItemAsync(Guid tenantId, string sessionId, Guid itemId, DateTimeOffset now, CancellationToken cancellationToken)
            => Result(tenantId, sessionId, now);

        public Task<StorefrontCartRepositoryResult> ClearAsync(Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken)
            => Result(tenantId, sessionId, now);

        private Task<StorefrontCartRepositoryResult> Result(Guid tenantId, string sessionId, DateTimeOffset now)
        {
            TenantId = tenantId;
            SessionId = sessionId;
            Now = now;
            return Task.FromResult(ErrorCode is null
                ? StorefrontCartRepositoryResult.Success(new StorefrontCartReadModel())
                : StorefrontCartRepositoryResult.Failure(ErrorCode));
        }
    }
}
