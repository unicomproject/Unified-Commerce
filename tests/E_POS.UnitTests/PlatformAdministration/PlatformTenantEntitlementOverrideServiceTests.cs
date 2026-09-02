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
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantEntitlementOverrideServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 6, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid PlanId = Guid.Parse("77777777-7777-4777-8777-777777777711");
    private static readonly Guid FeatureId = Guid.Parse("88888888-8888-4888-8888-888888888801");

    [Fact]
    public async Task UpdateEntitlementsAsync_WithValidOverride_Succeeds()
    {
        var repo = new FakeOverrideTenantRepository
        {
            TenantEntity = CreateTenant(),
            SubscriptionEntity = CreateSubscription(),
            DetailResponse = CreateDetail()
        };
        var service = CreateService(repo, AllTenantPermissions());

        var request = new UpdatePlatformTenantEntitlementsRequest
        {
            SubscriptionPlanId = PlanId,
            EnabledFeatureIds = [FeatureId],
            SourceType = "OVERRIDE",
            OverrideReason = "Special promotional grant for Q3."
        };

        var result = await service.UpdateEntitlementsAsync(TenantId, request, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.ReplaceEntitlementsCalled);
        Assert.Equal("OVERRIDE", repo.LastSourceType);
        Assert.Equal("Special promotional grant for Q3.", repo.LastOverrideReason);
    }

    [Fact]
    public async Task UpdateEntitlementsAsync_WithOverrideAndMissingReason_ReturnsValidationFailed()
    {
        var repo = new FakeOverrideTenantRepository
        {
            TenantEntity = CreateTenant(),
            SubscriptionEntity = CreateSubscription()
        };
        var service = CreateService(repo, AllTenantPermissions());

        var request = new UpdatePlatformTenantEntitlementsRequest
        {
            SubscriptionPlanId = PlanId,
            EnabledFeatureIds = [FeatureId],
            SourceType = "OVERRIDE",
            OverrideReason = "   "
        };

        var result = await service.UpdateEntitlementsAsync(TenantId, request, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.Contains("OverrideReason is required", result.Error.Message);
        Assert.False(repo.ReplaceEntitlementsCalled);
    }

    [Fact]
    public async Task UpdateEntitlementsAsync_WithInvalidSourceType_ReturnsValidationFailed()
    {
        var repo = new FakeOverrideTenantRepository
        {
            TenantEntity = CreateTenant(),
            SubscriptionEntity = CreateSubscription()
        };
        var service = CreateService(repo, AllTenantPermissions());

        var request = new UpdatePlatformTenantEntitlementsRequest
        {
            SubscriptionPlanId = PlanId,
            EnabledFeatureIds = [FeatureId],
            SourceType = "INVALID_TYPE",
            OverrideReason = "Test"
        };

        var result = await service.UpdateEntitlementsAsync(TenantId, request, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.Contains("SourceType must be OVERRIDE or MANUAL", result.Error.Message);
        Assert.False(repo.ReplaceEntitlementsCalled);
    }

    [Fact]
    public async Task UpdateEntitlementsAsync_WithInvalidEffectiveDates_ReturnsValidationFailed()
    {
        var repo = new FakeOverrideTenantRepository
        {
            TenantEntity = CreateTenant(),
            SubscriptionEntity = CreateSubscription()
        };
        var service = CreateService(repo, AllTenantPermissions());

        var request = new UpdatePlatformTenantEntitlementsRequest
        {
            SubscriptionPlanId = PlanId,
            EnabledFeatureIds = [FeatureId],
            SourceType = "OVERRIDE",
            OverrideReason = "Valid reason",
            EffectiveFrom = Now,
            EffectiveUntil = Now.AddDays(-1)
        };

        var result = await service.UpdateEntitlementsAsync(TenantId, request, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.Contains("EffectiveUntil must be greater than EffectiveFrom", result.Error.Message);
        Assert.False(repo.ReplaceEntitlementsCalled);
    }

    [Fact]
    public async Task RestoreEntitlementsToPlanAsync_WithValidSubscription_Succeeds()
    {
        var repo = new FakeOverrideTenantRepository
        {
            TenantEntity = CreateTenant(),
            SubscriptionEntity = CreateSubscription(),
            DetailResponse = CreateDetail()
        };
        var service = CreateService(repo, AllTenantPermissions());

        var result = await service.RestoreEntitlementsToPlanAsync(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.RestorePlanCalled);
        Assert.Equal(PlanId, repo.RestoredPlanId);
    }

    [Fact]
    public async Task RestoreEntitlementsToPlanAsync_WithoutSubscription_ReturnsValidationFailed()
    {
        var repo = new FakeOverrideTenantRepository
        {
            TenantEntity = CreateTenant(),
            SubscriptionEntity = null
        };
        var service = CreateService(repo, AllTenantPermissions());

        var result = await service.RestoreEntitlementsToPlanAsync(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.Contains("Cannot restore plan baseline without a valid subscription", result.Error.Message);
        Assert.False(repo.RestorePlanCalled);
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
            new FakePasswordHashService(),
            new FakeTenantUsageCounterService(),
            new PassingDefaultTenantSettingsProvider());
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

    private static PlatformTenantDetailResponse CreateDetail()
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
            [FeatureId],
            ["online_store"],
            Now,
            Now,
            Now,
            false,
            false,
            false,
            false);
    }

    private sealed class FakeOverrideTenantRepository : IPlatformTenantRepository
    {
        public Tenant? TenantEntity { get; init; }
        public TenantSubscription? SubscriptionEntity { get; init; }
        public PlatformTenantDetailResponse? DetailResponse { get; init; }
        public bool ReplaceEntitlementsCalled { get; private set; }
        public string? LastSourceType { get; private set; }
        public string? LastOverrideReason { get; private set; }
        public bool RestorePlanCalled { get; private set; }
        public Guid? RestoredPlanId { get; private set; }

        public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(TenantEntity?.Id == tenantId ? TenantEntity : null);

        public Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(SubscriptionEntity?.TenantId == tenantId ? SubscriptionEntity : null);

        public Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(DetailResponse);

        public Task ReplaceTenantEntitlementsAsync(
            Guid tenantId,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            string? revokedReason,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReplaceTenantEntitlementsAsync(
            Guid tenantId,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            string? revokedReason,
            string sourceType,
            string? overrideReason,
            DateTimeOffset? effectiveFrom,
            DateTimeOffset? effectiveUntil,
            CancellationToken cancellationToken)
        {
            ReplaceEntitlementsCalled = true;
            LastSourceType = sourceType;
            LastOverrideReason = overrideReason;
            return Task.CompletedTask;
        }

        public Task RestoreTenantPlanEntitlementsAsync(
            Guid tenantId,
            Guid subscriptionPlanId,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            CancellationToken cancellationToken)
        {
            RestorePlanCalled = true;
            RestoredPlanId = subscriptionPlanId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
            IReadOnlyList<Guid>? featureIds,
            IReadOnlyList<string>? featureCodes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ResolvedTenantFeature>>([
                new ResolvedTenantFeature(FeatureId, "online_store")
            ]);
        }

        public Task AddAuditLogAsync(
            Guid tenantId,
            Guid? platformUserId,
            string action,
            string summary,
            string? reason,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddAuditLogAsync(
            Guid tenantId,
            Guid? platformUserId,
            string action,
            string summary,
            string? reason,
            DateTimeOffset now,
            string? entityName,
            Guid? entityId,
            object? oldValues,
            object? newValues,
            string? userIpAddress,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Guid?> GetActiveBusinessTypeIdByCodeAsync(string businessCode, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
        public Task<TenantProfile?> GetTenantProfileEntityByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<TenantProfile?>(null);
        public Task UpsertTenantProfileAsync(TenantProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> HasVerifiedPaidInvoiceAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<PlatformTenantActivationRuntimeResult> ActivateTenantRuntimeAsync(Guid tenantId, Guid actorPlatformUserId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(new PlatformTenantActivationRuntimeResult(PlatformTenantActivationRuntimeOutcome.Success));
        public Task<PlatformTenantListResponse> GetTenantsAsync(PlatformTenantListQuery query, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PlatformTenantEntitlementOptionsResponse?> GetEntitlementOptionsAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task AddTenantWithSubscriptionAndEntitlementsAsync(Tenant tenant, TenantSubscription subscription, IReadOnlyList<Guid> enabledFeatureIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateTenantSubscriptionAsync(TenantSubscription subscription, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(Guid planId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PlatformTenantAuditLogListResponse> GetTenantAuditLogsAsync(Guid tenantId, int pageNumber, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakePlatformPermissionRepository : IPlatformPermissionRepository
    {
        private readonly IReadOnlySet<string> _permissions;
        public FakePlatformPermissionRepository(IReadOnlySet<string> permissions) => _permissions = permissions;
        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken) => Task.FromResult(_permissions);
        public Task<IReadOnlySet<string>> GetUserPermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken) => Task.FromResult(_permissions);
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly IReadOnlySet<string> _permissions;
        public FakePlatformPermissionChecker(IReadOnlySet<string> permissions) => _permissions = permissions;
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken) => Task.FromResult(_permissions.Contains(permissionCode));
        public Task<IReadOnlySet<string>> GetPermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken) => Task.FromResult(_permissions);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => $"HASHED:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"HASHED:{password}";
    }

    private sealed class FakePlatformSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
    {
        public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken) => Task.FromResult<SubscriptionPlan?>(SubscriptionPlan.Create(PlanId, "STARTER", "Starter Plan", "ACTIVE", "MONTHLY", 0m, Now));
        public Task<SubscriptionPlanListResponse> GetPlansAsync(SubscriptionPlanListQuery query, SubscriptionPlanPermissionFlags permissionFlags, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> PlanCodeExistsAsync(string planCode, Guid excludingPlanId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(Guid planId, SubscriptionPlanPermissionFlags permissionFlags, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SubscriptionPlanDetailResponse?> GetPlanDetailByIdAsync(Guid planId, SubscriptionPlanPermissionFlags permissionFlags, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task ReplacePlanFeaturesAsync(Guid planId, IReadOnlyList<Guid> featureIds, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(IReadOnlyCollection<Guid> featureIds, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task UpsertLegacyPlanLimitsAsync(Guid planId, int? maxOutlets, int? maxUsers, int? maxTills, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(Guid planId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> CountPlanAssignmentsAsync(Guid planId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string?> GetPlanCodeByIdAsync(Guid planId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task RemovePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task CopyPlanConfigurationAsync(Guid sourcePlanId, Guid targetPlanId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
