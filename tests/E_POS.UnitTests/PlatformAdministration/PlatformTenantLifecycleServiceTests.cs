using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantLifecycleServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 19, 0, 0, TimeSpan.Zero);
    private static readonly Guid PlanId = Guid.Parse("77777777-7777-4777-8777-777777777799");
    private static readonly Guid FeatureId = Guid.Parse("88888888-8888-4888-8888-888888888899");

    [Fact]
    public async Task CreateTenantAsync_WithValidRequest_ReturnsCreatedTenant()
    {
        var repository = new FakeLifecycleTenantRepository
        {
            DetailResponse = CreateDetail(Guid.NewGuid(), TenantStatusConstants.Active),
            IncludedFeatureIds = new HashSet<Guid> { FeatureId },
            ResolvedFeatures = [new ResolvedTenantFeature(FeatureId, "online_store")]
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository
            {
                PlanEntity = CreateActivePlan()
            },
            permissions: AllTenantPermissions());

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-SLICE10",
                Name = "Slice 10 Tenant",
                SubscriptionPlanId = PlanId,
                EnabledFeatureCodes = ["online_store"]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatusConstants.Active, result.Value!.Status);
        Assert.True(repository.AddCalled);
        Assert.Equal(TenantStatusConstants.Active, repository.AddedTenant!.Status);
        Assert.NotNull(repository.AddedTenant.ActivatedAt);
    }

    [Fact]
    public async Task CreateTenantAsync_WithDuplicateCode_ReturnsConflict()
    {
        var service = CreateService(
            new FakeLifecycleTenantRepository { TenantCodeExists = true },
            new FakePlatformSubscriptionPlanRepository { PlanEntity = CreateActivePlan() },
            permissions: AllTenantPermissions());

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-DUP",
                Name = "Duplicate Tenant",
                SubscriptionPlanId = PlanId
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateTenantAsync_WithInvalidPlan_ReturnsValidationFailed()
    {
        var service = CreateService(
            new FakeLifecycleTenantRepository(),
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-INVALID",
                Name = "Invalid Plan Tenant",
                SubscriptionPlanId = PlanId
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateTenantAsync_WithInvalidFeature_ReturnsValidationFailed()
    {
        var service = CreateService(
            new FakeLifecycleTenantRepository(),
            new FakePlatformSubscriptionPlanRepository { PlanEntity = CreateActivePlan() },
            permissions: AllTenantPermissions());

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-BAD-FEATURE",
                Name = "Bad Feature Tenant",
                SubscriptionPlanId = PlanId,
                EnabledFeatureCodes = ["unknown_feature"]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateTenantAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakeLifecycleTenantRepository(),
            new FakePlatformSubscriptionPlanRepository(),
            permissions: ViewOnlyPermissions());

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-FORBIDDEN",
                Name = "Forbidden Tenant",
                SubscriptionPlanId = PlanId
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateTenantAsync_WithValidRequest_ReturnsUpdatedTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-UPDATE",
                "ten-update",
                "Old Name",
                TenantStatusConstants.Draft,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Draft)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.UpdateTenantAsync(
            tenantId,
            new UpdatePlatformTenantRequest { Name = "Updated Name" },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", repository.TenantEntity!.DisplayName);
    }

    [Fact]
    public async Task UpdateTenantAsync_NameOnly_DoesNotEraseLocaleOrOperatingMode()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-LOCALE-KEEP",
                "ten-locale-keep",
                "Locale Tenant",
                TenantStatusConstants.Draft,
                "GBP",
                "Europe/London",
                null,
                null,
                Now,
                "en-GB",
                TenantOperatingModeConstants.PosOnly),
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Draft)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.UpdateTenantAsync(
            tenantId,
            new UpdatePlatformTenantRequest { Name = "Renamed Locale Tenant" },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed Locale Tenant", repository.TenantEntity!.DisplayName);
        Assert.Equal("en-GB", repository.TenantEntity.DefaultLocale);
        Assert.Equal(TenantOperatingModeConstants.PosOnly, repository.TenantEntity.OperatingMode);
    }

    [Fact]
    public async Task UpdateTenantAsync_WhenMissing_ReturnsNotFound()
    {
        var service = CreateService(
            new FakeLifecycleTenantRepository(),
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.UpdateTenantAsync(
            Guid.NewGuid(),
            new UpdatePlatformTenantRequest { Name = "Updated Name" },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.not_found", result.Error.Code);
    }

    [Fact]
    public async Task ActivateTenantAsync_FromDraft_ReturnsActiveTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-ACTIVATE",
                "ten-activate",
                "Activate Tenant",
                TenantStatusConstants.Draft,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Trial,
                Now),
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Active)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.ActivateTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatusConstants.Active, repository.TenantEntity!.Status);
        Assert.Equal(TenantSubscriptionStatusConstants.Active, repository.SubscriptionEntity!.SubscriptionStatus);
    }

    [Fact]
    public async Task ActivateTenantAsync_FromActive_IsIdempotent()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-ACTIVE",
                "ten-active",
                "Active Tenant",
                TenantStatusConstants.Active,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
                Now),
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Active)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.ActivateTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatusConstants.Active, result.Value!.LifecycleStatus);
    }

    [Fact]
    public async Task SuspendTenantAsync_FromActive_ReturnsSuspendedTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-SUSPEND",
                "ten-suspend",
                "Suspend Tenant",
                TenantStatusConstants.Active,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
                Now),
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Suspended)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.SuspendTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatusConstants.Suspended, repository.TenantEntity!.Status);
    }

    [Fact]
    public async Task SuspendTenantAsync_FromDraft_ReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-DRAFT",
                "ten-draft",
                "Draft Tenant",
                TenantStatusConstants.Draft,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.SuspendTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.invalid_transition", result.Error.Code);
    }

    [Fact]
    public async Task UpdateEntitlementsAsync_WithValidFeatures_ReturnsUpdatedTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-ENT",
                "ten-ent",
                "Entitlement Tenant",
                TenantStatusConstants.Active,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
                Now),
            IncludedFeatureIds = new HashSet<Guid> { FeatureId },
            ResolvedFeatures = [new ResolvedTenantFeature(FeatureId, "online_store")],
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Active)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.UpdateEntitlementsAsync(
            tenantId,
            new UpdatePlatformTenantEntitlementsRequest { EnabledFeatureCodes = ["online_store"] },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.ReplaceEntitlementsCalled);
    }

    private static PlatformTenantService CreateService(
        FakeLifecycleTenantRepository repository,
        FakePlatformSubscriptionPlanRepository subscriptionPlanRepository,
        IReadOnlySet<string> permissions)
    {
        return new PlatformTenantService(
            repository,
            subscriptionPlanRepository,
            new FakeLifecyclePermissionChecker(permissions),
            new FakeLifecyclePermissionRepository(permissions),
            new FakeLifecycleDateTimeProvider(),
            new FakeLifecyclePasswordHashService(),
            new FakeTenantUsageCounterService(),
            new PassingDefaultTenantSettingsProvider());
    }

    private static HashSet<string> AllTenantPermissions() =>
        PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ViewOnlyPermissions() =>
        [PlatformPermissionCodes.TenantsView];

    [Fact]
    public async Task ActivateTenantAsync_FromPendingPayment_ReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-PAY",
                "ten-pay",
                "Paid Pending",
                TenantStatusConstants.PendingPayment,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
                Now),
            HasVerifiedPaidInvoice = true
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.ActivateTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.invalid_transition", result.Error.Code);
        Assert.Contains("payment verification", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActivateTenantAsync_FromPendingActivationWithoutPayment_ReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-PA-UNPAID",
                "ten-pa-unpaid",
                "Pending Activation",
                TenantStatusConstants.PendingActivation,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
                Now),
            HasVerifiedPaidInvoice = false
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.ActivateTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.invalid_transition", result.Error.Code);
    }

    [Fact]
    public async Task ActivateTenantAsync_FromPendingActivationWithPayment_ReturnsActive()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-PA-PAID",
                "ten-pa-paid",
                "Pending Activation Paid",
                TenantStatusConstants.PendingActivation,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
                Now),
            HasVerifiedPaidInvoice = true,
            DetailResponse = CreateDetail(tenantId, TenantStatusConstants.Active)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.ActivateTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatusConstants.Active, repository.TenantEntity!.Status);
        Assert.NotNull(repository.TenantEntity.ActivatedAt);
    }

    [Fact]
    public async Task ActivateTenantAsync_FromCancelled_ReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-CANCEL",
                "ten-cancel",
                "Cancelled Tenant",
                TenantStatusConstants.Cancelled,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Cancelled,
                Now)
        };

        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            permissions: AllTenantPermissions());

        var result = await service.ActivateTenantAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.invalid_transition", result.Error.Code);
    }

    [Fact]
    public async Task CreateTenantAsync_PaidWizardPath_DoesNotWriteBillingIntoStatus()
    {
        var repository = new FakeLifecycleTenantRepository
        {
            DetailResponse = CreateDetail(Guid.NewGuid(), TenantStatusConstants.PendingPayment),
            IncludedFeatureIds = new HashSet<Guid> { FeatureId },
            ResolvedFeatures = [new ResolvedTenantFeature(FeatureId, "online_store")]
        };

        // Paid path uses wizard; exercise via wizard-shaped request through CreateTenantAsync routing.
        var service = CreateService(
            repository,
            new FakePlatformSubscriptionPlanRepository { PlanEntity = CreateActivePlan() },
            permissions: AllTenantPermissions());

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-LEGACY-TRIAL",
                Name = "Legacy Trial",
                BillingStatus = TenantBillingStatusConstants.Paid,
                SubscriptionPlanId = PlanId,
                EnabledFeatureCodes = ["online_store"]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatusConstants.Active, repository.AddedTenant!.Status);
        Assert.NotEqual(TenantBillingStatusConstants.Paid, repository.AddedTenant.Status);
    }

    private static SubscriptionPlan CreateActivePlan() =>
        SubscriptionPlan.Create(
            PlanId,
            "STARTER",
            "Starter Plan",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            49.99m,
            Now);

    private static PlatformTenantDetailResponse CreateDetail(Guid tenantId, string status)
    {
        return new PlatformTenantDetailResponse(
            tenantId,
            "TEN-001",
            "Tenant One",
            status,
            TenantBillingStatusConstants.Pending,
            "unified_epos",
            "LKR",
            "Asia/Colombo",
            "en-LK",
            "Retail",
            null,
            null,
            new PlatformTenantDetailSubscriptionDto(
                PlanId,
                "Starter Plan",
                TenantSubscriptionStatusConstants.Trial,
                null,
                null,
                null),
            0,
            0,
            0,
            false,
            false,
            false,
            [],
            [],
            Now,
            Now,
            Now,
            true,
            status is TenantStatusConstants.Draft or TenantStatusConstants.PendingActivation,
            status == TenantStatusConstants.Active,
            true,
            LifecycleStatus: status);
    }

    private sealed class FakeLifecycleDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeLifecyclePermissionChecker : IPlatformPermissionChecker
    {
        private readonly IReadOnlySet<string> _permissions;

        public FakeLifecyclePermissionChecker(IReadOnlySet<string> permissions)
        {
            _permissions = permissions;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(_permissions.Contains(permissionCode));
    }

    private sealed class FakeLifecyclePermissionRepository : IPlatformPermissionRepository
    {
        private readonly IReadOnlySet<string> _permissions;

        public FakeLifecyclePermissionRepository(IReadOnlySet<string> permissions)
        {
            _permissions = permissions;
        }

        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_permissions);
    }

    private sealed class FakeLifecycleTenantRepository : IPlatformTenantRepository
    {
        public bool TenantCodeExists { get; init; }
        public bool AddCalled { get; private set; }
        public bool ReplaceEntitlementsCalled { get; private set; }
        public Tenant? TenantEntity { get; set; }
        public TenantSubscription? SubscriptionEntity { get; set; }
        public PlatformTenantDetailResponse? DetailResponse { get; init; }
        public IReadOnlySet<Guid> IncludedFeatureIds { get; init; } = new HashSet<Guid>();
        public IReadOnlyList<ResolvedTenantFeature> ResolvedFeatures { get; init; } = [];
        public Dictionary<string, Guid> BusinessTypeIdsByCode { get; } = new(StringComparer.OrdinalIgnoreCase);
        public TenantProfile? ProfileEntity { get; set; }
        public TenantProfile? UpsertedProfile { get; private set; }
        public bool HasVerifiedPaidInvoice { get; set; }
        public Tenant? AddedTenant { get; private set; }

        public Task<Guid?> GetActiveBusinessTypeIdByCodeAsync(string businessCode, CancellationToken cancellationToken)
        {
            if (BusinessTypeIdsByCode.TryGetValue(businessCode.Trim(), out var id))
            {
                return Task.FromResult<Guid?>(id);
            }

            return Task.FromResult<Guid?>(null);
        }

        public Task<TenantProfile?> GetTenantProfileEntityByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
            => Task.FromResult(ProfileEntity);

        public Task UpsertTenantProfileAsync(TenantProfile profile, CancellationToken cancellationToken)
        {
            UpsertedProfile = profile;
            ProfileEntity = profile;
            return Task.CompletedTask;
        }

        public Task<bool> HasVerifiedPaidInvoiceAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(HasVerifiedPaidInvoice);

        public Task<PlatformTenantActivationRuntimeResult> ActivateTenantRuntimeAsync(Guid tenantId, Guid actorPlatformUserId,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (TenantEntity is null || SubscriptionEntity is null)
                return Task.FromResult(new PlatformTenantActivationRuntimeResult(PlatformTenantActivationRuntimeOutcome.NotFound));
            TenantEntity.Activate(actorPlatformUserId, now);
            SubscriptionEntity.Activate(now);
            return Task.FromResult(new PlatformTenantActivationRuntimeResult(PlatformTenantActivationRuntimeOutcome.Success));
        }

        public Task<PlatformTenantListResponse> GetTenantsAsync(
            PlatformTenantListQuery query,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DetailResponse);

        public Task<PlatformTenantEntitlementOptionsResponse?> GetEntitlementOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PlatformTenantEntitlementOptionsResponse?>(null);

        public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken) =>
            Task.FromResult(TenantCodeExists);

        public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(TenantEntity?.Id == tenantId ? TenantEntity : null);

        public Task AddTenantWithSubscriptionAndEntitlementsAsync(
            Tenant tenant,
            TenantSubscription subscription,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            AddCalled = true;
            AddedTenant = tenant;
            TenantEntity = tenant;
            SubscriptionEntity = subscription;
            return Task.CompletedTask;
        }

        public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            TenantEntity = tenant;
            return Task.CompletedTask;
        }

        public Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SubscriptionEntity?.TenantId == tenantId ? SubscriptionEntity : null);

        public Task UpdateTenantSubscriptionAsync(
            TenantSubscription subscription,
            CancellationToken cancellationToken)
        {
            SubscriptionEntity = subscription;
            return Task.CompletedTask;
        }

        public Task ReplaceTenantEntitlementsAsync(
            Guid tenantId,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            string? revokedReason,
            CancellationToken cancellationToken)
        {
            ReplaceEntitlementsCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            Task.FromResult(IncludedFeatureIds);

        public Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
            IReadOnlyList<Guid>? featureIds,
            IReadOnlyList<string>? featureCodes,
            CancellationToken cancellationToken)
        {
            if (featureCodes?.Contains("unknown_feature", StringComparer.Ordinal) == true)
            {
                return Task.FromResult<IReadOnlyList<ResolvedTenantFeature>>([]);
            }

            return Task.FromResult(ResolvedFeatures);
        }

        public Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformTenantCreateOptionsResponse([], [], [], [], [], [], [], [], [], [], [], [], []));

        public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
            IReadOnlyList<string> permissionCodes,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, Guid>>(
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));

        public Task<PlatformTenantAuditLogListResponse> GetTenantAuditLogsAsync(
            Guid tenantId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformTenantAuditLogListResponse([], pageNumber, pageSize, 0, 0));

        public Task AddAuditLogAsync(
            Guid tenantId,
            Guid? platformUserId,
            string action,
            string summary,
            string? reason,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeLifecyclePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => $"HASHED:{password}";

        public bool VerifyPassword(string password, string passwordHash) =>
            passwordHash == $"HASHED:{password}";
    }

    private sealed class FakePlatformSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
    {
        public SubscriptionPlan? PlanEntity { get; init; }

        public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken) =>
            Task.FromResult(PlanEntity?.Id == planId ? PlanEntity : null);

        public Task<SubscriptionPlanListResponse> GetPlansAsync(
            SubscriptionPlanListQuery query,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> PlanCodeExistsAsync(string planCode, Guid excludingPlanId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(
            Guid planId,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<SubscriptionPlanDetailResponse?> GetPlanDetailByIdAsync(
            Guid planId,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task ReplacePlanFeaturesAsync(
            Guid planId,
            IReadOnlyList<Guid> featureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(
            IReadOnlyCollection<Guid> featureIds,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task UpsertLegacyPlanLimitsAsync(
            Guid planId,
            int? maxOutlets,
            int? maxUsers,
            int? maxTills,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int> CountPlanAssignmentsAsync(Guid planId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<string?> GetPlanCodeByIdAsync(Guid planId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task RemovePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task CopyPlanConfigurationAsync(Guid sourcePlanId, Guid targetPlanId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}


