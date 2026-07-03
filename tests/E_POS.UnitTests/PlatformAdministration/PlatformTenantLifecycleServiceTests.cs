using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Application.Modules.SubscriptionBilling.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Constants;
using E_POS.Domain.Modules.TenantFoundation.Entities;
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
            DetailResponse = CreateDetail(Guid.NewGuid(), TenantStatusConstants.Draft),
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
        Assert.Equal(TenantStatusConstants.Draft, result.Value!.Status);
        Assert.True(repository.AddCalled);
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
            TenantEntity = Tenant.CreateDraft(
                tenantId,
                "TEN-UPDATE",
                "Old Name",
                TenantBillingStatusConstants.Pending,
                "LKR",
                "Asia/Colombo",
                "en-LK",
                "unified_epos",
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
        Assert.Equal("Updated Name", repository.TenantEntity!.Name);
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
            TenantEntity = Tenant.CreateDraft(
                tenantId,
                "TEN-ACTIVATE",
                "Activate Tenant",
                TenantBillingStatusConstants.Pending,
                "LKR",
                "Asia/Colombo",
                "en-LK",
                "unified_epos",
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
    public async Task ActivateTenantAsync_FromActive_ReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-ACTIVE",
                "Active Tenant",
                TenantStatusConstants.Active,
                TenantBillingStatusConstants.Paid,
                Now),
            SubscriptionEntity = TenantSubscription.Create(
                Guid.NewGuid(),
                tenantId,
                PlanId,
                TenantSubscriptionStatusConstants.Active,
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
    public async Task SuspendTenantAsync_FromActive_ReturnsSuspendedTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeLifecycleTenantRepository
        {
            TenantEntity = Tenant.Create(
                tenantId,
                "TEN-SUSPEND",
                "Suspend Tenant",
                TenantStatusConstants.Active,
                TenantBillingStatusConstants.Paid,
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
            TenantEntity = Tenant.CreateDraft(
                tenantId,
                "TEN-DRAFT",
                "Draft Tenant",
                TenantBillingStatusConstants.Pending,
                "LKR",
                "Asia/Colombo",
                "en-LK",
                "unified_epos",
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
                "Entitlement Tenant",
                TenantStatusConstants.Active,
                TenantBillingStatusConstants.Paid,
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
            new FakeLifecyclePasswordHashService());
    }

    private static HashSet<string> AllTenantPermissions() =>
        PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ViewOnlyPermissions() =>
        [PlatformPermissionCodes.TenantsView];

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
            status == TenantStatusConstants.Draft,
            status == TenantStatusConstants.Active,
            true);
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

        public Task<IReadOnlyList<Guid>> GetTenantAdminBootstrapPermissionIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
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

        public Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(
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
    }
}
