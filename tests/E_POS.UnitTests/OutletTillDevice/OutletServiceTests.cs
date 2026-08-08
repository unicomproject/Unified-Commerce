using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.UnitTests.TestSupport;
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
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "outletCode");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateBusinessHourDay_ReturnsValidationFailure()
    {
        var request = CreateValidRequest() with
        {
            BusinessHours =
            [
                new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null),
                new OutletBusinessHourRequest(1, new TimeOnly(10, 0), new TimeOnly(18, 0), false, null, null)
            ]
        };
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field.Contains("dayOfWeek"));
    }

    [Fact]
    public async Task CreateAsync_WithCollectionEnabledAndNoPickupMethod_ReturnsPickupMethodMissing()
    {
        var request = CreateValidRequest() with
        {
            CollectionEnabled = true,
            PreparationLeadMinutes = 30,
            PickupWindowMinutes = 30
        };
        var service = CreateService(new FakeOutletRepository { PickupMethodId = null });

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.pickup_method_missing", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCollectionEnabledAndClickCollectDisabled_ReturnsFeatureDisabled()
    {
        var request = CreateValidRequest() with
        {
            CollectionEnabled = true,
            PreparationLeadMinutes = 30,
            PickupWindowMinutes = 30
        };
        var service = CreateService(new FakeOutletRepository { ClickCollectFeatureEnabled = false });

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.click_collect_feature_disabled", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCollectionEnabledAndMissingConfiguration_ReturnsValidationFailure()
    {
        var request = CreateValidRequest() with { CollectionEnabled = true };
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "preparationLeadMinutes");
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "pickupWindowMinutes");
    }

    [Fact]
    public async Task CreateAsync_WithCollectionConfigurationOutsideAllowedRange_ReturnsValidationFailure()
    {
        var request = CreateValidRequest() with
        {
            CollectionEnabled = true,
            PreparationLeadMinutes = 10_081,
            PickupWindowMinutes = 1_441
        };
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "preparationLeadMinutes");
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "pickupWindowMinutes");
    }

    [Fact]
    public async Task CreateAsync_WithCollectionEnabledAndNoCurrentlyValidOpenHours_ReturnsValidationFailure()
    {
        var request = CreateValidRequest() with
        {
            CollectionEnabled = true,
            PreparationLeadMinutes = 30,
            PickupWindowMinutes = 30,
            BusinessHours =
            [
                new OutletBusinessHourRequest(
                    1,
                    new TimeOnly(9, 0),
                    new TimeOnly(17, 0),
                    false,
                    null,
                    new DateOnly(2000, 1, 1))
            ]
        };
        var service = CreateService(new FakeOutletRepository());

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "businessHours");
    }

    [Fact]
    public async Task CreateAsync_WithCollectionConfiguration_PassesConfigurationToPickupMapping()
    {
        var repository = new FakeOutletRepository();
        var request = CreateValidRequest() with
        {
            CollectionEnabled = true,
            PreparationLeadMinutes = 45,
            PickupWindowMinutes = 30,
            CollectionCutoffTime = new TimeOnly(16, 30)
        };
        var service = CreateService(repository);

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedPickupMapping);
        Assert.Equal(45, repository.AddedPickupMapping!.PreparationLeadMinutes);
        Assert.Equal(30, repository.AddedPickupMapping.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(16, 30), repository.AddedPickupMapping.CutoffTime);
        Assert.Equal(OutletConstants.ActiveStatus, repository.AddedPickupMapping.Status);
        Assert.Equal(TenantId, repository.AddedPickupMapping.TenantId);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_UsesServerTenantAndUserContext()
    {
        var repository = new FakeOutletRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedOutlet);
        Assert.Equal(TenantId, repository.AddedOutlet!.TenantId);
        Assert.Equal(UserId, repository.AddedOutlet.CreatedByTenantUserId);
    }

    [Fact]
    public async Task CreateAsync_WithMissingOutletName_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with { OutletName = " " };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "outletName");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTimezone_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with { Timezone = "Not/A/Timezone" };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "timezone");
    }

    [Fact]
    public async Task CreateAsync_WithIanaTimezone_SucceedsOnWindows()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with { Timezone = "Asia/Colombo" };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEmail_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with { Email = "not-an-email" };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "email");
    }

    [Fact]
    public async Task CreateAsync_WithUnsupportedOutletType_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with { OutletType = "RETAIL" };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "outletType");
    }

    [Fact]
    public async Task CreateAsync_WithDeletedStatus_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with { Status = OutletConstants.DeletedStatus };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "status");
    }

    [Fact]
    public async Task UpdateAsync_WithDeletedStatus_ReturnsValidationFailure()
    {
        var aggregate = new OutletEditAggregate(
            Outlet.Create(Guid.NewGuid(), TenantId, "Main Outlet", "OUT001", "ACTIVE", "STORE", "UTC", false, null, null, UserId, Now),
            OutletAddress.Create(Guid.NewGuid(), TenantId, Guid.NewGuid(), "1 Main Street", null, "Colombo", "Western", "00100", "LK", null, null, null, UserId, Now),
            [],
            null);
        var service = CreateService(new FakeOutletRepository { EditAggregate = aggregate });
        var request = CreateValidUpdateRequest() with { Status = OutletConstants.DeletedStatus };

        var result = await service.UpdateAsync(CreateContext(), aggregate.Outlet.Id, request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "status");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCountryCode_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with
        {
            Address = new OutletAddressRequest("1 Main Street", null, "Colombo", "Western", "00100", "ZZ", null, null, null)
        };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field == "address.countryCode");
    }

    [Fact]
    public async Task CreateAsync_WithOpenDayMissingTimes_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with
        {
            BusinessHours = [new OutletBusinessHourRequest(1, null, new TimeOnly(17, 0), false, null, null)]
        };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field.Contains("openingTime"));
    }

    [Fact]
    public async Task CreateAsync_WithClosedDayAndTimes_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeOutletRepository());
        var request = CreateValidRequest() with
        {
            BusinessHours = [new OutletBusinessHourRequest(0, new TimeOnly(9, 0), new TimeOnly(17, 0), true, null, null)]
        };

        var result = await service.CreateAsync(CreateContext(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Error.FieldErrors ?? [], field => field.Field.Contains("openingTime"));
    }

    [Fact]
    public async Task CreateAsync_WithSuspendedTenant_ReturnsTenantBlocked()
    {
        var service = CreateService(new FakeOutletRepository { TenantStatus = TenantStatusConstants.Suspended });

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.tenant_blocked", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithFeatureDisabled_ReturnsFeatureDisabled()
    {
        var service = CreateService(new FakeOutletRepository { OutletFeatureEnabled = false });

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.feature_disabled", result.Error.Code);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_ReturnsLookupData()
    {
        var service = CreateService(new FakeOutletRepository());

        var result = await service.GetCreateOptionsAsync(CreateContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.OutletTypes);
        Assert.NotEmpty(result.Value.Countries);
        Assert.NotEmpty(result.Value.Timezones);
        Assert.Equal("ACTIVE", result.Value.Defaults.Status);
    }

    [Fact]
    public async Task ListAsync_WithViewPermission_ReturnsSuccess()
    {
        var service = CreateService(new FakeOutletRepository());

        var result = await service.ListAsync(CreateContext([OutletConstants.ViewPermission]), 1, 50, null, null, null, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ListAsync_WithoutEntitlement_ReturnsFeatureDisabled_AndDoesNotQueryList()
    {
        var repository = new FakeOutletRepository { OutletFeatureEnabled = false };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            CreateContext([OutletConstants.ViewPermission]),
            1,
            50,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.feature_disabled", result.Error.Code);
        Assert.Equal(0, repository.ListCallCount);
    }

    [Fact]
    public async Task ListAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        var repository = new FakeOutletRepository();
        var service = CreateService(repository);

        var result = await service.ListAsync(CreateContext([]), 1, 50, null, null, null, null, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.ListCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutEntitlement_ReturnsFeatureDisabled_AndDoesNotQueryDetail()
    {
        var repository = new FakeOutletRepository { OutletFeatureEnabled = false };
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(CreateContext([OutletConstants.ViewPermission]), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.feature_disabled", result.Error.Code);
        Assert.Equal(0, repository.GetByIdCallCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithoutEntitlement_ReturnsFeatureDisabled_AndDoesNotQuerySummary()
    {
        var repository = new FakeOutletRepository { OutletFeatureEnabled = false };
        var service = CreateService(repository);

        var result = await service.GetSummaryAsync(CreateContext([OutletConstants.ViewPermission]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.feature_disabled", result.Error.Code);
        Assert.Equal(0, repository.GetSummaryCallCount);
    }

    [Fact]
    public async Task DeleteAsync_WithoutEntitlement_ReturnsFeatureDisabled_AndDoesNotMutate()
    {
        var outlet = Outlet.Create(Guid.NewGuid(), TenantId, "Main", "MAIN", "ACTIVE", "STORE", "UTC", false, null, null, UserId, Now);
        var repository = new FakeOutletRepository
        {
            OutletFeatureEnabled = false,
            EditAggregate = new OutletEditAggregate(outlet, null, [], null)
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(CreateContext(), outlet.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.feature_disabled", result.Error.Code);
        Assert.Equal(0, repository.GetEditAggregateCallCount);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        var outlet = Outlet.Create(Guid.NewGuid(), TenantId, "Main", "MAIN", "ACTIVE", "STORE", "UTC", false, null, null, UserId, Now);
        var repository = new FakeOutletRepository
        {
            EditAggregate = new OutletEditAggregate(outlet, null, [], null)
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(CreateContext([]), outlet.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.GetEditAggregateCallCount);
        Assert.Equal(0, repository.SaveChangesCallCount);
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
        var outlet = Outlet.Create(Guid.NewGuid(), TenantId, "Main", "MAIN", "ACTIVE", "STORE", "UTC", false, null, null, UserId, Now);
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

    [Fact]
    public async Task CreateAsync_WhenSubscriptionLimitReached_ReturnsLimitError()
    {
        var service = CreateService(new FakeOutletRepository(), new DenyingTenantResourceLimitGuard());

        var result = await service.CreateAsync(CreateContext(), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, result.Error.Code);
    }

    private static OutletService CreateService(
        FakeOutletRepository repository,
        ITenantResourceLimitGuard? limitGuard = null)
    {
        return new OutletService(
            repository,
            new FakeCodeSequenceRepository(),
            new OutletRequestValidator(),
            new FakeOutletAuditLogger(),
            new FakeDateTimeProvider(),
            new FakeTenantFeatureEntitlementEvaluator { OutletFeatureEnabled = repository.OutletFeatureEnabled },
            limitGuard ?? new AllowingTenantResourceLimitGuard());
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
            "UTC",
            false,
            "+94770000000",
            "main@example.com",
            new OutletAddressRequest("1 Main Street", null, "Colombo", "Western", "00100", "LK", null, null, null),
            [new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null)],
            false);
    }

    private static OutletUpdateRequest CreateValidUpdateRequest()
    {
        return new OutletUpdateRequest(
            "Updated Outlet",
            "ACTIVE",
            "STORE",
            "UTC",
            false,
            "+94770000000",
            "updated@example.com",
            new OutletAddressRequest("1 Main Street", null, "Colombo", "Western", "00100", "LK", null, null, null),
            [new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null)],
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

    private sealed class FakeOutletAuditLogger : IOutletAuditLogger
    {
        public int CreatedCount { get; private set; }

        public void LogOutletCreated(Guid tenantId, Guid actorTenantUserId, Guid outletId, string outletCode, string outletType, string status)
        {
            CreatedCount++;
        }

        public void LogManagerAssigned(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid assignedTenantUserId) { }
        public void LogManagerRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogImageAssociated(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid mediaAssetId) { }
        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid mediaAssetId) { }
        public void LogImageReplaced(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid previousMediaAssetId, Guid newMediaAssetId) { }
        public void LogImageDetached(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid detachedMediaAssetId) { }
        public void LogStatusChanged(Guid tenantId, Guid actorTenantUserId, Guid outletId, string status) { }
    }

    private sealed class FakeOutletRepository : IOutletRepository
    {
        public bool DuplicateCode { get; init; }
        public Guid? PickupMethodId { get; init; } = Guid.NewGuid();
        public OutletEditAggregate? EditAggregate { get; init; }
        public bool HasActiveTillOrDevice { get; init; }
        public string? TenantStatus { get; init; } = TenantAuthConstants.ActiveTenantStatus;
        public bool OutletFeatureEnabled { get; init; } = true;
        public bool ClickCollectFeatureEnabled { get; init; } = true;
        public int ListCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int GetSummaryCallCount { get; private set; }
        public int GetEditAggregateCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        private readonly List<Outlet> _outlets = [];

        public Task<bool> OutletCodeExistsAsync(Guid tenantId, string outletCode, Guid? excludeOutletId, CancellationToken cancellationToken) => Task.FromResult(DuplicateCode);
        public Task<Guid?> GetActivePickupFulfillmentMethodIdAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(PickupMethodId);
        public Task<OutletListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, string? outletType, string? status, string? sortBy, string? sortDirection, CancellationToken cancellationToken)
        {
            ListCallCount++;
            var query = _outlets.Where(x => x.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.OutletName.Contains(search) || x.OutletCode.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(outletType))
            {
                query = query.Where(x => x.OutletType == outletType);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(x => new OutletSummaryResponse(x.Id, x.OutletCode, x.OutletName, x.Status, x.OutletType, x.Timezone, x.IsDefaultOutlet, x.Phone, x.Email, true, null, null, null, null, 1))
                .ToList();
            return Task.FromResult(new OutletListResponse(items, pageNumber, pageSize, query.Count()));
        }

        public Task<OutletSummaryDashboardResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            GetSummaryCallCount++;
            return Task.FromResult(new OutletSummaryDashboardResponse(1, 1, 0, null));
        }

        public Task<OutletResponse?> GetByIdAsync(Guid tenantId, Guid outletId, bool includeDeleted, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            return Task.FromResult<OutletResponse?>(null);
        }

        public Task<OutletEditAggregate?> GetEditAggregateAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
        {
            GetEditAggregateCallCount++;
            return Task.FromResult(EditAggregate);
        }
        public Task<bool> HasActiveTillOrDeviceAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) => Task.FromResult(HasActiveTillOrDevice);
        public Task<bool> AllOutletsBelongToTenantAsync(Guid tenantId, Guid[] outletIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(TenantStatus);
        public Task<bool> IsOutletManagementFeatureEnabledAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(OutletFeatureEnabled);
        public Task<bool> IsClickCollectFeatureEnabledAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(ClickCollectFeatureEnabled);
        public Task<OutletCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(new OutletCreateOptionsResponse(
                [new OutletLookupOptionResponse("STORE", "Store")],
                [new OutletCountryOptionResponse("LK", "Sri Lanka")],
                [new OutletLookupOptionResponse("UTC", "UTC")],
                new OutletCreateDefaultsResponse("LK", "UTC", "ACTIVE")));
        public Outlet? AddedOutlet { get; private set; }
        public FulfillmentMethodOutlet? AddedPickupMapping { get; private set; }
        public Task<bool> AddAsync(Outlet outlet, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? pickupMapping, CancellationToken cancellationToken)
        {
            AddedOutlet = outlet;
            AddedPickupMapping = pickupMapping;
            return Task.FromResult(true);
        }
        public Task<bool> SaveUpdatedAsync(OutletEditAggregate aggregate, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? newPickupMapping, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        private static OutletResponse CreateResponse(Guid outletId)
        {
            return new OutletResponse(
                outletId,
                "MAIN001",
                "Main Outlet",
                "ACTIVE",
                "STORE",
                "UTC",
                false,
                null,
                null,
                new OutletAddressResponse(Guid.NewGuid(), "PHYSICAL", "1 Main Street", null, "Colombo", null, null, "LK", null, null, null, true, "ACTIVE"),
                [],
                false,
                null,
                null,
                null,
                null,
                Now,
                UserId,
                Now,
                UserId);
        }
    }

    private sealed class FakeTenantFeatureEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public bool OutletFeatureEnabled { get; init; } = true;

        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var canonical = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(featureCode);
            if (string.Equals(canonical, PlatformTenantFeatureCodes.OutletManagement, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(OutletFeatureEnabled
                    ? TenantFeatureEntitlementEvaluation.Allowed(featureCode, canonical, false, true, false)
                    : TenantFeatureEntitlementEvaluation.Denied(
                        TenantFeatureEntitlementDecision.Disabled,
                        featureCode,
                        canonical,
                        false,
                        true,
                        false,
                        "disabled"));
            }

            return Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(featureCode, canonical, false, true, false));
        }

        public Task<bool> IsEnabledAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            EvaluateAsync(tenantId, featureCode, now, cancellationToken).ContinueWith(t => t.Result.IsAllowed, cancellationToken);
    }

    private sealed class DenyingTenantResourceLimitGuard : ITenantResourceLimitGuard
    {
        public Task<TenantResourceLimitEvaluation> EvaluateAsync(
            Guid tenantId,
            string limitKey,
            int requestedIncrease,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TenantResourceLimitEvaluation(
                limitKey,
                "outlets",
                3,
                requestedIncrease,
                3,
                0,
                false,
                false,
                false,
                SubscriptionLimitErrorCodes.LimitReached,
                "Outlets subscription limit reached."));

        public Task<TenantResourceLimitGuardResult<T>> ExecuteWithinCapacityAsync<T>(
            Guid tenantId,
            string limitKey,
            int requestedIncrease,
            Func<CancellationToken, Task<TenantResourceCapacityOperationResult<T>>> operation,
            CancellationToken cancellationToken) =>
            EvaluateAsync(tenantId, limitKey, requestedIncrease, cancellationToken)
                .ContinueWith(task => TenantResourceLimitGuardResult<T>.Denied(task.Result), cancellationToken);

        public Task<TenantResourceCapacitySnapshot> GetCapacitySnapshotAsync(
            Guid tenantId,
            string limitKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TenantResourceCapacitySnapshot(limitKey, "outlets", 3, 3, 0, false, false, false));
    }
}
