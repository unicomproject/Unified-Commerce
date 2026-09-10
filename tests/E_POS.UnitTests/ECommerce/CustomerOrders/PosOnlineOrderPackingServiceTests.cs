using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderPackingServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Pack_RequiresAccessOrdersViewPackingViewAndPack()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var missingPack = await service.PackAsync(
            Context(
                PosOnlineOrderPackingService.AccessPermission,
                PosOnlineOrderPackingService.OrdersViewPermission,
                PosOnlineOrderPackingService.PackingViewPermission),
            OutletId, OrderId, PackRequest(), CancellationToken.None);

        Assert.False(missingPack.IsSuccess);
        Assert.Equal("online_orders.permission_denied", missingPack.Error.Code);
        Assert.Equal(0, repository.PackCalls);
    }

    [Fact]
    public async Task Ready_RequiresCollectionMarkReady()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var missingReady = await service.MarkReadyAsync(
            Context(
                PosOnlineOrderPackingService.AccessPermission,
                PosOnlineOrderPackingService.OrdersViewPermission,
                PosOnlineOrderPackingService.PackingViewPermission,
                PosOnlineOrderPackingService.PackPermission),
            OutletId, OrderId, ReadyRequest(), CancellationToken.None);

        Assert.False(missingReady.IsSuccess);
        Assert.Equal("online_orders.permission_denied", missingReady.Error.Code);
        Assert.Equal(0, repository.ReadyCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Pack_RequiresPositiveExpectedVersion(long version)
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.PackAsync(
            AuthorizedPackContext(), OutletId, OrderId,
            new PosOnlineOrderPackRequest { ExpectedVersion = version }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_expected_version", result.Error.Code);
        Assert.Equal(0, repository.PackCalls);
    }

    [Fact]
    public async Task Pack_NoteOverMaxLength_RejectedBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.PackAsync(
            AuthorizedPackContext(), OutletId, OrderId,
            new PosOnlineOrderPackRequest
            {
                ExpectedVersion = 7,
                PackingNote = new string('x', PosOnlineOrderPackingService.PackingNoteMaxLength + 1)
            }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_packing_note", result.Error.Code);
        Assert.Equal(0, repository.PackCalls);
    }

    [Fact]
    public async Task Pack_WhitespaceNote_NormalizedToNull()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.PackAsync(
            AuthorizedPackContext(), OutletId, OrderId,
            new PosOnlineOrderPackRequest { ExpectedVersion = 7, PackingNote = "   " },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.PackCalls);
        Assert.Null(repository.PackRequest?.PackingNote);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task Pack_MaxLengthNote_AcceptedAndTrimmed()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var note = new string('a', PosOnlineOrderPackingService.PackingNoteMaxLength);

        var result = await service.PackAsync(
            AuthorizedPackContext(), OutletId, OrderId,
            new PosOnlineOrderPackRequest { ExpectedVersion = 7, PackingNote = $"  {note}  " },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(note, repository.PackRequest?.PackingNote);
    }

    [Fact]
    public async Task Pack_Authorized_ForwardsServerTimeAndVersion()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.PackAsync(
            AuthorizedPackContext(), OutletId, OrderId, PackRequest(" Fragile "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Fragile", repository.PackRequest?.PackingNote);
        Assert.Equal(7, repository.PackRequest?.ExpectedVersion);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task Ready_Authorized_ForwardsServerTime()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.MarkReadyAsync(
            AuthorizedReadyContext(), OutletId, OrderId, ReadyRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.ReadyCalls);
        Assert.Equal(7, repository.ReadyRequest?.ExpectedVersion);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task MissingEntitlement_StopsBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, allowed: false);

        var result = await service.PackAsync(
            AuthorizedPackContext(), OutletId, OrderId, PackRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.feature_not_entitled", result.Error.Code);
        Assert.Equal(0, repository.PackCalls);
    }

    private static PosOnlineOrderPackRequest PackRequest(string? note = null) => new()
    {
        ExpectedVersion = 7,
        PackingNote = note
    };

    private static PosOnlineOrderReadyRequest ReadyRequest() => new() { ExpectedVersion = 7 };

    private static TenantRequestContext AuthorizedPackContext() => Context(
        PosOnlineOrderPackingService.AccessPermission,
        PosOnlineOrderPackingService.OrdersViewPermission,
        PosOnlineOrderPackingService.PackingViewPermission,
        PosOnlineOrderPackingService.PackPermission);

    private static TenantRequestContext AuthorizedReadyContext() => Context(
        PosOnlineOrderPackingService.AccessPermission,
        PosOnlineOrderPackingService.OrdersViewPermission,
        PosOnlineOrderPackingService.PackingViewPermission,
        PosOnlineOrderPackingService.MarkReadyPermission);

    private static TenantRequestContext Context(params string[] permissions) =>
        new(TenantId, UserId, permissions);

    private static PosOnlineOrderPackingService CreateService(FakeRepository repository, bool allowed = true) =>
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

    private sealed class FakeRepository : IPosOnlineOrderPackingRepository
    {
        public int PackCalls { get; private set; }
        public int ReadyCalls { get; private set; }
        public DateTimeOffset Now { get; private set; }
        public PosOnlineOrderPackRequest? PackRequest { get; private set; }
        public PosOnlineOrderReadyRequest? ReadyRequest { get; private set; }

        public Task<PosOnlineOrderPackingRepositoryResult> PackAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
            PosOnlineOrderPackRequest request, DateTimeOffset now, CancellationToken cancellationToken)
        {
            PackCalls++;
            PackRequest = request;
            Now = now;
            return Task.FromResult(PosOnlineOrderPackingRepositoryResult.Success(
                new PosOnlineOrderPackReadyCommandResponse { FulfillmentVersion = 8 }));
        }

        public Task<PosOnlineOrderPackingRepositoryResult> MarkReadyAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
            PosOnlineOrderReadyRequest request, DateTimeOffset now, CancellationToken cancellationToken)
        {
            ReadyCalls++;
            ReadyRequest = request;
            Now = now;
            return Task.FromResult(PosOnlineOrderPackingRepositoryResult.Success(
                new PosOnlineOrderPackReadyCommandResponse { FulfillmentVersion = 9 }));
        }
    }
}
