using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Application.Modules.OutletTillDevice.Services;
using E_POS.Application.Modules.OutletTillDevice.Validators;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class PosDeviceServiceTests
{
    [Fact]
    public async Task CreateAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateDeviceService(new FakePosDeviceRepository());

        var result = await service.CreateAsync(CreateContext([]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_device.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCreatePermission_GeneratesCodeAndReturnsSuccess()
    {
        var repository = new FakePosDeviceRepository();
        var service = CreateDeviceService(repository);

        var result = await service.CreateAsync(CreateContext([PosDeviceConstants.CreatePermission]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.SavedDevice);
        Assert.Equal("DEV001", repository.SavedDevice!.DeviceCode);
        Assert.Equal("SN-001", repository.SavedDevice.DeviceSerialNumber);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSerialNumber_ReturnsDuplicateSerialNumber()
    {
        var service = CreateDeviceService(new FakePosDeviceRepository { DuplicateSerialNumber = true });

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_device.duplicate_serial_number", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithTillAssignment_ReturnsDeleteConflict()
    {
        var device = PosDevice.Create(Guid.NewGuid(), TenantId, OutletId, "Front Tablet", "DEV001", "SN-001", "ACTIVE", Now);
        var service = CreateDeviceService(new FakePosDeviceRepository
        {
            EditableDevice = device,
            HasTillAssignment = true
        });

        var result = await service.DeleteAsync(CreateContext(), device.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_device.delete_conflict", result.Error.Code);
    }

    [Fact]
    public async Task AssignAsync_WithoutManagePermission_ReturnsPermissionDenied()
    {
        var service = CreateAssignmentService(new FakeTillDeviceAssignmentRepository());

        var result = await service.AssignAsync(CreateContext([PosDeviceConstants.CreatePermission]), TillId, PosDeviceId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till_device_assignment.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task AssignAsync_WithManagePermission_ReturnsSuccess()
    {
        var repository = new FakeTillDeviceAssignmentRepository();
        var service = CreateAssignmentService(repository);

        var result = await service.AssignAsync(CreateContext([TillConstants.ManagePermission]), TillId, PosDeviceId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.SavedAssignment);
        Assert.Equal(TillId, repository.SavedAssignment!.TillId);
        Assert.Equal(PosDeviceId, repository.SavedAssignment.PosDeviceId);
    }

    [Fact]
    public async Task AssignAsync_WhenDeviceAssignedToAnotherTill_ReturnsConflict()
    {
        var service = CreateAssignmentService(new FakeTillDeviceAssignmentRepository { DeviceAssignedToAnotherTill = true });

        var result = await service.AssignAsync(CreateContext([TillConstants.ManagePermission]), TillId, PosDeviceId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till_device_assignment.device_already_assigned", result.Error.Code);
    }

    [Fact]
    public async Task AssignAsync_WhenOutletMismatch_ReturnsOutletMismatch()
    {
        var service = CreateAssignmentService(new FakeTillDeviceAssignmentRepository { SameOutlet = false });

        var result = await service.AssignAsync(CreateContext([TillConstants.ManagePermission]), TillId, PosDeviceId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till_device_assignment.outlet_mismatch", result.Error.Code);
    }

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid TillId = Guid.NewGuid();
    private static readonly Guid PosDeviceId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    private static PosDeviceService CreateDeviceService(FakePosDeviceRepository repository)
    {
        return new PosDeviceService(repository, new PosDeviceRequestValidator(), new FakeCodeSequenceRepository(), new FakeDateTimeProvider());
    }

    private static TillDeviceAssignmentService CreateAssignmentService(FakeTillDeviceAssignmentRepository repository)
    {
        return new TillDeviceAssignmentService(repository, new FakeDateTimeProvider());
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string>? permissions = null)
    {
        return new TenantRequestContext(TenantId, UserId, permissions ?? [PosDeviceConstants.ManagePermission]);
    }

    private static PosDeviceCreateRequest CreateValidRequest()
    {
        return new PosDeviceCreateRequest(OutletId, "Front Tablet", "sn-001", "ACTIVE");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeCodeSequenceRepository : ICodeSequenceRepository
    {
        public Task<string> GetNextCodeAsync(Guid tenantId, string sequenceKey, string prefix, int paddingLength, DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.FromResult("DEV001");
        }
    }

    private sealed class FakePosDeviceRepository : IPosDeviceRepository
    {
        public bool ActiveOutletExists { get; init; } = true;
        public bool DuplicateSerialNumber { get; init; }
        public bool HasTillAssignment { get; init; }
        public PosDevice? EditableDevice { get; init; }
        public PosDevice? SavedDevice { get; private set; }

        public Task<bool> ActiveOutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(ActiveOutletExists);
        public Task<bool> DeviceSerialNumberExistsAsync(string deviceSerialNumber, Guid? excludePosDeviceId, CancellationToken cancellationToken) => Task.FromResult(DuplicateSerialNumber);
        public Task<PosDeviceListResponse> ListAsync(Guid tenantId, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new PosDeviceListResponse([], pageNumber, pageSize, 0));
        public Task<PosDeviceResponse?> GetByIdAsync(Guid tenantId, Guid posDeviceId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<PosDeviceResponse?>(CreateDeviceResponse(posDeviceId));
        public Task<PosDevice?> GetEditableAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(EditableDevice);
        public Task<bool> HasTillAssignmentAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(HasTillAssignment);

        public Task AddAsync(PosDevice posDevice, CancellationToken cancellationToken)
        {
            SavedDevice = posDevice;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTillDeviceAssignmentRepository : ITillDeviceAssignmentRepository
    {
        public bool ActiveTillExists { get; init; } = true;
        public bool ActiveDeviceExists { get; init; } = true;
        public bool SameOutlet { get; init; } = true;
        public bool DeviceAssignedToAnotherTill { get; init; }
        public TillDeviceAssignment? ExistingAssignment { get; init; }
        public TillDeviceAssignment? SavedAssignment { get; private set; }

        public Task<TillDeviceAssignmentResponse?> GetByTillAndDeviceAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingAssignment is null && SavedAssignment is null ? null : CreateAssignmentResponse(tillId, posDeviceId));
        }

        public Task<TillDeviceAssignmentListResponse?> ListByTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) => Task.FromResult<TillDeviceAssignmentListResponse?>(new TillDeviceAssignmentListResponse(tillId, []));
        public Task<bool> ActiveTillExistsAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) => Task.FromResult(ActiveTillExists);
        public Task<bool> ActiveDeviceExistsAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(ActiveDeviceExists);
        public Task<bool> TillAndDeviceShareOutletAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(SameOutlet);
        public Task<bool> DeviceAssignedToAnyTillAsync(Guid tenantId, Guid posDeviceId, Guid? excludeTillId, CancellationToken cancellationToken) => Task.FromResult(DeviceAssignedToAnotherTill);
        public Task<TillDeviceAssignment?> GetEditableAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken) => Task.FromResult(ExistingAssignment);

        public Task AddAsync(TillDeviceAssignment assignment, CancellationToken cancellationToken)
        {
            SavedAssignment = assignment;
            return Task.CompletedTask;
        }

        public Task RevokeAsync(TillDeviceAssignment assignment, DateTimeOffset now, CancellationToken cancellationToken)
        {
            assignment.Revoke(now);
            return Task.CompletedTask;
        }
    }

    private static PosDeviceResponse CreateDeviceResponse(Guid posDeviceId)
    {
        return new PosDeviceResponse(posDeviceId, OutletId, "OUT001", "Main Outlet", "DEV001", "Front Tablet", "SN-001", "ACTIVE", null, null, null, Now, Now);
    }

    private static TillDeviceAssignmentResponse CreateAssignmentResponse(Guid tillId, Guid posDeviceId)
    {
        return new TillDeviceAssignmentResponse(Guid.NewGuid(), tillId, "TILL001", "Main Till", posDeviceId, "DEV001", "Front Tablet", OutletId, "OUT001", "Main Outlet", Now.UtcDateTime.ToString("O"), Now, Now);
    }
}