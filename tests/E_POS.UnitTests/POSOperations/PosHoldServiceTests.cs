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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CancelHoldAsync_WithoutReason_ReturnsInvalidReason(string? reason)
    {
        var service = new PosHoldService(new FakeRepository(), new FakeClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            Guid.NewGuid(), reason, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.invalid_reason", result.Error.Code);
    }

    [Fact]
    public async Task CancelHoldAsync_WithReasonExceedingMaxLength_ReturnsInvalidReason()
    {
        var service = new PosHoldService(new FakeRepository(), new FakeClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            Guid.NewGuid(), new string('a', 251), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.invalid_reason", result.Error.Code);
    }

    [Fact]
    public async Task CancelHoldAsync_WithTrimmedReasonOfExactly250Characters_Succeeds()
    {
        var repository = new FakeRepository
        {
            CancelResult = new PosCancelHoldRepositoryResult(null)
        };
        var service = new PosHoldService(repository, new FakeClock());
        var reason = new string('b', 250);

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            Guid.NewGuid(), reason, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(reason, repository.LastCancelReason);
    }

    [Fact]
    public async Task CancelHoldAsync_TrimsReasonBeforePersisting()
    {
        var repository = new FakeRepository
        {
            CancelResult = new PosCancelHoldRepositoryResult(null)
        };
        var service = new PosHoldService(repository, new FakeClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            Guid.NewGuid(), "  Customer left  ", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Customer left", repository.LastCancelReason);
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
                new PosCheckoutSaleDetailsDto("New Sale", 0, Now, "Cashier"), [], []),
            []);
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
    public async Task RecallHoldAsync_WithStockWarnings_PassesThroughToResponse()
    {
        var holdId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var response = new PosRecallHoldResponseDto(
            holdId, Guid.NewGuid(), "HOLD-000001", deviceId, null, null,
            "NewSale", null, Now, [],
            new PosCheckoutSummaryResponseDto(
                new PosCheckoutBillingSummaryDto(0, 0, 0, 0, 0, "LKR"),
                new PosCheckoutSaleDetailsDto("New Sale", 0, Now, "Cashier"), [], []),
            ["Insufficient stock for SKU-1."]);
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
        Assert.Single(result.Value!.StockWarnings);
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
            Guid.NewGuid(), "PS-2026-00001", Guid.NewGuid(), "SO-000001",
            Guid.NewGuid(), Guid.NewGuid(), null, null, "Waiting", "held",
            1, 100, 0, 0, 100, "LKR", Now, Now.AddHours(24), []);
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
        Assert.Equal(Now, repository.HeldAt);
        Assert.Equal(Now.AddHours(24), repository.ExpiresAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(72)]
    public async Task CreateHoldAsync_ClientExpiryCannotOverrideServerExpiry(int clientHours)
    {
        var repository = new FakeRepository
        {
            CreateResult = new PosCreateHoldRepositoryResult(null, new PosHoldListItemDto(
                Guid.NewGuid(), "PS-2026-00001", Guid.NewGuid(), "SO-000001",
                Guid.NewGuid(), Guid.NewGuid(), null, null, null, "held",
                1, 100, 0, 0, 100, "LKR", Now, Now.AddHours(24), []))
        };
        var service = new PosHoldService(repository, new FakeClock());
        var request = new PosCreateHoldRequestDto(
            Guid.NewGuid(), "NewSale", null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)],
            null, null, "expiry-key", Now.AddHours(clientHours));

        var result = await service.CreateHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, repository.HeldAt);
        Assert.Equal(Now.AddHours(24), repository.ExpiresAt);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenSourceSalePartiallyPaid_ReturnsClearError()
    {
        var repository = new FakeRepository
        {
            CreateResult = new PosCreateHoldRepositoryResult(
                "pos_holds.sale_partially_paid_cannot_be_parked", null)
        };
        var service = new PosHoldService(repository, new FakeClock());
        var request = new PosCreateHoldRequestDto(
            Guid.NewGuid(), "NewSale", null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)],
            null, null, "key-partial-payment", null, Guid.NewGuid());

        var result = await service.CreateHoldAsync(
            new TenantRequestContext(
                Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.sale_partially_paid_cannot_be_parked", result.Error.Code);
        Assert.Equal(
            "This sale already has a payment recorded and cannot be parked.",
            result.Error.Message);
    }

    [Fact]
    public void ParkSaleReference_UsesServerYearAndFiveDigitSequence()
    {
        Assert.Equal("PS-2026-00001", ParkSaleReference.Format(Now, 1));
        Assert.Equal("PS-2027-00001", ParkSaleReference.Format(Now.AddYears(1), 1));
        Assert.True(ParkSaleReference.TryReadSequence("PS-2026-00012", Now, out var sequence));
        Assert.Equal(12, sequence);
        Assert.False(ParkSaleReference.TryReadSequence("HOLD-000001", Now, out _));
    }

    [Fact]
    public void ParkSaleReference_LockResourceIsTenantAndYearScoped()
    {
        var tenantOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenantTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var tenantOne2026 = ParkSaleReference.LockResource(tenantOne, Now);

        Assert.Equal(tenantOne2026, ParkSaleReference.LockResource(tenantOne, Now.AddMonths(2)));
        Assert.NotEqual(tenantOne2026, ParkSaleReference.LockResource(tenantTwo, Now));
        Assert.NotEqual(tenantOne2026, ParkSaleReference.LockResource(tenantOne, Now.AddYears(1)));
    }

    [Fact]
    public void ParkSaleReference_RejectsSequenceBeyondFiveDigits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ParkSaleReference.Format(Now, ParkSaleReference.MaximumSequence + 1));
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
        var deviceId = Guid.NewGuid();
        var repository = new FakeRepository();
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.View]);

        var result = await service.GetHoldsAsync(
            context, new PosHoldListQueryDto(deviceId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalCount);
        Assert.Equal(context.TenantId, repository.TenantId);
        Assert.Equal(context.UserId, repository.UserId);
        Assert.Equal(deviceId, repository.DeviceId);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task GetHoldsAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var repository = new FakeRepository();
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []);

        var result = await service.GetHoldsAsync(
            context, new PosHoldListQueryDto(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.permission_denied", result.Error.Code);
        Assert.Null(repository.TenantId);
    }

    [Fact]
    public async Task GetHoldsAsync_WithEmptyDeviceId_ReturnsInvalidDeviceId()
    {
        var repository = new FakeRepository();
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.View]);

        var result = await service.GetHoldsAsync(
            context, new PosHoldListQueryDto(Guid.Empty), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.invalid_device_id", result.Error.Code);
        Assert.Null(repository.TenantId);
    }

    [Fact]
    public async Task GetHoldsAsync_WhenTillSessionNotOpen_ReturnsMappedError()
    {
        var repository = new FakeRepository
        {
            GetActiveHoldsResult = new PosGetActiveHoldsRepositoryResult(
                "pos_checkout.till_session_not_open", null)
        };
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.View]);

        var result = await service.GetHoldsAsync(
            context, new PosHoldListQueryDto(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_checkout.till_session_not_open", result.Error.Code);
    }

    [Fact]
    public async Task GetHoldsAsync_OnlyReturnsHoldsOnCallersResolvedTill()
    {
        var deviceId = Guid.NewGuid();
        var matchingTillHold = new PosHoldListItemDto(
            Guid.NewGuid(), "PS-2026-00001", Guid.NewGuid(), "SO-000001",
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, "held",
            1, 100, 0, 0, 100, "LKR", Now, Now.AddHours(24), []);
        var repository = new FakeRepository
        {
            // The repository is responsible for filtering by the till resolved from the
            // device's open session; the service must pass through exactly what the
            // repository returns without re-deriving a till from OpenedByTenantUserId.
            GetActiveHoldsResult = new PosGetActiveHoldsRepositoryResult(
                null, [matchingTillHold], 1, 100, "LKR")
        };
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.View]);

        var result = await service.GetHoldsAsync(
            context, new PosHoldListQueryDto(deviceId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalCount);
        Assert.Equal(matchingTillHold.HoldId, result.Value!.Holds[0].HoldId);
        Assert.Equal(deviceId, repository.DeviceId);
    }

    [Theory]
    [InlineData("unknown", 1, 25, "pos_holds.invalid_scope")]
    [InlineData("today", 0, 25, "pos_holds.invalid_page")]
    [InlineData("today", 1, 0, "pos_holds.invalid_page_size")]
    [InlineData("today", 1, 101, "pos_holds.invalid_page_size")]
    public async Task GetHoldsAsync_WithInvalidQuery_ReturnsValidationError(
        string scope, int page, int pageSize, string expectedCode)
    {
        var repository = new FakeRepository();
        var service = new PosHoldService(repository, new FakeClock());
        var context = new TenantRequestContext(
            Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.View]);

        var result = await service.GetHoldsAsync(
            context,
            new PosHoldListQueryDto(Guid.NewGuid(), scope, page, pageSize),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error.Code);
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
        public PosGetActiveHoldsRepositoryResult GetActiveHoldsResult { get; init; } =
            new(null, Array.Empty<PosHoldListItemDto>(), 0, 0, "LKR");
        public Guid? TenantId { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? DeviceId { get; private set; }
        public DateTimeOffset? Now { get; private set; }
        public DateTimeOffset? HeldAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }
        public string? LastCancelReason { get; private set; }

        public Task<PosCancelHoldRepositoryResult> CancelHoldAsync(
            Guid tenantId,
            Guid tenantUserId,
            Guid holdId,
            string? reason,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastCancelReason = reason;
            return Task.FromResult(CancelResult);
        }

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
            DateTimeOffset heldAt,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            HeldAt = heldAt;
            ExpiresAt = expiresAt;
            return Task.FromResult(CreateResult);
        }

        public Task<PosGetActiveHoldsRepositoryResult> GetActiveHoldsAsync(
            Guid tenantId,
            Guid tenantUserId,
            PosHoldListQueryDto query,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            UserId = tenantUserId;
            DeviceId = query.DeviceId;
            Now = now;
            return Task.FromResult(GetActiveHoldsResult);
        }
    }
}
