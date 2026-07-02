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

public sealed class TillServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTillRepository());

        var result = await service.CreateAsync(CreateContext([TillConstants.ViewPermission]), CreateValidCreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithManagePermission_ReturnsSuccess()
    {
        var repository = new FakeTillRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(CreateContext(), CreateValidCreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedTill);
        Assert.Equal("TILL001", repository.AddedTill!.TillCode);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStatus_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeTillRepository());
        var request = CreateValidCreateRequest() with { Status = "DELETED" };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithMissingOutlet_ReturnsOutletNotFound()
    {
        var service = CreateService(new FakeTillRepository { OutletExists = false });

        var result = await service.CreateAsync(CreateContext(), CreateValidCreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.outlet_not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCodePreCheck_ReturnsDuplicateCode()
    {
        var service = CreateService(new FakeTillRepository { DuplicateCode = true });

        var result = await service.CreateAsync(CreateContext(), CreateValidCreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCodeRace_ReturnsDuplicateCode()
    {
        var service = CreateService(new FakeTillRepository { AddSucceeds = false });

        var result = await service.CreateAsync(CreateContext(), CreateValidCreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task ListAsync_WithViewPermission_ReturnsSuccess()
    {
        var service = CreateService(new FakeTillRepository());

        var result = await service.ListAsync(CreateContext([TillConstants.ViewPermission]), null, 1, 50, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WithOutletChangeAndAssignedDevice_ReturnsConflict()
    {
        var existingTill = Till.Create(Guid.NewGuid(), TenantId, OutletId, "Till 001", "TILL001", "ACTIVE", Now);
        var service = CreateService(new FakeTillRepository
        {
            EditableTill = existingTill,
            HasDeviceAssignment = true
        });
        var request = new TillUpdateRequest(Guid.NewGuid(), "Till 001", "ACTIVE");

        var result = await service.UpdateAsync(CreateContext([TillConstants.UpdatePermission]), existingTill.Id, request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.outlet_change_conflict", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateCodeRace_ReturnsDuplicateCode()
    {
        var existingTill = Till.Create(Guid.NewGuid(), TenantId, OutletId, "Till 001", "TILL001", "ACTIVE", Now);
        var service = CreateService(new FakeTillRepository
        {
            EditableTill = existingTill,
            SaveSucceeds = false
        });

        var result = await service.UpdateAsync(CreateContext([TillConstants.UpdatePermission]), existingTill.Id, CreateValidUpdateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithAssignedDevice_ReturnsDeleteConflict()
    {
        var existingTill = Till.Create(Guid.NewGuid(), TenantId, OutletId, "Till 001", "TILL001", "ACTIVE", Now);
        var service = CreateService(new FakeTillRepository
        {
            EditableTill = existingTill,
            HasDeviceAssignment = true
        });

        var result = await service.DeleteAsync(CreateContext([TillConstants.DeletePermission]), existingTill.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.delete_conflict", result.Error.Code);
    }

    private static TillService CreateService(FakeTillRepository repository)
    {
        return new TillService(repository, new FakeCodeSequenceRepository(), new TillRequestValidator(), new FakeDateTimeProvider());
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string>? permissions = null)
    {
        return new TenantRequestContext(TenantId, UserId, permissions ?? [TillConstants.ManagePermission]);
    }

    private static TillCreateRequest CreateValidCreateRequest()
    {
        return new TillCreateRequest(OutletId, "Till 001", "ACTIVE");
    }

    private static TillUpdateRequest CreateValidUpdateRequest()
    {
        return new TillUpdateRequest(OutletId, "Till 001", "ACTIVE");
    }

    private sealed class FakeCodeSequenceRepository : ICodeSequenceRepository
    {
        private int _nextValue;

        public Task<string> GetNextCodeAsync(Guid tenantId, string sequenceKey, string prefix, int paddingLength, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _nextValue++;
            return Task.FromResult($"{prefix}{_nextValue.ToString().PadLeft(paddingLength, '0')}");
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeTillRepository : ITillRepository
    {
        public bool OutletExists { get; init; } = true;
        public bool DuplicateCode { get; init; }
        public bool HasDeviceAssignment { get; init; }
        public bool AddSucceeds { get; init; } = true;
        public bool SaveSucceeds { get; init; } = true;
        public Till? EditableTill { get; init; }
        public Till? AddedTill { get; private set; }

        public Task<bool> ActiveOutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(OutletExists);

        public Task<bool> TillCodeExistsAsync(Guid tenantId, Guid outletId, string tillCode, Guid? excludeTillId, CancellationToken cancellationToken) => Task.FromResult(DuplicateCode);

        public Task<TillListResponse> ListAsync(Guid tenantId, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TillListResponse([], pageNumber, pageSize, 0));
        }

        public Task<TillResponse?> GetByIdAsync(Guid tenantId, Guid tillId, bool includeDeleted, CancellationToken cancellationToken)
        {
            return Task.FromResult<TillResponse?>(new TillResponse(tillId, OutletId, "MAIN", "Main Outlet", "TILL001", "Till 001", "ACTIVE", false, Now, Now));
        }

        public Task<Till?> GetEditableAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) => Task.FromResult(EditableTill);

        public Task<bool> HasDeviceAssignmentAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) => Task.FromResult(HasDeviceAssignment);

        public Task<bool> AddAsync(Till till, CancellationToken cancellationToken)
        {
            AddedTill = till;
            return Task.FromResult(AddSucceeds);
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(SaveSucceeds);
    }
}


