using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class PosCashDrawerServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid TillId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Summary_WithoutCashDrawerView_IsDeniedAndDoesNotLoadFinancialData()
    {
        var repository = new FakeDrawerRepository();
        var tillSessions = new FakeTillSessionRepository { Result = OpenSession() };
        var service = CreateService(repository, tillSessions);

        var result = await service.GetFinancialSummaryAsync(
            Context("Manager", "Cashier", "Supervisor"), DeviceId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("cash_drawer.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.SummaryCalls);
        Assert.Equal(0, tillSessions.ResolveCalls);
    }

    [Fact]
    public async Task Movements_WithoutCashDrawerView_IsDenied()
    {
        var repository = new FakeDrawerRepository();
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });

        var result = await service.GetFinancialMovementsAsync(
            Context("Manager"), DeviceId, 1, 25, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("cash_drawer.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.MovementsCalls);
    }

    [Fact]
    public async Task CreateMovement_WithoutCashDrawerMovementCreate_IsDeniedAndDoesNotPersist()
    {
        var repository = new FakeDrawerRepository();
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });

        var result = await service.CreateFinancialMovementAsync(
            Context(CashDrawerPermissions.View, "Manager", "Cashier"),
            ValidCreateRequest(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("cash_drawer.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Theory]
    [InlineData("till_session.device_not_trusted")]
    [InlineData("till_session.device_not_found")]
    public async Task CashDrawerEndpoints_RejectUntrustedOrCrossTenantDevice(string errorCode)
    {
        var repository = new FakeDrawerRepository();
        var tillSessions = new FakeTillSessionRepository
        {
            Result = new CurrentTillSessionResolveResult(false, errorCode, null)
        };
        var service = CreateService(repository, tillSessions);
        var context = Context(CashDrawerPermissions.View, CashDrawerPermissions.CreateMovement);

        var summary = await service.GetFinancialSummaryAsync(context, DeviceId, CancellationToken.None);
        var movements = await service.GetFinancialMovementsAsync(context, DeviceId, 1, 25, CancellationToken.None);
        var create = await service.CreateFinancialMovementAsync(context, ValidCreateRequest(), CancellationToken.None);

        Assert.Equal(errorCode, summary.Error.Code);
        Assert.Equal(errorCode, movements.Error.Code);
        Assert.Equal(errorCode, create.Error.Code);
        Assert.Equal(0, repository.SummaryCalls);
        Assert.Equal(0, repository.MovementsCalls);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CashDrawerEndpoints_RejectInactiveDeviceAsNotTrusted()
    {
        var repository = new FakeDrawerRepository();
        var tillSessions = new FakeTillSessionRepository
        {
            Result = new CurrentTillSessionResolveResult(false, "till_session.device_not_trusted", null)
        };
        var service = CreateService(repository, tillSessions);
        var context = Context(CashDrawerPermissions.View, CashDrawerPermissions.CreateMovement);

        var create = await service.CreateFinancialMovementAsync(context, ValidCreateRequest(), CancellationToken.None);

        Assert.Equal("till_session.device_not_trusted", create.Error.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Theory]
    [InlineData("till_session.not_found")]
    [InlineData("cash_drawer.till_session_not_open")]
    public async Task CashDrawerEndpoints_RejectClosedOrMissingOpenTill(string resolveError)
    {
        var repository = new FakeDrawerRepository
        {
            Summary = null,
            CreateResult = ("cash_drawer.till_session_not_open", null)
        };
        var tillSessions = new FakeTillSessionRepository
        {
            Result = resolveError == "cash_drawer.till_session_not_open"
                ? OpenSession()
                : new CurrentTillSessionResolveResult(false, resolveError, null)
        };
        var service = CreateService(repository, tillSessions);
        var context = Context(CashDrawerPermissions.View, CashDrawerPermissions.CreateMovement);

        if (resolveError == "till_session.not_found")
        {
            var summary = await service.GetFinancialSummaryAsync(context, DeviceId, CancellationToken.None);
            var movements = await service.GetFinancialMovementsAsync(context, DeviceId, 1, 25, CancellationToken.None);
            var create = await service.CreateFinancialMovementAsync(context, ValidCreateRequest(), CancellationToken.None);
            Assert.Equal("till_session.not_found", summary.Error.Code);
            Assert.Equal("till_session.not_found", movements.Error.Code);
            Assert.Equal("till_session.not_found", create.Error.Code);
            Assert.Equal(0, repository.CreateCalls);
            return;
        }

        var closedCreate = await service.CreateFinancialMovementAsync(context, ValidCreateRequest(), CancellationToken.None);
        Assert.Equal("cash_drawer.till_session_not_open", closedCreate.Error.Code);
        Assert.Equal(1, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateMovement_WrongSessionOrDeviceSessionMismatch_IsRejected()
    {
        var repository = new FakeDrawerRepository();
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });
        var mismatched = ValidCreateRequest() with { TillSessionId = Guid.NewGuid() };

        var result = await service.CreateFinancialMovementAsync(
            Context(CashDrawerPermissions.CreateMovement), mismatched, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("cash_drawer.till_session_mismatch", result.Error.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateMovement_ZeroOrNegativeAmount_IsRejected(decimal amount)
    {
        var repository = new FakeDrawerRepository();
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });

        var result = await service.CreateFinancialMovementAsync(
            Context(CashDrawerPermissions.CreateMovement),
            ValidCreateRequest() with { Amount = amount },
            CancellationToken.None);

        Assert.Equal("cash_drawer.invalid_amount", result.Error.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Theory]
    [InlineData("OPENING_FLOAT")]
    [InlineData("CASH_SALE")]
    [InlineData("NOT_A_TYPE")]
    public async Task CreateMovement_UnsupportedType_IsRejected(string type)
    {
        var repository = new FakeDrawerRepository();
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });

        var result = await service.CreateFinancialMovementAsync(
            Context(CashDrawerPermissions.CreateMovement),
            ValidCreateRequest() with { MovementType = type },
            CancellationToken.None);

        Assert.Equal("cash_drawer.invalid_movement_type", result.Error.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateMovement_MissingReason_IsRejected(string? reason)
    {
        var repository = new FakeDrawerRepository();
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });

        var result = await service.CreateFinancialMovementAsync(
            Context(CashDrawerPermissions.CreateMovement),
            ValidCreateRequest() with { Reason = reason! },
            CancellationToken.None);

        Assert.Equal("cash_drawer.invalid_reason", result.Error.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateMovement_WithPermissionAndMatchingSession_DelegatesToRepository()
    {
        var movement = new PosCashDrawerMovementDto(
            Guid.NewGuid(), "CASH_IN", "IN", 1000m, "LKR", "Float", null, "Cashier", Now);
        var repository = new FakeDrawerRepository { CreateResult = (null, movement) };
        var service = CreateService(repository, new FakeTillSessionRepository { Result = OpenSession() });

        var result = await service.CreateFinancialMovementAsync(
            Context(CashDrawerPermissions.CreateMovement), ValidCreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(movement.MovementId, result.Value!.MovementId);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal(TillId, repository.LastTrustedTillId);
    }

    private static PosDrawerService CreateService(
        FakeDrawerRepository repository,
        FakeTillSessionRepository tillSessions) =>
        new(repository, null!, new UnusedPasswordHasher(), new FixedClock(), tillSessions);

    private static TenantRequestContext Context(params string[] permissions) =>
        new(TenantId, UserId, permissions);

    private static CreatePosCashMovementRequest ValidCreateRequest() =>
        new(Guid.NewGuid(), DeviceId, SessionId, "CASH_IN", 1000m, "Approved reason");

    private static CurrentTillSessionResolveResult OpenSession() =>
        new(true, null, new CurrentTillSessionDbSnapshot(
            SessionId, Guid.NewGuid(), TillId, DeviceId, 10000m, "OPEN", Now, null, "LKR", 10000m, "Till", "Cashier"));

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class UnusedPasswordHasher : IPasswordHashService
    {
        public string HashPassword(string password) => throw new NotSupportedException();
        public bool VerifyPassword(string password, string passwordHash) => throw new NotSupportedException();
    }

    private sealed class FakeTillSessionRepository : IPosTillSessionRepository
    {
        public int ResolveCalls { get; private set; }
        public CurrentTillSessionResolveResult Result { get; init; } =
            new(false, "till_session.not_found", null);

        public Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
            Guid tenantId, Guid deviceId, CancellationToken cancellationToken)
        {
            ResolveCalls++;
            return Task.FromResult(Result);
        }

        public Task<OpenTillRepositoryResult> OpenTillAsync(
            Guid tenantId, Guid tenantUserId, OpenTillCommand command, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CloseTillRepositoryResult> CloseTillAsync(
            Guid tenantId, Guid tenantUserId, CloseTillCommand command, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDrawerRepository : IPosDrawerRepository
    {
        public int SummaryCalls { get; private set; }
        public int MovementsCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public Guid LastTrustedTillId { get; private set; }
        public PosCashDrawerSummaryDto? Summary { get; init; }
        public (string? ErrorCode, PosCashDrawerMovementDto? Movement) CreateResult { get; init; } =
            (null, null);

        public Task<CashDrawerOperationDto?> GetOperationByIdAsync(
            Guid tenantId, Guid operationId, CancellationToken cancellationToken) =>
            Task.FromResult<CashDrawerOperationDto?>(null);

        public Task<CashDrawerOperationDto?> GetOperationByRequestIdAsync(
            Guid tenantId, Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult<CashDrawerOperationDto?>(null);

        public Task<CashDrawerSettingsDto?> GetActiveDrawerSettingsAsync(
            Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult<CashDrawerSettingsDto?>(null);

        public Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> RegisterOperationAsync(
            Guid tenantId, Guid userId, RegisterDrawerOperationRequest request, Guid? approverId,
            DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<(string?, CashDrawerOperationDto?)>((null, null));

        public Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> FinalizeOperationAsync(
            Guid tenantId, Guid userId, Guid operationId, FinalizeDrawerOperationRequest request,
            DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<(string?, CashDrawerOperationDto?)>((null, null));

        public Task<IReadOnlyList<CashDrawerOperationDto>> GetHistoryAsync(
            Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CashDrawerOperationDto>>([]);

        public Task<PosCashDrawerSummaryDto?> GetFinancialSummaryAsync(
            Guid tenantId, Guid tillSessionId, CancellationToken cancellationToken)
        {
            SummaryCalls++;
            return Task.FromResult(Summary);
        }

        public Task<PosCashDrawerMovementPageDto> GetFinancialMovementsAsync(
            Guid tenantId, Guid tillSessionId, int page, int pageSize, CancellationToken cancellationToken)
        {
            MovementsCalls++;
            return Task.FromResult(new PosCashDrawerMovementPageDto([], page, pageSize, 0, 0));
        }

        public Task<(string? ErrorCode, PosCashDrawerMovementDto? Movement)> CreateFinancialMovementAsync(
            Guid tenantId, Guid userId, Guid trustedTillId, CreatePosCashMovementRequest request,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            CreateCalls++;
            LastTrustedTillId = trustedTillId;
            return Task.FromResult(CreateResult);
        }
    }
}
