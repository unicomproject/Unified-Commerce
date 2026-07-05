using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Application.Modules.OutletTillDevice.Services;
using E_POS.Application.Modules.OutletTillDevice.Validators;
using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class OutletServiceTests
{
    [Fact]
    public async Task CreateAsync_WithoutOutletPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext([]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsDuplicateCode()
    {
        var service = CreateService(new FakeOutletRepository { DuplicateCode = true });

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.duplicate_code", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateBusinessHourDay_ReturnsValidationFailure()
    {
        var request = CreateValidRequest() with
        {
            BusinessHours =
            [
                new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new OutletBusinessHourRequest(1, new TimeOnly(10, 0), new TimeOnly(18, 0))
            ]
        };
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCollectionEnabledAndNoPickupMethod_ReturnsPickupMethodMissing()
    {
        var request = CreateValidRequest() with { CollectionEnabled = true };
        var service = CreateService(new FakeOutletRepository { PickupMethodId = null });

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.pickup_method_missing", result.Error.Code);
    }

    [Fact]
    public async Task ListAsync_WithViewPermission_ReturnsSuccess()
    {
        var service = CreateService(new FakeOutletRepository());

        var result = await service.ListAsync(CreateContext([OutletConstants.ViewPermission]), 1, 50, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_WithViewPermissionOnly_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext([OutletConstants.ViewPermission]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.permission_denied", result.Error.Code);
    }
    [Fact]
    public async Task DeleteAsync_WithActiveTillOrDevice_ReturnsDeleteConflict()
    {
        var outlet = Outlet.Create(Guid.NewGuid(), TenantId, "Main", "MAIN", "ACTIVE", "STORE", true, null, null, Now);
        var service = CreateService(new FakeOutletRepository
        {
            EditAggregate = new OutletEditAggregate(outlet, null, [], null),
            HasActiveTillOrDevice = true
        });

        var result = await service.DeleteAsync(CreateContext(), outlet.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.delete_conflict", result.Error.Code);
    }

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    private static OutletService CreateService(FakeOutletRepository repository)
    {
        return new OutletService(repository, new FakeCodeSequenceRepository(), new OutletRequestValidator(), new FakeDateTimeProvider());
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string>? permissions = null)
    {
        return new TenantRequestContext(TenantId, UserId, permissions ?? [OutletConstants.ManagePermission]);
    }

    private static OutletCreateRequest CreateValidRequest()
    {
        return new OutletCreateRequest(
            "Main Outlet",
            "ACTIVE",
            "STORE",
            true,
            "+94770000000",
            "main@example.com",
            new OutletAddressRequest("1 Main Street", null, "Colombo", "Western", "00100", "LK"),
            [new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0))],
            false);
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

    private sealed class FakeOutletRepository : IOutletRepository
    {
        public bool DuplicateCode { get; init; }
        public Guid? PickupMethodId { get; init; } = Guid.NewGuid();
        public OutletEditAggregate? EditAggregate { get; init; }
        public bool HasActiveTillOrDevice { get; init; }

        public Task<bool> OutletCodeExistsAsync(Guid tenantId, string outletCode, Guid? excludeOutletId, CancellationToken cancellationToken) => Task.FromResult(DuplicateCode);
        public Task<Guid?> GetActivePickupFulfillmentMethodIdAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(PickupMethodId);
        public Task<OutletListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new OutletListResponse([], pageNumber, pageSize, 0));
        public Task<OutletResponse?> GetByIdAsync(Guid tenantId, Guid outletId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<OutletResponse?>(CreateResponse(outletId));
        public Task<OutletEditAggregate?> GetEditAggregateAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(EditAggregate);
        public Task<bool> HasActiveTillOrDeviceAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(HasActiveTillOrDevice);
        public Task<bool> AllOutletsBelongToTenantAsync(Guid tenantId, Guid[] outletIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public Outlet? AddedOutlet { get; private set; }
        public Task<bool> AddAsync(Outlet outlet, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? pickupMapping, CancellationToken cancellationToken)
        {
            AddedOutlet = outlet;
            return Task.FromResult(true);
        }
        public Task<bool> SaveUpdatedAsync(OutletEditAggregate aggregate, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? newPickupMapping, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static OutletResponse CreateResponse(Guid outletId)
        {
            return new OutletResponse(
                outletId,
                "MAIN001",
                "Main Outlet",
                "ACTIVE",
                "STORE",
                true,
                null,
                null,
                new OutletAddressResponse(Guid.NewGuid(), "PHYSICAL", "1 Main Street", null, "Colombo", null, null, "LK"),
                [],
                false,
                Now,
                Now);
        }
    }
}
