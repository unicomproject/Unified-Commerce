using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Application.Modules.ECommerce.CartCheckout.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CartCheckout;

public sealed class StorefrontCheckoutServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateFromCartAsync_ValidRequest_ForwardsNormalizedCustomerContext()
    {
        var repository = new FakeRepository();
        var service = new StorefrontCheckoutService(repository, new FakeClock());
        var request = new CreateStorefrontCheckoutFromCartRequest
        {
            SelectedOutletId = Guid.NewGuid(),
            PickupContactEmail = "customer@example.com"
        };

        var result = await service.CreateFromCartAsync(
            TenantId, CustomerId, " guest-session ", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(CustomerId, repository.CustomerId);
        Assert.Equal("guest-session", repository.CartSessionId);
        Assert.Same(request, repository.CreateRequest);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task CreateFromCartAsync_MissingCartSession_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = new StorefrontCheckoutService(repository, new FakeClock());

        var result = await service.CreateFromCartAsync(
            TenantId,
            CustomerId,
            null,
            new CreateStorefrontCheckoutFromCartRequest { SelectedOutletId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.invalid_cart_session", result.Error.Code);
        Assert.Null(repository.CreateRequest);
    }

    [Fact]
    public async Task ConfirmAsync_MissingIdempotencyKey_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = new StorefrontCheckoutService(repository, new FakeClock());

        var result = await service.ConfirmAsync(
            TenantId, CustomerId, Guid.NewGuid(), " ", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.invalid_idempotency_key", result.Error.Code);
        Assert.Null(repository.IdempotencyKey);
    }

    [Fact]
    public async Task ConfirmAsync_IdempotencyKeyAboveDatabaseLimit_DoesNotCallRepository()
    {
        var repository = new FakeRepository();
        var service = new StorefrontCheckoutService(repository, new FakeClock());

        var result = await service.ConfirmAsync(
            TenantId, CustomerId, Guid.NewGuid(), new string('x', 51), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.invalid_idempotency_key", result.Error.Code);
        Assert.Null(repository.IdempotencyKey);
    }

    [Fact]
    public async Task ConfirmAsync_ExpiredRepositoryResult_ReturnsSafeConflictError()
    {
        var repository = new FakeRepository { ErrorCode = "storefront_checkout.session_expired" };
        var service = new StorefrontCheckoutService(repository, new FakeClock());

        var result = await service.ConfirmAsync(
            TenantId, CustomerId, Guid.NewGuid(), "confirm-1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("storefront_checkout.session_expired", result.Error.Code);
        Assert.Equal("The checkout session has expired.", result.Error.Message);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeRepository : IStorefrontCheckoutRepository
    {
        public string? ErrorCode { get; init; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public string? CartSessionId { get; private set; }
        public CreateStorefrontCheckoutFromCartRequest? CreateRequest { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public DateTimeOffset? Now { get; private set; }

        public Task<StorefrontCheckoutRepositoryResult> CreateFromCartAsync(
            Guid tenantId,
            Guid customerId,
            string cartSessionId,
            CreateStorefrontCheckoutFromCartRequest request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CustomerId = customerId;
            CartSessionId = cartSessionId;
            CreateRequest = request;
            Now = now;
            return Result();
        }

        public Task<StorefrontCheckoutRepositoryResult> GetAsync(
            Guid tenantId,
            Guid customerId,
            Guid checkoutSessionId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CustomerId = customerId;
            return Result();
        }

        public Task<StorefrontCheckoutRepositoryResult> ConfirmAsync(
            Guid tenantId,
            Guid customerId,
            Guid checkoutSessionId,
            string idempotencyKey,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CustomerId = customerId;
            IdempotencyKey = idempotencyKey;
            Now = now;
            return Result();
        }

        private Task<StorefrontCheckoutRepositoryResult> Result() =>
            Task.FromResult(ErrorCode is null
                ? StorefrontCheckoutRepositoryResult.Success(new StorefrontCheckoutReadModel())
                : StorefrontCheckoutRepositoryResult.Failure(ErrorCode));
    }
}
