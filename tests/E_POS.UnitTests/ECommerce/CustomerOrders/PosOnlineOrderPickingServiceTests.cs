using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderPickingServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_RequiresAccessAndPickingView()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetAsync(
            new TenantRequestContext(TenantId, UserId, [PosOnlineOrderPickingService.AccessPermission]),
            OutletId, OrderId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.QueryCalls);
    }

    [Fact]
    public async Task GetAsync_RequiresOrdersViewPermission()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.GetAsync(
            new TenantRequestContext(TenantId, UserId,
                [PosOnlineOrderPickingService.AccessPermission,
                 PosOnlineOrderPickingService.ViewPermission]),
            OutletId, OrderId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.QueryCalls);
    }

    [Fact]
    public async Task PickLine_ScanRequiresPickAndScanPermissions()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var context = Context(PosOnlineOrderPickingService.AccessPermission,
            PosOnlineOrderPickingService.PickPermission);

        var result = await service.PickLineAsync(context, OutletId, OrderId, LineId,
            PickRequest("SCAN"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.PickCalls);
    }

    [Fact]
    public async Task PickLine_ManualRequiresManualEntryPermission()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var context = Context(PosOnlineOrderPickingService.AccessPermission,
            PosOnlineOrderPickingService.PickPermission,
            PosOnlineOrderPickingService.ScanPermission);

        var result = await service.PickLineAsync(context, OutletId, OrderId, LineId,
            PickRequest("MANUAL"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.PickCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PickLine_RequiresPositiveExpectedVersion(long version)
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var request = new PosOnlineOrderPickLineRequest
        {
            Quantity = 1, InputMethod = "MANUAL", ExpectedVersion = version
        };

        var result = await service.PickLineAsync(
            Context(PosOnlineOrderPickingService.AccessPermission,
                PosOnlineOrderPickingService.PickPermission,
                PosOnlineOrderPickingService.ManualEntryPermission),
            OutletId, OrderId, LineId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_expected_version", result.Error.Code);
        Assert.Equal(0, repository.PickCalls);
    }

    [Fact]
    public async Task PickLine_AuthorizedScan_ForwardsNormalizedContract()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.PickLineAsync(
            Context(PosOnlineOrderPickingService.AccessPermission,
                PosOnlineOrderPickingService.PickPermission,
                PosOnlineOrderPickingService.ScanPermission),
            OutletId, OrderId, LineId, PickRequest("scan"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.PickCalls);
        Assert.Equal("SCAN", repository.PickRequest?.InputMethod);
        Assert.Equal(7, repository.PickRequest?.ExpectedVersion);
        Assert.Equal(Now, repository.Now);
    }

    [Theory]
    [InlineData("SCAN")]
    [InlineData("MANUAL")]
    public async Task PickLine_ScanAndManualRequireBarcode(string inputMethod)
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var inputPermission = inputMethod == "SCAN"
            ? PosOnlineOrderPickingService.ScanPermission
            : PosOnlineOrderPickingService.ManualEntryPermission;

        var result = await service.PickLineAsync(
            Context(PosOnlineOrderPickingService.AccessPermission,
                PosOnlineOrderPickingService.PickPermission, inputPermission),
            OutletId, OrderId, LineId,
            new PosOnlineOrderPickLineRequest
            {
                Quantity = 1,
                Barcode = "   ",
                InputMethod = inputMethod,
                ExpectedVersion = 7
            }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_barcode", result.Error.Code);
        Assert.Equal(0, repository.PickCalls);
    }

    [Fact]
    public async Task PickLine_AuthorizedManual_ForwardsTrimmedBarcode()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.PickLineAsync(
            Context(PosOnlineOrderPickingService.AccessPermission,
                PosOnlineOrderPickingService.PickPermission,
                PosOnlineOrderPickingService.ManualEntryPermission),
            OutletId, OrderId, LineId, PickRequest("manual"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MANUAL", repository.PickRequest?.InputMethod);
        Assert.Equal("012345", repository.PickRequest?.Barcode);
    }

    [Fact]
    public async Task ReportIssue_DoesNotRequirePickPermission()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.ReportIssueAsync(
            Context(PosOnlineOrderPickingService.AccessPermission,
                PosOnlineOrderPickingService.ReportIssuePermission),
            OutletId, OrderId, LineId,
            new PosOnlineOrderPickingIssueRequest
            {
                Reason = "item_not_found", Note = " Shelf checked ", ExpectedVersion = 7
            }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.IssueCalls);
        Assert.Equal("ITEM_NOT_FOUND", repository.IssueRequest?.Reason);
        Assert.Equal("Shelf checked", repository.IssueRequest?.Note);
    }

    [Fact]
    public async Task MissingEntitlement_StopsBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, allowed: false);

        var result = await service.GetAsync(
            Context(PosOnlineOrderPickingService.AccessPermission,
                PosOnlineOrderPickingService.ViewPermission),
            OutletId, OrderId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.feature_not_entitled", result.Error.Code);
        Assert.Equal(0, repository.QueryCalls);
    }

    [Fact]
    public async Task AddNote_RequiresDedicatedPermissionAndValidatesBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var denied = await service.AddNoteAsync(
            Context(PosOnlineOrderPickingService.AccessPermission), OutletId, OrderId,
            new PosOnlineOrderPickingNoteRequest { Note = "Checked shelf", ExpectedVersion = 7 },
            CancellationToken.None);
        var empty = await service.AddNoteAsync(
            Context(PosOnlineOrderPickingService.AccessPermission, PosOnlineOrderPickingService.NotePermission),
            OutletId, OrderId,
            new PosOnlineOrderPickingNoteRequest { Note = "   ", ExpectedVersion = 7 },
            CancellationToken.None);
        var tooLong = await service.AddNoteAsync(
            Context(PosOnlineOrderPickingService.AccessPermission, PosOnlineOrderPickingService.NotePermission),
            OutletId, OrderId,
            new PosOnlineOrderPickingNoteRequest
            {
                Note = new string('x', PosOnlineOrderPickingService.PickingNoteMaxLength + 1),
                ExpectedVersion = 7
            }, CancellationToken.None);

        Assert.Equal("online_orders.permission_denied", denied.Error.Code);
        Assert.Equal("online_orders.invalid_note", empty.Error.Code);
        Assert.Equal("online_orders.invalid_note", tooLong.Error.Code);
        Assert.Equal(0, repository.NoteCalls);
    }

    [Fact]
    public async Task AddNote_Authorized_TrimsAndForwardsServerTime()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.AddNoteAsync(
            Context(PosOnlineOrderPickingService.AccessPermission, PosOnlineOrderPickingService.NotePermission),
            OutletId, OrderId,
            new PosOnlineOrderPickingNoteRequest { Note = "  Checked shelf  ", ExpectedVersion = 7 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.NoteCalls);
        Assert.Equal("Checked shelf", repository.NoteRequest?.Note);
        Assert.Equal(7, repository.NoteRequest?.ExpectedVersion);
        Assert.Equal(Now, repository.Now);
    }

    private static PosOnlineOrderPickLineRequest PickRequest(string inputMethod) => new()
    {
        Quantity = 1,
        Barcode = " 012345 ",
        InputMethod = inputMethod,
        ExpectedVersion = 7
    };

    private static TenantRequestContext Context(params string[] permissions) =>
        new(TenantId, UserId,
            permissions.Append(PosOnlineOrderPickingService.OrdersViewPermission).Distinct().ToArray());

    [Theory]
    [InlineData("READY", "commerce.online_order.collection.view_ready", true)]
    [InlineData("READY", "commerce.online_order.picking.view", false)]
    [InlineData("PICKING", "commerce.online_order.collection.view_ready", false)]
    [InlineData("PACKED", "commerce.online_order.collection.view_ready", false)]
    [InlineData("PICKING", "commerce.online_order.picking.view", true)]
    [InlineData("PACKED", "commerce.online_order.picking.view", true)]
    public async Task GetAsync_EnforcesStateSpecificPermission(string state, string permission, bool allowed)
    {
        var repository = new FakeRepository { State = state };
        var result = await CreateService(repository).GetAsync(
            Context(PosOnlineOrderPickingService.AccessPermission, permission),
            OutletId, OrderId, CancellationToken.None);
        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed) Assert.Equal("online_orders.permission_denied", result.Error.Code);
    }

    private static PosOnlineOrderPickingService CreateService(FakeRepository repository, bool allowed = true) =>
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

    private sealed class FakeRepository : IPosOnlineOrderPickingRepository
    {
        public string State { get; init; } = "PICKING";
        public int QueryCalls { get; private set; }
        public int PickCalls { get; private set; }
        public int IssueCalls { get; private set; }
        public int NoteCalls { get; private set; }
        public DateTimeOffset Now { get; private set; }
        public PosOnlineOrderPickLineRequest? PickRequest { get; private set; }
        public PosOnlineOrderPickingIssueRequest? IssueRequest { get; private set; }
        public PosOnlineOrderPickingNoteRequest? NoteRequest { get; private set; }

        public Task<PosOnlineOrderPickingRepositoryResult> GetAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
            DateTimeOffset serverTime, CancellationToken cancellationToken)
        {
            QueryCalls++;
            Now = serverTime;
            return Task.FromResult(PosOnlineOrderPickingRepositoryResult.QuerySuccess(
                new PosOnlineOrderPickingResponse { Status = State }));
        }

        public Task<PosOnlineOrderPickingRepositoryResult> PickLineAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
            PosOnlineOrderPickLineRequest request, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            PickCalls++;
            PickRequest = request;
            Now = now;
            return Task.FromResult(PosOnlineOrderPickingRepositoryResult.CommandSuccess(
                new PosOnlineOrderPickingCommandResponse()));
        }

        public Task<PosOnlineOrderPickingRepositoryResult> ReportIssueAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
            PosOnlineOrderPickingIssueRequest request, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            IssueCalls++;
            IssueRequest = request;
            Now = now;
            return Task.FromResult(PosOnlineOrderPickingRepositoryResult.CommandSuccess(
                new PosOnlineOrderPickingCommandResponse()));
        }

        public Task<PosOnlineOrderPickingRepositoryResult> AddNoteAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
            PosOnlineOrderPickingNoteRequest request, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            NoteCalls++;
            NoteRequest = request;
            Now = now;
            return Task.FromResult(PosOnlineOrderPickingRepositoryResult.NoteSuccess(
                new PosOnlineOrderPickingNoteCommandResponse()));
        }
    }
}
