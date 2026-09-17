using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderCollectionServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Validate_RequiresScanAndValidatePermissions()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.ValidateQrAsync(
            Context(
                PosOnlineOrderCollectionService.AccessPermission,
                PosOnlineOrderCollectionService.OrdersViewPermission),
            OutletId,
            new PosOnlineOrderCollectionValidateRequest { Token = "token" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.ValidateCalls);
    }

    [Fact]
    public async Task Complete_RequiresHandoverAndCollectPermissions()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.CompleteAsync(
            Context(
                PosOnlineOrderCollectionService.AccessPermission,
                PosOnlineOrderCollectionService.OrdersViewPermission,
                PosOnlineOrderCollectionService.ScanQrPermission,
                PosOnlineOrderCollectionService.ValidateQrPermission),
            OutletId,
            OrderId,
            new PosOnlineOrderCollectionCompleteRequest { ExpectedVersion = 3 },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.CompleteCalls);
    }

    [Fact]
    public async Task Validate_EmptyToken_RejectedWithoutRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.ValidateQrAsync(
            AuthorizedValidateContext(),
            OutletId,
            new PosOnlineOrderCollectionValidateRequest { Token = "  " },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.collection.qr_invalid", result.Error.Code);
        Assert.Equal(0, repository.ValidateCalls);
    }

    [Fact]
    public async Task Validate_Success_IsReadOnly_PassesHashedToken()
    {
        var repository = new FakeRepository
        {
            ValidateResult = PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Success(new PosOnlineOrderCollectionValidateResponse
                {
                    OrderId = OrderId,
                    CanCollect = true
                })
        };
        var service = CreateService(repository);
        const string raw = "raw-collection-token";

        var result = await service.ValidateQrAsync(
            AuthorizedValidateContext(),
            OutletId,
            new PosOnlineOrderCollectionValidateRequest { Token = raw },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.ValidateCalls);
        Assert.Equal(0, repository.CompleteCalls);
        Assert.Equal(PosOnlineOrderCollectionService.HashToken(raw), repository.LastTokenHash);
        Assert.NotEqual(raw, repository.LastTokenHash);
    }

    [Fact]
    public async Task Complete_RequiresPositiveExpectedVersion()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.CompleteAsync(
            AuthorizedCompleteContext(),
            OutletId,
            OrderId,
            new PosOnlineOrderCollectionCompleteRequest { ExpectedVersion = 0 },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_expected_version", result.Error.Code);
        Assert.Equal(0, repository.CompleteCalls);
    }

    private static PosOnlineOrderCollectionService CreateService(FakeRepository repository) =>
        new(repository, new FakeEntitlements(allowed: true), new FakeClock());

    private static TenantRequestContext AuthorizedValidateContext() => Context(
        PosOnlineOrderCollectionService.AccessPermission,
        PosOnlineOrderCollectionService.OrdersViewPermission,
        PosOnlineOrderCollectionService.ScanQrPermission,
        PosOnlineOrderCollectionService.ValidateQrPermission);

    private static TenantRequestContext AuthorizedCompleteContext() => Context(
        PosOnlineOrderCollectionService.AccessPermission,
        PosOnlineOrderCollectionService.OrdersViewPermission,
        PosOnlineOrderCollectionService.HandoverPermission,
        PosOnlineOrderCollectionService.CollectPermission);

    private static TenantRequestContext Context(params string[] permissions) =>
        new(TenantId, UserId, permissions);

    private sealed class FakeRepository : IPosOnlineOrderCollectionRepository
    {
        public int ValidateCalls { get; private set; }
        public int CompleteCalls { get; private set; }
        public string? LastTokenHash { get; private set; }
        public PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>? ValidateResult { get; init; }
        public PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>? CompleteResult { get; init; }

        public Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>> ValidateQrAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, string tokenHash, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ValidateCalls++;
            LastTokenHash = tokenHash;
            return Task.FromResult(ValidateResult ??
                PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                    .Failure("online_orders.collection.qr_invalid"));
        }

        public Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>> CompleteAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, long expectedVersion,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            CompleteCalls++;
            return Task.FromResult(CompleteResult ??
                PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>
                    .Failure("online_orders.not_found"));
        }
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

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
