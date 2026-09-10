using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class CashierPosChunk6AuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Expand_DoesNotAutoAddUnassignedCashChildren_WhenParentOnlyGranted()
    {
        var expanded = TenantPermissionAliases.Expand([PaymentPermissions.AcceptCash]);

        Assert.Contains(PaymentPermissions.AcceptCash, expanded);
        Assert.DoesNotContain("pos.cash_payment.tender.exact", expanded);
        Assert.DoesNotContain("pos.cash_payment.numpad.container", expanded);
    }

    [Fact]
    public void Resolver_ParentOnly_DoesNotMakeChildEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve([PaymentPermissions.AcceptCash]);
        Assert.DoesNotContain("pos.cash_payment.tender.exact", effective);
    }

    [Fact]
    public void Resolver_RoleParentPlusUserChild_IsEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            "pos.cash_payment.tender.exact",
        ]);
        Assert.Contains("pos.cash_payment.tender.exact", effective);
    }

    [Fact]
    public void HasPermission_PaymentMethods_AreIndependent()
    {
        var cashOnly = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [PaymentPermissions.AcceptCash]);
        Assert.True(cashOnly.HasPermission(PaymentPermissions.AcceptCash));
        Assert.False(cashOnly.HasPermission(PaymentPermissions.AcceptCard));
        Assert.False(cashOnly.HasPermission(PaymentPermissions.AcceptQr));
        Assert.False(cashOnly.HasPermission(PaymentPermissions.AcceptSplit));
    }

    [Fact]
    public async Task CancelHold_CreatePermissionAlone_DoesNotAuthorizeOrMutate()
    {
        var repository = new FakeHoldRepository();
        var service = new PosHoldService(repository, new FixedClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.Park.Create]),
            Guid.NewGuid(),
            "Customer left",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_holds.permission_denied", result.Error!.Code);
        Assert.Equal(0, repository.CancelCalls);
    }

    [Fact]
    public async Task CancelHold_RequiresHeldSalesCancel()
    {
        var repository = new FakeHoldRepository
        {
            CancelResult = new PosCancelHoldRepositoryResult(null)
        };
        var service = new PosHoldService(repository, new FixedClock());

        var result = await service.CancelHoldAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [SalesPermissions.HeldSales.Cancel]),
            Guid.NewGuid(),
            "Customer left",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.CancelCalls);
    }

    [Fact]
    public async Task OpenTill_DoesNotAuthorizeClose()
    {
        var service = new PosTillSessionService(new NoOpTillRepo(), new FixedClock());
        var result = await service.CloseTillAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [PosPermissions.Till.SessionOpen]),
            new CloseTillRequest(Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.permission_denied", result.Error!.Code);
    }

    [Fact]
    public async Task CloseTill_DoesNotAuthorizeOpen()
    {
        var service = new PosTillSessionService(new NoOpTillRepo(), new FixedClock());
        var result = await service.OpenTillAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [PosPermissions.Till.SessionClose]),
            new OpenTillRequest(Guid.NewGuid(), Guid.NewGuid(), 0, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.permission_denied", result.Error!.Code);
    }

    [Theory]
    [InlineData("CASH_IN", "IN", CashDrawerPermissions.Canonical.CashOut)]
    [InlineData("CASH_IN", "IN", CashDrawerPermissions.Canonical.CashDrop)]
    [InlineData("CASH_OUT", "OUT", CashDrawerPermissions.Canonical.CashIn)]
    [InlineData("CASH_DROP", "OUT", CashDrawerPermissions.Canonical.CashIn)]
    public async Task CashMovement_WrongActionPermission_IsDeniedWithoutMutation(
        string typeCode,
        string direction,
        string grantedPermission)
    {
        var typeId = Guid.NewGuid();
        var repository = new FakeDrawerRepo
        {
            MovementType = new PosCashMovementTypeDto(typeId, typeCode, typeCode, direction, false, true)
        };
        var service = new PosDrawerService(
            repository,
            null!,
            new UnusedPasswordHasher(),
            new FixedClock(),
            new OpenTillSessionRepo());

        var result = await service.CreateFinancialMovementAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [grantedPermission]),
            new CreatePosCashMovementRequest(Guid.NewGuid(), Guid.NewGuid(), typeId, 10m, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("cash_drawer.permission_denied", result.Error!.Code);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CashIn_WithMatchingPermission_MutatesOnce()
    {
        var typeId = Guid.NewGuid();
        var repository = new FakeDrawerRepo
        {
            MovementType = new PosCashMovementTypeDto(typeId, "CASH_IN", "Cash In", "IN", false, true),
            CreateResult = (null, new PosCashDrawerMovementDto(
                Guid.NewGuid(), "CASH_IN", "IN", 10m, "LKR", null, null, "Cashier", Now, "M1", typeId, "Cash In"))
        };
        var service = new PosDrawerService(
            repository,
            null!,
            new UnusedPasswordHasher(),
            new FixedClock(),
            new OpenTillSessionRepo());

        var result = await service.CreateFinancialMovementAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [CashDrawerPermissions.Canonical.CashIn]),
            new CreatePosCashMovementRequest(Guid.NewGuid(), Guid.NewGuid(), typeId, 10m, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.CreateCalls);
    }

    [Fact]
    public void PreAuth_DoesNotGrantBusinessPermissions()
    {
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["pre_auth.login.email.input"]);

        Assert.False(context.HasPermission(PaymentPermissions.AcceptCash));
        Assert.False(context.HasPermission(SalesPermissions.HeldSales.Cancel));
        Assert.False(context.HasPermission(PosPermissions.Till.SessionOpen));
    }

    [Fact]
    public void CatalogCodeFreeze_RoleAssignableCountUnchanged()
    {
        Assert.Equal(346, CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes.Count);
        Assert.Contains(SalesPermissions.HeldSales.Cancel, CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes);
        Assert.Contains(CashDrawerPermissions.Canonical.CashIn, CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes);
        Assert.Contains(PosPermissions.Till.SessionOpen, CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class UnusedPasswordHasher : IPasswordHashService
    {
        public string HashPassword(string password) => throw new NotSupportedException();
        public bool VerifyPassword(string password, string passwordHash) => throw new NotSupportedException();
    }

    private sealed class FakeHoldRepository : IPosHoldRepository
    {
        public int CancelCalls { get; private set; }
        public PosCancelHoldRepositoryResult CancelResult { get; init; } = new("pos_holds.not_found");

        public Task<PosCancelHoldRepositoryResult> CancelHoldAsync(
            Guid tenantId, Guid tenantUserId, Guid holdId, string? reason, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CancelCalls++;
            return Task.FromResult(CancelResult);
        }

        public Task<PosCreateHoldRepositoryResult> CreateHoldAsync(
            Guid tenantId, Guid tenantUserId, IReadOnlyCollection<string> permissions,
            PosCreateHoldRequestDto request, DateTimeOffset heldAt, DateTimeOffset expiresAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PosGetActiveHoldsRepositoryResult> GetActiveHoldsAsync(
            Guid tenantId, Guid tenantUserId, PosHoldListQueryDto query, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PosRecallHoldRepositoryResult> RecallHoldAsync(
            Guid tenantId, Guid tenantUserId, IReadOnlyCollection<string> permissions, Guid holdId,
            PosRecallHoldRequestDto request, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class NoOpTillRepo : IPosTillSessionRepository
    {
        public Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
            Guid tenantId, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(new CurrentTillSessionResolveResult(false, "till_session.not_found", null));

        public Task<OpenTillRepositoryResult> OpenTillAsync(
            Guid tenantId, Guid tenantUserId, OpenTillCommand command, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CloseTillRepositoryResult> CloseTillAsync(
            Guid tenantId, Guid tenantUserId, CloseTillCommand command, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class OpenTillSessionRepo : IPosTillSessionRepository
    {
        public Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
            Guid tenantId, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(new CurrentTillSessionResolveResult(
                true,
                null,
                new CurrentTillSessionDbSnapshot(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), deviceId, 0m, "OPEN", Now, null, "LKR", 0m, "Till", "Cashier")));

        public Task<OpenTillRepositoryResult> OpenTillAsync(
            Guid tenantId, Guid tenantUserId, OpenTillCommand command, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CloseTillRepositoryResult> CloseTillAsync(
            Guid tenantId, Guid tenantUserId, CloseTillCommand command, DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDrawerRepo : IPosDrawerRepository
    {
        public int CreateCalls { get; private set; }
        public required PosCashMovementTypeDto MovementType { get; init; }
        public (string? ErrorCode, PosCashDrawerMovementDto? Movement) CreateResult { get; init; }

        public Task<PosCashMovementTypeDto?> GetMovementTypeByIdAsync(
            Guid tenantId, Guid movementTypeId, CancellationToken cancellationToken) =>
            Task.FromResult<PosCashMovementTypeDto?>(MovementType);

        public Task<(string? ErrorCode, PosCashDrawerMovementDto? Movement)> CreateFinancialMovementAsync(
            Guid tenantId, Guid userId, Guid trustedTillId, CreatePosCashMovementRequest request,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(CreateResult);
        }

        public Task<CashDrawerOperationDto?> GetOperationByIdAsync(Guid tenantId, Guid operationId, CancellationToken cancellationToken) => Task.FromResult<CashDrawerOperationDto?>(null);
        public Task<CashDrawerOperationDto?> GetOperationByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken) => Task.FromResult<CashDrawerOperationDto?>(null);
        public Task<CashDrawerSettingsDto?> GetActiveDrawerSettingsAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult<CashDrawerSettingsDto?>(null);
        public Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> RegisterOperationAsync(Guid tenantId, Guid userId, RegisterDrawerOperationRequest request, Guid? approverId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult<(string?, CashDrawerOperationDto?)>((null, null));
        public Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> FinalizeOperationAsync(Guid tenantId, Guid userId, Guid operationId, FinalizeDrawerOperationRequest request, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult<(string?, CashDrawerOperationDto?)>((null, null));
        public Task<IReadOnlyList<CashDrawerOperationDto>> GetHistoryAsync(Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CashDrawerOperationDto>>([]);
        public Task<PosCashDrawerSummaryDto?> GetFinancialSummaryAsync(Guid tenantId, Guid tillSessionId, CancellationToken cancellationToken) => Task.FromResult<PosCashDrawerSummaryDto?>(null);
        public Task<PosCashDrawerMovementPageDto> GetFinancialMovementsAsync(Guid tenantId, Guid tillSessionId, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult(new PosCashDrawerMovementPageDto([], page, pageSize, 0, 0));
        public Task<IReadOnlyList<PosCashMovementTypeDto>> GetMovementTypesAsync(Guid tenantId, string direction, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PosCashMovementTypeDto>>([]);
    }
}
