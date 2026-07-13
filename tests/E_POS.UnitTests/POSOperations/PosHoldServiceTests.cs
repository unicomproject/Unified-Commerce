using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosHoldServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CancelHoldAsync_WithCreatePermission_ReturnsSuccess()
    {
        var repository = new FakeRepository
        {
            CancelResult = new PosCancelHoldRepositoryResult(null)
        };
        var service = new PosHoldService(repository, new FakeClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            Guid.NewGuid(), "Customer left", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CancelHoldAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        var service = new PosHoldService(new FakeRepository(), new FakeClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []),
            Guid.NewGuid(), null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task RecallHoldAsync_WithRecallPermission_ReturnsRecalculatedCart()
    {
        var holdId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var response = new PosRecallHoldResponseDto(
            holdId, Guid.NewGuid(), "HOLD-000001", deviceId, null, null,
            "NewSale", null, Now, [],
            new PosCheckoutSummaryResponseDto(
                new PosCheckoutBillingSummaryDto(0, 0, 0, 0, 0, "LKR"),
                new PosCheckoutSaleDetailsDto("New Sale", 0, Now, "Cashier"), [], []));
        var repository = new FakeRepository
        {
            RecallResult = new PosRecallHoldRepositoryResult(null, response)
        };
        var service = new PosHoldService(repository, new FakeClock());

        var result = await service.RecallHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Recall]),
            holdId, new PosRecallHoldRequestDto(deviceId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(holdId, result.Value!.HoldId);
    }

    [Fact]
    public async Task RecallHoldAsync_WithoutRecallPermission_ReturnsPermissionDenied()
    {
        var service = new PosHoldService(new FakeRepository(), new FakeClock());

        var result = await service.RecallHoldAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []),
            Guid.NewGuid(), new PosRecallHoldRequestDto(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateHoldAsync_WithCreatePermission_ReturnsCreatedHold()
    {
        var expected = new PosHoldListItemDto(
            Guid.NewGuid(), "HOLD-000001", Guid.NewGuid(), "SO-000001",
            Guid.NewGuid(), Guid.NewGuid(), null, null, "Waiting", "held",
            1, 100, 0, 0, 100, "LKR", Now, null, []);
        var repository = new FakeRepository
        {
            CreateResult = new PosCreateHoldRepositoryResult(null, expected)
        };
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]);
        var request = new PosCreateHoldRequestDto(
            Guid.NewGuid(), "NewSale", null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)],
            "Waiting", null, "key-1");

        var result = await service.CreateHoldAsync(context, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected.HoldId, result.Value!.HoldId);
    }

    [Fact]
    public async Task CreateHoldAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = new PosHoldService(new FakeRepository(), new FakeClock());
        var request = new PosCreateHoldRequestDto(
            Guid.NewGuid(), "NewSale", null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)], null, null, "key-1");

        var result = await service.CreateHoldAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []),
            request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetHoldsAsync_WithViewPermission_ReturnsRepositoryItems()
    {
        var repository = new FakeRepository();
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.View]);

        var result = await service.GetHoldsAsync(context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalCount);
        Assert.Equal(context.TenantId, repository.TenantId);
        Assert.Equal(context.UserId, repository.UserId);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task GetHoldsAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var repository = new FakeRepository();
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []);

        var result = await service.GetHoldsAsync(context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.permission_denied", result.Error.Code);
        Assert.Null(repository.TenantId);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeRepository : IPosHoldRepository
    {
        public PosCancelHoldRepositoryResult CancelResult { get; init; } =
            new("pos_holds.cancel_failed");
        public PosRecallHoldRepositoryResult RecallResult { get; init; } =
            new("pos_holds.recall_failed", null);
        public PosCreateHoldRepositoryResult CreateResult { get; init; } =
            new("pos_holds.create_failed", null);
        public Guid? TenantId { get; private set; }
        public Guid? UserId { get; private set; }
        public DateTimeOffset? Now { get; private set; }

        public Task<PosCancelHoldRepositoryResult> CancelHoldAsync(
            Guid tenantId,
            Guid tenantUserId,
            Guid holdId,
            string? reason,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult(CancelResult);

        public Task<PosRecallHoldRepositoryResult> RecallHoldAsync(
            Guid tenantId,
            Guid tenantUserId,
            IReadOnlyCollection<string> permissions,
            Guid holdId,
            PosRecallHoldRequestDto request,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult(RecallResult);

        public Task<PosCreateHoldRepositoryResult> CreateHoldAsync(
            Guid tenantId,
            Guid tenantUserId,
            IReadOnlyCollection<string> permissions,
            PosCreateHoldRequestDto request,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult(CreateResult);

        public Task<IReadOnlyList<PosHoldListItemDto>> GetActiveHoldsAsync(
            Guid tenantId,
            Guid tenantUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            UserId = tenantUserId;
            Now = now;
            return Task.FromResult<IReadOnlyList<PosHoldListItemDto>>(
                Array.Empty<PosHoldListItemDto>());
        }
    }
}
