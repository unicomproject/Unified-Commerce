using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderDetailServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 4, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("commerce.online_order.orders.access")]
    [InlineData("commerce.online_order.orders.view")]
    public async Task GetAsync_WhenOneReadPermissionIsMissing_ReturnsForbidden(string grantedPermission)
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetAsync(
            new TenantRequestContext(TenantId, UserId, [grantedPermission]),
            OutletId,
            OrderId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task ListAsync_Authorized_ForwardsBoundedCanonicalQuery()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var query = new PosOnlineOrderListQuery(
            OutletId, "customer", null, "collectionTime", "asc", 1, 4);

        var result = await service.ListAsync(Context(), query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(query, repository.ListQuery);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(UserId, repository.UserId);
        Assert.Equal(Now, result.Value!.ServerTime);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ListAsync_InvalidPagination_DoesNotReadRepository(int page, int pageSize)
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.ListAsync(
            Context(), new PosOnlineOrderListQuery(OutletId, null, null, null, null, page, pageSize),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_pagination", result.Error.Code);
        Assert.Null(repository.ListQuery);
    }

    [Fact]
    public async Task GetAsync_WhenEntitlementIsDenied_DoesNotReadRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, entitlementAllowed: false);

        var result = await service.GetAsync(Context(), OutletId, OrderId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.feature_not_entitled", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_Authorized_ForwardsTenantUserOutletOrderAndServerTime()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetAsync(Context(), OutletId, OrderId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(UserId, repository.UserId);
        Assert.Equal(OutletId, repository.OutletId);
        Assert.Equal(OrderId, repository.OrderId);
        Assert.Equal(Now, repository.ServerTime);
        Assert.Null(result.Value!.CustomerClassification);
    }

    [Theory]
    [InlineData("online_orders.outlet_access_denied")]
    [InlineData("online_orders.not_found")]
    public async Task GetAsync_RepositoryDenial_PreservesStableCode(string code)
    {
        var repository = new FakeRepository { Result = PosOnlineOrderDetailRepositoryResult.Failure(code) };
        var service = CreateService(repository);

        var result = await service.GetAsync(Context(), OutletId, OrderId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error.Code);
    }

    private static TenantRequestContext Context() => new(
        TenantId,
        UserId,
        [PosOnlineOrderDetailService.AccessPermission, PosOnlineOrderDetailService.ViewPermission]);

    private static PosOnlineOrderDetailService CreateService(
        FakeRepository repository,
        bool entitlementAllowed = true) =>
        new(repository, new FakeEntitlements(entitlementAllowed), new FakeClock());

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeEntitlements(bool allowed) : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult(allowed
                ? TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false)
                : TenantFeatureEntitlementEvaluation.Denied(
                    TenantFeatureEntitlementDecision.Disabled,
                    featureCode,
                    featureCode,
                    false,
                    true,
                    false,
                    "Disabled"));

        public Task<bool> IsEnabledAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult(allowed);
    }

    private sealed class FakeRepository : IPosOnlineOrderDetailRepository
    {
        public PosOnlineOrderListQuery? ListQuery { get; private set; }

        public Task<PosOnlineOrderListRepositoryResult> ListAsync(
            Guid tenantId,
            Guid tenantUserId,
            PosOnlineOrderListQuery query,
            DateTimeOffset serverTime,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            UserId = tenantUserId;
            ServerTime = serverTime;
            ListQuery = query;
            return Task.FromResult(PosOnlineOrderListRepositoryResult.Success(
                new PosOnlineOrderListResponse(
                    [], new PosOnlineOrderSummaryResponse(0, 0, 0, 0, 0, 0),
                    query.Page, query.PageSize, 0, 0, serverTime)));
        }

        public PosOnlineOrderDetailRepositoryResult Result { get; init; } =
            PosOnlineOrderDetailRepositoryResult.Success(new PosOnlineOrderDetailResponse
            {
                Id = PosOnlineOrderDetailServiceTests.OrderId,
                OrderNumber = "EC-1001",
                CustomerName = "Customer",
                CustomerClassification = null,
                OutletId = PosOnlineOrderDetailServiceTests.OutletId,
                OutletName = "Main Store",
                ServerTime = Now
            });

        public int CallCount { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid OutletId { get; private set; }
        public Guid OrderId { get; private set; }
        public DateTimeOffset ServerTime { get; private set; }

        public Task<PosOnlineOrderDetailRepositoryResult> GetAsync(
            Guid tenantId,
            Guid tenantUserId,
            Guid outletId,
            Guid orderId,
            DateTimeOffset serverTime,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            UserId = tenantUserId;
            OutletId = outletId;
            OrderId = orderId;
            ServerTime = serverTime;
            return Task.FromResult(Result);
        }
    }
}
