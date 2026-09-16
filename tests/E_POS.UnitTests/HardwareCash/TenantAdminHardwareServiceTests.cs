using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class TenantAdminHardwareServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("66666666-0000-4000-8000-000000000001");
    private static readonly Guid OutletId = Guid.Parse("77777777-0000-4000-8000-000000000001");
    private static readonly Guid TillId = Guid.Parse("88888888-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithoutManagePermission_ReturnsDenied()
    {
        var service = CreateService(new FakeHardwareRepository());
        var result = await service.CreateAsync(
            CreateContext([TenantAdminTillPermissions.HardwareView]),
            ValidCreateRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("hardware.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithManagePermission_Succeeds()
    {
        var repository = new FakeHardwareRepository { OutletExists = true };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.SavedDevice);
        Assert.Equal("PRN001", repository.SavedDevice!.HardwareDeviceCode);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsConflict()
    {
        var repository = new FakeHardwareRepository { OutletExists = true, CodeExists = true };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("hardware.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task AssignToTillAsync_WhenAlreadyAssigned_ReturnsConflict()
    {
        var device = HardwareDevice.Create(
            Guid.NewGuid(), TenantId, OutletId, null, "PRN001", "Printer", "RECEIPT_PRINTER",
            "NETWORK", null, null, null, null, null, null, "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository
        {
            EditableDevice = device,
            Till = Till.Create(
                TillId, TenantId, OutletId, "Till 1", "FRONT", 1, "T001", "STANDARD",
                0m, "GBP", true, TillConstants.ActiveStatus, UserId, Now),
            ActiveAssignment = HardwareDeviceAssignment.Create(
                Guid.NewGuid(), TenantId, OutletId, device.Id, TillId, null, true, UserId, Now),
        };
        var service = CreateService(repository);

        var result = await service.AssignToTillAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]),
            TillId,
            new TenantAdminHardwareAssignmentRequest(device.Id, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("hardware.assignment_conflict", result.Error.Code);
    }

    [Fact]
    public async Task RecordHardwareHeartbeatAsync_UntrustedDevice_ReturnsForbidden()
    {
        var pos = PosDevice.Create(
            Guid.NewGuid(), TenantId, OutletId, "POS001", "Tablet", "TABLET", "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository { PosDevice = pos };
        var service = CreateService(repository);

        var result = await service.RecordHardwareHeartbeatAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]),
            pos.Id,
            new PosHardwareHeartbeatRequest(Now, [new PosHardwareHeartbeatItemRequest(Guid.NewGuid())]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("hardware.pos_device_untrusted", result.Error.Code);
    }

    [Theory]
    [InlineData("USB")]
    [InlineData("NETWORK")]
    public async Task CreateAsync_CardReaderRejectsRawTransport(string connection)
    {
        var service = CreateService(new FakeHardwareRepository { OutletExists = true });
        var result = await service.CreateAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { HardwareDeviceType = "CARD_READER", ConnectionType = connection },
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"cashDrawer\":\"true\"}")]
    [InlineData("{\"autoCut\":1}")]
    [InlineData("{\"paperWidth\":57}")]
    [InlineData("{\"paperWidth\":\"80\"}")]
    [InlineData("{")]
    [InlineData("{\"port\":{}}")]
    public async Task CreateAsync_InvalidConfiguration_ReturnsValidation(string config)
    {
        var service = CreateService(new FakeHardwareRepository { OutletExists = true });
        var result = await service.CreateAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { ConfigJson = config }, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_DrawerRejectsMissingParent()
    {
        var service = CreateService(new FakeHardwareRepository { OutletExists = true });
        var result = await service.CreateAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { HardwareDeviceType = "CASH_DRAWER", ConnectionType = "USB",
                ConfigJson = "{\"parentPrinterId\":\"11111111-1111-4111-8111-111111111111\"}" },
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_DrawerRejectsParentInAnotherOutlet()
    {
        var parent = HardwareDevice.Create(Guid.NewGuid(), TenantId, Guid.NewGuid(), null, "P", "Printer",
            "RECEIPT_PRINTER", "USB", null, null, null, null, null, null, "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository { OutletExists = true, EditableDevice = parent };
        var result = await CreateService(repository).CreateAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { HardwareDeviceType = "CASH_DRAWER", ConnectionType = "USB",
                ConfigJson = System.Text.Json.JsonSerializer.Serialize(new { parentPrinterId = parent.Id }) },
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Null(repository.SavedDevice);
    }

    [Fact]
    public async Task AssignAsync_DrawerRequiresParentAtSameTarget()
    {
        var parent = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "P", "Printer",
            "RECEIPT_PRINTER", "USB", null, null, null, null, null, null, "ACTIVE", UserId, Now);
        var drawer = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "D", "Drawer",
            "CASH_DRAWER", "USB", null, null, null, null, null,
            System.Text.Json.JsonSerializer.Serialize(new { parentPrinterId = parent.Id }), "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository {
            EditableDevice = drawer, ParentDevice = parent,
            Till = Till.Create(TillId, TenantId, OutletId, "Till 1", "FRONT", 1, "T001", "STANDARD",
                0m, "GBP", true, TillConstants.ActiveStatus, UserId, Now),
            ParentAssignment = HardwareDeviceAssignment.Create(Guid.NewGuid(), TenantId, OutletId, parent.Id,
                Guid.NewGuid(), null, false, UserId, Now),
        };
        var result = await CreateService(repository).AssignToTillAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            TillId, new TenantAdminHardwareAssignmentRequest(drawer.Id), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Null(repository.SavedAssignment);
    }

    [Fact]
    public async Task ReleaseAsync_RejectsPrinterWithAssignedDrawer()
    {
        var parentId = Guid.NewGuid();
        var drawer = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "D", "Drawer",
            "CASH_DRAWER", "USB", null, null, null, null, null,
            System.Text.Json.JsonSerializer.Serialize(new { parentPrinterId = parentId }), "ACTIVE", UserId, Now);
        var assignment = HardwareDeviceAssignment.Create(Guid.NewGuid(), TenantId, OutletId, parentId,
            TillId, null, false, UserId, Now);
        var repository = new FakeHardwareRepository {
            ActiveAssignment = assignment,
            ListedRows = [new HardwareDeviceListRow(drawer, "Store",
                HardwareDeviceAssignment.Create(Guid.NewGuid(), TenantId, OutletId, drawer.Id, TillId, null, false, UserId, Now))],
        };
        var result = await CreateService(repository).ReleaseAssignmentAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            assignment.Id, new TenantAdminHardwareAssignmentReleaseRequest(), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Null(assignment.ReleasedAt);
    }

    [Theory]
    [InlineData("SCALE", "SERIAL")]
    [InlineData("CUSTOMER_DISPLAY", "USB")]
    [InlineData("CARD_READER", "PROVIDER")]
    [InlineData("RECEIPT_PRINTER", "BUILT_IN")]
    [InlineData("BARCODE_SCANNER", "NETWORK")]
    public async Task CreateAsync_WithoutRuntimeAdapter_DoesNotPersist(string type, string connection)
    {
        var repository = new FakeHardwareRepository { OutletExists = true };
        var result = await CreateService(repository).CreateAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { HardwareDeviceType = type, ConnectionType = connection }, CancellationToken.None);
        Assert.Equal("hardware.unsupported", result.Error.Code);
        Assert.Null(repository.SavedDevice);
    }

    [Theory]
    [InlineData("{\"accessKey\":\"test-only\"}")]
    [InlineData("{\"capabilitySource\":\"CERTIFIED\"}")]
    [InlineData("{\"protocol\":\"HID\"}")]
    [InlineData("{\"adapterKey\":\"unknown\"}")]
    [InlineData("{\"protocol\":\"ESC/POS\",\"protocol\":\"ESC/POS\"}")]
    public async Task CreateAsync_UntrustedMetadata_DoesNotPersist(string config)
    {
        var repository = new FakeHardwareRepository { OutletExists = true };
        var result = await CreateService(repository).CreateAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { ConfigJson = config }, CancellationToken.None);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
        Assert.Null(repository.SavedDevice);
    }

    [Fact]
    public async Task Heartbeat_WithoutPermission_IsDenied()
    {
        var result = await CreateService(new FakeHardwareRepository()).RecordHardwareHeartbeatAsync(
            CreateContext([]), Guid.NewGuid(), new(Now, []), CancellationToken.None);
        Assert.Equal("hardware.permission_denied", result.Error.Code);
    }

    [Theory]
    [InlineData(-301)]
    [InlineData(31)]
    public async Task Heartbeat_InvalidObservationTime_DoesNotUpdate(int seconds)
    {
        var pos = PosDevice.Create(Guid.NewGuid(), TenantId, OutletId, "POS001", "Tablet", "TABLET", "ACTIVE", UserId, Now);
        pos.PairForActivation("Tablet", "TABLET", "ANDROID", "test", "test-fingerprint-hash", UserId, Now);
        var device = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "PRN001", "Printer",
            "RECEIPT_PRINTER", "USB", null, null, null, null, null, null, "ACTIVE", UserId, Now);
        var result = await CreateService(new FakeHardwareRepository { PosDevice = pos, EditableDevice = device })
            .RecordHardwareHeartbeatAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]), pos.Id,
                new(Now.AddSeconds(seconds), [new(device.Id)]), CancellationToken.None);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
        Assert.Null(device.LastSeenAt);
    }

    [Fact]
    public void HardwareHeartbeat_DoesNotMoveLastSeenBackwards()
    {
        var device = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "PRN001", "Printer",
            "RECEIPT_PRINTER", "USB", null, null, null, null, null, null, "ACTIVE", UserId, Now);
        device.RecordHeartbeat(Now);
        device.RecordHeartbeat(Now.AddMinutes(-1));
        Assert.Equal(Now, device.LastSeenAt);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("{\"protocol\":\"ESC/POS\",\"cashDrawer\":false}", false)]
    [InlineData("{\"protocol\":\"ESC/POS\",\"cashDrawer\":true}", true)]
    public async Task Drawer_RequiresConfiguredParentCapability(string? parentConfig, bool expectedSuccess)
    {
        var parent = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "P", "Printer",
            "RECEIPT_PRINTER", "USB", null, null, null, null, null, parentConfig, "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository { OutletExists = true, ParentDevice = parent };
        var result = await CreateService(repository).CreateAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]),
            ValidCreateRequest() with { HardwareDeviceType = "CASH_DRAWER", ConnectionType = "USB",
                ConfigJson = System.Text.Json.JsonSerializer.Serialize(new { parentPrinterId = parent.Id }) }, CancellationToken.None);
        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.Equal(expectedSuccess, repository.SavedDevice is not null);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(65, false)]
    public async Task Heartbeat_RejectsDuplicateOrOversizedBatch(int count, bool duplicates)
    {
        var pos = PosDevice.Create(Guid.NewGuid(), TenantId, OutletId, "POS001", "Tablet", "TABLET", "ACTIVE", UserId, Now);
        pos.PairForActivation("Tablet", "TABLET", "ANDROID", "test", "test-fingerprint-hash", UserId, Now);
        var id = Guid.NewGuid();
        var items = Enumerable.Range(0, count).Select(_ => new PosHardwareHeartbeatItemRequest(duplicates ? id : Guid.NewGuid())).ToArray();
        var result = await CreateService(new FakeHardwareRepository { PosDevice = pos }).RecordHardwareHeartbeatAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]), pos.Id, new(Now, items), CancellationToken.None);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
    }

    [Theory]
    [InlineData(false, 1, false, "INACTIVE", "hardware.permission_denied")]
    [InlineData(true, 2, false, "INACTIVE", "hardware.version_conflict")]
    [InlineData(true, 1, true, "INACTIVE", "hardware.assignment_conflict")]
    [InlineData(true, 1, false, "DELETED", "hardware.validation_failed")]
    [InlineData(true, 1, false, "INACTIVE", null)]
    public async Task Update_ProtectsPermissionVersionAndAssignments(bool manage, int version, bool assigned, string status, string? error)
    {
        var device = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "PRN001", "Printer",
            "RECEIPT_PRINTER", "NETWORK", "Vendor", "Model", "Serial", null, null, "{}", "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository { EditableDevice = device,
            ActiveAssignment = assigned ? HardwareDeviceAssignment.Create(Guid.NewGuid(), TenantId, OutletId,
                device.Id, TillId, null, true, UserId, Now) : null };
        var result = await CreateService(repository).UpdateAsync(CreateContext(manage
            ? [TenantAdminTillPermissions.HardwareManage] : [TenantAdminTillPermissions.HardwareView]),
            device.Id, new("Renamed printer", status, version), CancellationToken.None);
        if (error is not null)
        {
            Assert.Equal(error, result.Error.Code);
            Assert.Equal("Printer", device.HardwareDeviceName);
            Assert.Equal(1, device.ConfigurationVersion);
        }
        else
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.ConfigurationVersion);
            Assert.Equal("INACTIVE", device.Status);
            Assert.Equal("PRN001", device.HardwareDeviceCode);
            Assert.Equal("NETWORK", device.ConnectionType);
            Assert.Equal("Serial", device.SerialNumber);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Heartbeat_RejectsMissingTimeOrNullItem(bool missingTime)
    {
        var pos = PosDevice.Create(Guid.NewGuid(), TenantId, OutletId, "POS001", "Tablet", "TABLET", "ACTIVE", UserId, Now);
        pos.PairForActivation("Tablet", "TABLET", "ANDROID", "test", "test-fingerprint-hash", UserId, Now);
        var result = await CreateService(new FakeHardwareRepository { PosDevice = pos }).RecordHardwareHeartbeatAsync(
            CreateContext([TenantAdminTillPermissions.HardwareManage]), pos.Id,
            new(missingTime ? null : Now, [missingTime ? new(Guid.NewGuid()) : null!]), CancellationToken.None);
        Assert.Equal("hardware.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task TestReport_DeduplicatesAndRejectsChangedReplay_WithoutPersistingFreeText()
    {
        var pos = PosDevice.Create(Guid.NewGuid(), TenantId, OutletId, "POS001", "Tablet", "TABLET", "ACTIVE", UserId, Now);
        pos.PairForActivation("Tablet", "TABLET", "ANDROID", "test", "test-fingerprint-hash", UserId, Now);
        var device = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "PRN001", "Printer",
            "RECEIPT_PRINTER", "NETWORK", null, null, null, null, null, "{}", "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository { PosDevice = pos, EditableDevice = device };
        var service = CreateService(repository);
        var context = CreateContext([TenantAdminTillPermissions.HardwareManage]);
        var request = new PosHardwareTestResultRequest(device.Id, "PRINT", "SUCCESS", "OK",
            "arbitrary-client-text-must-not-persist", Now, pos.Id, Guid.NewGuid(), 1);
        var first = await service.ReportHardwareTestAsync(context, request, CancellationToken.None);
        var replay = await service.ReportHardwareTestAsync(context, request, CancellationToken.None);
        var conflict = await service.ReportHardwareTestAsync(context, request with { TestStatus = "FAILED" }, CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Value);
        Assert.NotNull(replay.Value);
        Assert.Equal(first.Value.TestLogId, replay.Value.TestLogId);
        Assert.Single(repository.TestLogs);
        Assert.DoesNotContain("arbitrary-client-text", repository.TestLogs[0].ResultMessage!);
        Assert.Null(repository.TestLogs[0].PhysicalConfirmation);
        Assert.Equal("hardware.idempotency_conflict", conflict.Error.Code);
    }

    [Fact]
    public async Task Heartbeat_StaleConfigurationCannotRefreshReadiness()
    {
        var pos = PosDevice.Create(Guid.NewGuid(), TenantId, OutletId, "POS001", "Tablet", "TABLET", "ACTIVE", UserId, Now);
        pos.PairForActivation("Tablet", "TABLET", "ANDROID", "test", "test-fingerprint-hash", UserId, Now);
        var device = HardwareDevice.Create(Guid.NewGuid(), TenantId, OutletId, null, "PRN001", "Printer",
            "RECEIPT_PRINTER", "NETWORK", null, null, null, null, null, "{}", "ACTIVE", UserId, Now);
        var repository = new FakeHardwareRepository { PosDevice = pos, EditableDevice = device };
        var service = CreateService(repository);
        var request = new PosHardwareHeartbeatRequest(Now,
            [new(device.Id, "CONNECTED", "HEALTHY", ConfigurationVersion: 2)]);
        var denied = await service.RecordHardwareHeartbeatAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]), pos.Id, request, CancellationToken.None);
        Assert.Equal("hardware.version_conflict", denied.Error.Code);
        Assert.Null(device.LastSeenAt);
        Assert.Empty(repository.TestLogs);
        var accepted = await service.RecordHardwareHeartbeatAsync(CreateContext([TenantAdminTillPermissions.HardwareManage]), pos.Id,
            request with { Hardware = [new(device.Id, "CONNECTED", "HEALTHY", ConfigurationVersion: 1)] }, CancellationToken.None);
        Assert.True(accepted.IsSuccess);
        Assert.Equal(Now, device.LastSeenAt);
    }

    private static TenantAdminHardwareService CreateService(FakeHardwareRepository repository) =>
        new(repository, new FixedClock(), new FakeAuditLogger());

    private static TenantRequestContext CreateContext(string[] permissions) =>
        new(TenantId, UserId, permissions);

    private static TenantAdminHardwareDeviceCreateRequest ValidCreateRequest() =>
        new(OutletId, "PRN001", "Front Printer", "RECEIPT_PRINTER", "NETWORK");

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeAuditLogger : ITenantAdminHardwareAuditLogger
    {
        public void LogHardwareAssigned(Guid tenantId, Guid? actorUserId, Guid assignmentId, Guid hardwareDeviceId, Guid? tillId, Guid? posDeviceId) { }
        public void LogHardwareCreated(Guid tenantId, Guid? actorUserId, Guid hardwareDeviceId, string deviceCode, string hardwareType) { }
        public void LogHardwareHeartbeat(Guid tenantId, Guid posDeviceId, Guid hardwareDeviceId, string? warningCode) { }
        public void LogHardwareReleased(Guid tenantId, Guid? actorUserId, Guid assignmentId, Guid hardwareDeviceId, string? reason) { }
        public void LogHardwareTestFailed(Guid tenantId, Guid hardwareDeviceId, string testType, string? message) { }
    }

    private sealed class FakeHardwareRepository : ITenantAdminHardwareRepository
    {
        public Task<HardwareTestLog?> GetTestByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult(TestLogs.FirstOrDefault(x => x.TenantId == tenantId && x.RequestId == requestId));
        public List<HardwareTestLog> TestLogs { get; } = [];
        public Task<HardwareTestLog?> GetLatestTelemetryAsync(Guid tenantId, Guid hardwareId, CancellationToken cancellationToken) => Task.FromResult<HardwareTestLog?>(null);
        public bool OutletExists { get; set; }
        public bool CodeExists { get; set; }
        public HardwareDevice? SavedDevice { get; private set; }
        public HardwareDevice? EditableDevice { get; set; }
        public HardwareDevice? ParentDevice { get; set; }
        public HardwareDeviceAssignment? ParentAssignment { get; set; }
        public HardwareDeviceAssignment? SavedAssignment { get; private set; }
        public IReadOnlyList<HardwareDeviceListRow> ListedRows { get; set; } = [];
        public Till? Till { get; set; }
        public PosDevice? PosDevice { get; set; }
        public HardwareDeviceAssignment? ActiveAssignment { get; set; }

        public Task AddAssignmentAsync(HardwareDeviceAssignment assignment, CancellationToken cancellationToken) {
            SavedAssignment = assignment;
            return Task.CompletedTask;
        }
        public Task AddDeviceAsync(HardwareDevice device, CancellationToken cancellationToken)
        {
            SavedDevice = device;
            EditableDevice = device;
            return Task.CompletedTask;
        }
        public Task AddTestLogAsync(HardwareTestLog testLog, CancellationToken cancellationToken) { TestLogs.Add(testLog); return Task.CompletedTask; }
        public Task<bool> DeviceCodeExistsAsync(Guid tenantId, string hardwareDeviceCode, Guid? excludeDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(CodeExists);
        public Task<HardwareDeviceAssignment?> GetActiveAssignmentForDeviceAsync(Guid tenantId, Guid hardwareDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(hardwareDeviceId == ParentDevice?.Id ? ParentAssignment : ActiveAssignment);
        public Task<HardwareDeviceAssignment?> GetAssignmentAsync(Guid tenantId, Guid assignmentId, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveAssignment);
        public Task<HardwareDeviceDetailRow?> GetDetailAsync(Guid tenantId, Guid hardwareDeviceId, CancellationToken cancellationToken)
        {
            if (EditableDevice is null) return Task.FromResult<HardwareDeviceDetailRow?>(null);
            return Task.FromResult<HardwareDeviceDetailRow?>(new HardwareDeviceDetailRow(EditableDevice, "Outlet", ActiveAssignment));
        }
        public Task<HardwareDevice?> GetEditableDeviceAsync(Guid tenantId, Guid hardwareDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(hardwareDeviceId == ParentDevice?.Id ? ParentDevice : EditableDevice);
        public Task<PosDevice?> GetPosDeviceAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(PosDevice);
        public Task<Till?> GetTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult(Till);
        public Task<bool> IsHardwareLinkedToPosDeviceAsync(Guid tenantId, Guid posDeviceId, Guid hardwareDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<(IReadOnlyList<HardwareDeviceListRow> Items, int TotalCount)> ListAsync(
            Guid tenantId, Guid? outletId, string? hardwareType, string? lifecycleStatus, string? assignmentStatus,
            bool? availableOnly, string? search, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult((ListedRows, ListedRows.Count));
        public Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
            Task.FromResult(OutletExists);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
