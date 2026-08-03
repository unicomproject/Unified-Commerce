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
        public bool OutletExists { get; set; }
        public bool CodeExists { get; set; }
        public HardwareDevice? SavedDevice { get; private set; }
        public HardwareDevice? EditableDevice { get; set; }
        public Till? Till { get; set; }
        public PosDevice? PosDevice { get; set; }
        public HardwareDeviceAssignment? ActiveAssignment { get; set; }

        public Task AddAssignmentAsync(HardwareDeviceAssignment assignment, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddDeviceAsync(HardwareDevice device, CancellationToken cancellationToken)
        {
            SavedDevice = device;
            EditableDevice = device;
            return Task.CompletedTask;
        }
        public Task AddTestLogAsync(HardwareTestLog testLog, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> DeviceCodeExistsAsync(Guid tenantId, string hardwareDeviceCode, Guid? excludeDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(CodeExists);
        public Task<HardwareDeviceAssignment?> GetActiveAssignmentForDeviceAsync(Guid tenantId, Guid hardwareDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveAssignment);
        public Task<HardwareDeviceAssignment?> GetAssignmentAsync(Guid tenantId, Guid assignmentId, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveAssignment);
        public Task<HardwareDeviceDetailRow?> GetDetailAsync(Guid tenantId, Guid hardwareDeviceId, CancellationToken cancellationToken)
        {
            if (EditableDevice is null) return Task.FromResult<HardwareDeviceDetailRow?>(null);
            return Task.FromResult<HardwareDeviceDetailRow?>(new HardwareDeviceDetailRow(EditableDevice, "Outlet", ActiveAssignment));
        }
        public Task<HardwareDevice?> GetEditableDeviceAsync(Guid tenantId, Guid hardwareDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(EditableDevice);
        public Task<PosDevice?> GetPosDeviceAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(PosDevice);
        public Task<Till?> GetTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult(Till);
        public Task<bool> IsHardwareLinkedToPosDeviceAsync(Guid tenantId, Guid posDeviceId, Guid hardwareDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<(IReadOnlyList<HardwareDeviceListRow> Items, int TotalCount)> ListAsync(
            Guid tenantId, Guid? outletId, string? hardwareType, string? lifecycleStatus, string? assignmentStatus,
            bool? availableOnly, string? search, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<HardwareDeviceListRow>)[], 0));
        public Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
            Task.FromResult(OutletExists);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
