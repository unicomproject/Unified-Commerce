using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantEntitlementOptionsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 6, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid PlanId = Guid.Parse("77777777-7777-4777-8777-777777777711");
    private static readonly Guid FeatureId = Guid.Parse("88888888-8888-4888-8888-888888888801");

    [Fact]
    public async Task GetEntitlementOptionsAsync_WithPermission_ReturnsOptions()
    {
        var options = CreateOptions();
        var service = CreateService(
            new FakeEntitlementTenantRepository
            {
                TenantEntity = CreateTenant(),
                SubscriptionEntity = CreateSubscription(),
                EntitlementOptionsResponse = options
            },
            permissions: AllTenantPermissions());

        var result = await service.GetEntitlementOptionsAsync(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, result.Value!.TenantId);
        Assert.Equal(PlanId, result.Value.CurrentSubscriptionPlanId);
        Assert.Equal("STARTER", result.Value.CurrentSubscriptionPlanCode);
        Assert.Single(result.Value.EnabledFeatureIds);
        Assert.Equal(FeatureId, result.Value.EnabledFeatureIds[0]);
        Assert.Equal("online_store", result.Value.EnabledFeatureCodes[0]);
        Assert.Single(result.Value.Plans);
        Assert.NotEmpty(result.Value.Plans[0].IncludedFeatureIds);
        Assert.Single(result.Value.CatalogModules);
        Assert.NotEmpty(result.Value.CatalogModules[0].Features);
    }

    [Fact]
    public async Task GetEntitlementOptionsAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakeEntitlementTenantRepository
            {
                TenantEntity = CreateTenant(),
                SubscriptionEntity = CreateSubscription(),
                EntitlementOptionsResponse = CreateOptions()
            },
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsView });

        var result = await service.GetEntitlementOptionsAsync(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetEntitlementOptionsAsync_WhenTenantMissing_ReturnsNotFound()
    {
        var service = CreateService(
            new FakeEntitlementTenantRepository
            {
                TenantEntity = null,
                EntitlementOptionsResponse = CreateOptions()
            },
            permissions: AllTenantPermissions());

        var result = await service.GetEntitlementOptionsAsync(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetTenantDetailAsync_ReturnsEnabledFeatureIdsAndCodes()
    {
        var featureId = FeatureId;
        var service = CreateService(
            new FakeEntitlementTenantRepository
            {
                DetailResponse = CreateDetailWithFeatures(featureId, "online_store")
            },
            permissions: AllTenantPermissions());

        var result = await service.GetTenantDetailAsync(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([featureId], result.Value!.EnabledFeatureIds);
        Assert.Equal(["online_store"], result.Value.EnabledFeatureCodes);
        Assert.True(result.Value.OnlineStoreEnabled);
    }

    private static PlatformTenantService CreateService(
        IPlatformTenantRepository repository,
        IReadOnlySet<string> permissions)
    {
        return new PlatformTenantService(
            repository,
            new FakePlatformSubscriptionPlanRepository(),
            new FakePlatformPermissionChecker(permissions),
            new FakePlatformPermissionRepository(permissions),
            new FakeDateTimeProvider(),
            new FakePasswordHashService());
    }

    private static HashSet<string> AllTenantPermissions() =>
        PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

    private static Tenant CreateTenant() =>
        Tenant.Create(
            TenantId,
            "TEN-001",
            "ten-001",
            "Tenant One",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now);

    private static TenantSubscription CreateSubscription() =>
        TenantSubscription.Create(
            Guid.Parse("22222222-2222-4222-8222-222222222201"),
            TenantId,
            PlanId,
            "ACTIVE",
            Now);

    private static PlatformTenantEntitlementOptionsResponse CreateOptions()
    {
        return new PlatformTenantEntitlementOptionsResponse(
            TenantId,
            PlanId,
            "STARTER",
            "Starter Plan",
            [FeatureId],
            ["online_store"],
            [
                new PlatformTenantEntitlementPlanOptionDto(
                    PlanId,
                    "STARTER",
                    "Starter Plan",
                    "active",
                    [FeatureId],
                    ["online_store"])
            ],
            [
                new PlatformTenantEntitlementCatalogModuleDto(
                    Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                    "commerce",
                    "Commerce",
                    [
                        new PlatformTenantEntitlementCatalogFeatureDto(
                            FeatureId,
                            "online_store",
                            "Online Store",
                            "Online store entitlement")
                    ])
            ]);
    }

    private static PlatformTenantDetailResponse CreateDetailWithFeatures(Guid featureId, string featureCode)
    {
        return new PlatformTenantDetailResponse(
            TenantId,
            "TEN-001",
            "Tenant One",
            "active",
            "paid",
            "unified_epos",
            "LKR",
            "Asia/Colombo",
            "en-LK",
            "Retail",
            null,
            null,
            new PlatformTenantDetailSubscriptionDto(PlanId, "Starter Plan", "ACTIVE", null, null, null),
            1,
            1,
            1,
            true,
            false,
            false,
            [featureId],
            [featureCode],
            Now,
            Now,
            Now,
            false,
            false,
            false,
            false);
    }

    private sealed class FakeEntitlementTenantRepository : IPlatformTenantRepository
    {
        public Tenant? TenantEntity { get; init; }
        public TenantSubscription? SubscriptionEntity { get; init; }
        public PlatformTenantDetailResponse? DetailResponse { get; init; }
        public PlatformTenantEntitlementOptionsResponse? EntitlementOptionsResponse { get; init; }

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
            Task.FromResult(EntitlementOptionsResponse);

        public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(TenantEntity?.Id == tenantId ? TenantEntity : null);

        public Task AddTenantWithSubscriptionAndEntitlementsAsync(
            Tenant tenant,
            TenantSubscription subscription,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SubscriptionEntity?.TenantId == tenantId ? SubscriptionEntity : null);

        public Task UpdateTenantSubscriptionAsync(
            TenantSubscription subscription,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReplaceTenantEntitlementsAsync(
            Guid tenantId,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            string? revokedReason,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
            IReadOnlyList<Guid>? featureIds,
            IReadOnlyList<string>? featureCodes,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ResolvedTenantFeature>>([]);

        public Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformTenantCreateOptionsResponse([], [], [], [], [], [], [], [], [], [], [], [], []));

        public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Guid>> GetTenantAdminBootstrapPermissionIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class FakePlatformSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
    {
        public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken) =>
            Task.FromResult<SubscriptionPlan?>(null);

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
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReplacePlanFeaturesAsync(
            Guid planId,
            IReadOnlyList<Guid> featureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(
            IReadOnlyCollection<Guid> featureIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task UpsertLegacyPlanLimitsAsync(
            Guid planId,
            int? maxOutlets,
            int? maxUsers,
            int? maxTills,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, decimal?>>(new Dictionary<string, decimal?>());
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly IReadOnlySet<string> _permissions;

        public FakePlatformPermissionChecker(IReadOnlySet<string> permissions)
        {
            _permissions = permissions;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(_permissions.Contains(permissionCode));
    }

    private sealed class FakePlatformPermissionRepository : IPlatformPermissionRepository
    {
        private readonly IReadOnlySet<string> _permissions;

        public FakePlatformPermissionRepository(IReadOnlySet<string> permissions)
        {
            _permissions = permissions;
        }

        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_permissions);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => password;

        public bool VerifyPassword(string password, string passwordHash) => password == passwordHash;
    }
}


