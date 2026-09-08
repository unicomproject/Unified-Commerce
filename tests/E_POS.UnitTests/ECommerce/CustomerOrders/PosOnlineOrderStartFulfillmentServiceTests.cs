using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderStartFulfillmentServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartAsync_MissingPermission_IsDeniedBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.StartAsync(
            new TenantRequestContext(TenantId, UserId, []), OutletId, OrderId,
            new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = 5 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task StartAsync_MissingEntitlement_IsDeniedBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, false);

        var result = await service.StartAsync(Context(), OutletId, OrderId,
            new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = 5 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.feature_not_entitled", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task StartAsync_InvalidExpectedVersion_IsRejected(long version)
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.StartAsync(Context(), OutletId, OrderId,
            new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = version }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_expected_version", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task StartAsync_Authorized_ForwardsExpectedVersionAndAuthoritativeTime()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.StartAsync(Context(), OutletId, OrderId,
            new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = 5 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(UserId, repository.UserId);
        Assert.Equal(OutletId, repository.OutletId);
        Assert.Equal(OrderId, repository.OrderId);
        Assert.Equal(5, repository.ExpectedVersion);
        Assert.Equal(Now, repository.Now);
    }

    [Theory]
    [InlineData("online_orders.concurrency_conflict")]
    [InlineData("online_orders.invalid_state")]
    [InlineData("online_orders.invalid_reservation")]
    [InlineData("online_orders.not_found")]
    public async Task StartAsync_RepositoryFailure_PreservesStableCode(string code)
    {
        var repository = new FakeRepository
        {
            Result = PosOnlineOrderStartFulfillmentRepositoryResult.Failure(code)
        };
        var service = CreateService(repository);

        var result = await service.StartAsync(Context(), OutletId, OrderId,
            new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = 5 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error.Code);
    }

    private static TenantRequestContext Context() => new(
        TenantId, UserId, [PosOnlineOrderStartFulfillmentService.StartPermission]);

    private static PosOnlineOrderStartFulfillmentService CreateService(FakeRepository repository, bool allowed = true) =>
        new(repository, new FakeEntitlements(allowed), new FakeClock());

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeEntitlements(bool allowed) : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId, string featureCode, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(allowed
                ? TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false)
                : TenantFeatureEntitlementEvaluation.Denied(
                    TenantFeatureEntitlementDecision.Disabled, featureCode, featureCode,
                    false, true, false, "Disabled"));

        public Task<bool> IsEnabledAsync(
            Guid tenantId, string featureCode, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(allowed);
    }

    private sealed class FakeRepository : IPosOnlineOrderStartFulfillmentRepository
    {
        public PosOnlineOrderStartFulfillmentRepositoryResult Result { get; init; } =
            PosOnlineOrderStartFulfillmentRepositoryResult.Success(new PosOnlineOrderStartFulfillmentResponse());
        public int CallCount { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid OutletId { get; private set; }
        public Guid OrderId { get; private set; }
        public long ExpectedVersion { get; private set; }
        public DateTimeOffset Now { get; private set; }

        public Task<PosOnlineOrderStartFulfillmentRepositoryResult> StartAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
            long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            UserId = tenantUserId;
            OutletId = outletId;
            OrderId = orderId;
            ExpectedVersion = expectedVersion;
            Now = now;
            return Task.FromResult(Result);
        }
    }
}
