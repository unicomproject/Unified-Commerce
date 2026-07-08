using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class TillServiceTests
{
    [Fact]
    public async Task CreateAsync_WithoutTillCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTillRepository());

        var result = await service.CreateAsync(CreateContext([]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCreatePermission_ReturnsSuccess()
    {
        var service = CreateService(new FakeTillRepository());

        var result = await service.CreateAsync(CreateContext([TillConstants.CreatePermission]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_WithViewPermissionOnly_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTillRepository());

        var result = await service.CreateAsync(CreateContext([TillConstants.ViewPermission]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStatus_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeTillRepository());
        var request = CreateValidRequest() with { Status = "DELETED" };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenOutletIsMissing_ReturnsOutletNotFound()
    {
        var service = CreateService(new FakeTillRepository { ActiveOutletExists = false });

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.outlet_not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsDuplicateCode()
    {
        var service = CreateService(new FakeTillRepository { DuplicateCode = true });

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task ListAsync_WithViewPermission_ReturnsSuccess()
    {
        var service = CreateService(new FakeTillRepository());

        var result = await service.ListAsync(CreateContext([TillConstants.ViewPermission]), OutletId, 1, 50, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WithUpdatePermission_ReturnsSuccess()
    {
        var till = Till.Create(Guid.NewGuid(), TenantId, OutletId, "Main", 1, "Main Till 01", "MAIN-01", "ACTIVE", Now);
        var service = CreateService(new FakeTillRepository { EditableTill = till });

        var result = await service.UpdateAsync(CreateContext([TillConstants.UpdatePermission]), till.Id, new TillUpdateRequest(OutletId, "Main", 1, "Updated Till 01", "main-02", "INACTIVE"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WithDeviceAssignment_ReturnsDeleteConflict()
    {
        var till = Till.Create(Guid.NewGuid(), TenantId, OutletId, "Main", 1, "Main Till 01", "MAIN-01", "ACTIVE", Now);
        var service = CreateService(new FakeTillRepository
        {
            EditableTill = till,
            HasDeviceAssignment = true
        });

        var result = await service.DeleteAsync(CreateContext(), till.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.delete_conflict", result.Error.Code);
    }

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    private static TillService CreateService(FakeTillRepository repository)
    {
        return new TillService(repository, new TillRequestValidator(), new FakeDateTimeProvider());
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string>? permissions = null)
    {
        return new TenantRequestContext(TenantId, UserId, permissions ?? [TillConstants.ManagePermission]);
    }

    private static TillCreateRequest CreateValidRequest()
    {
        return new TillCreateRequest(OutletId, "Main", 1, "Main Till 01", "main-01", "ACTIVE");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeTillRepository : ITillRepository
    {
        public bool ActiveOutletExists { get; init; } = true;
        public bool DuplicateCode { get; init; }
        public bool HasDeviceAssignment { get; init; }
        public Till? EditableTill { get; init; }
        public Till? SavedTill { get; private set; }

        public Task<bool> ActiveOutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(ActiveOutletExists);
        public Task<bool> TillCodeExistsAsync(Guid tenantId, Guid outletId, string tillCode, Guid? excludeTillId, CancellationToken cancellationToken) => Task.FromResult(DuplicateCode);
        public Task<bool> TillAreaNumberExistsAsync(Guid tenantId, Guid outletId, string tillAreaName, int tillNumber, Guid? excludeTillId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<TillListResponse> ListAsync(Guid tenantId, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new TillListResponse([], pageNumber, pageSize, 0));
        public Task<TillResponse?> GetByIdAsync(Guid tenantId, Guid tillId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<TillResponse?>(CreateResponse(tillId));
        public Task<Till?> GetEditableAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) => Task.FromResult(EditableTill);
        public Task<bool> HasDeviceAssignmentAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) => Task.FromResult(HasDeviceAssignment);

        public Task AddAsync(Till till, CancellationToken cancellationToken)
        {
            SavedTill = till;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static TillResponse CreateResponse(Guid tillId)
        {
            return new TillResponse(tillId, OutletId, "MAIN", "Main Outlet", "Main", 1, "MAIN-01", "Main Till 01", "ACTIVE", false, Now, Now);
        }
    }
}


