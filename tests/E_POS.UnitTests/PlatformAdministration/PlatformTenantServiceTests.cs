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

public sealed class PlatformTenantServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTenantsAsync_WithPermission_ReturnsTenantList()
    {
        var list = CreateListResponse();
        var service = CreateService(
            new FakePlatformTenantRepository { ListResponse = list },
            hasViewPermission: true);

        var result = await service.GetTenantsAsync(new PlatformTenantListQuery(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task GetSummaryAsync_WithPermission_ReturnsSummary()
    {
        var service = CreateService(
            new FakePlatformTenantRepository { SummaryResponse = CreateSummary() },
            hasViewPermission: true);

        var result = await service.GetSummaryAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalTenants);
    }

    [Fact]
    public async Task GetFilterOptionsAsync_WithPermission_ReturnsFilterOptions()
    {
        var service = CreateService(
            new FakePlatformTenantRepository
            {
                FilterOptionsResponse = new PlatformTenantFilterOptionsResponse(
                    ["active"],
                    ["paid"],
                    ["unified_epos"],
                    [new PlatformTenantFilterOptionPlanDto(Guid.NewGuid(), "Starter", "STARTER")])
            },
            hasViewPermission: true);

        var result = await service.GetFilterOptionsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Plans);
    }

    [Fact]
    public async Task GetTenantsAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformTenantRepository { ListResponse = CreateListResponse() },
            hasViewPermission: false);

        var result = await service.GetTenantsAsync(new PlatformTenantListQuery(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetTenantsAsync_NormalizesPaginationAndSortDirection()
    {
        var repository = new FakePlatformTenantRepository { ListResponse = CreateListResponse() };
        var service = CreateService(repository, hasViewPermission: true);

        await service.GetTenantsAsync(new PlatformTenantListQuery
        {
            PageNumber = 0,
            PageSize = 500,
            SortDirection = "DESC"
        }, Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(repository.LastQuery);
        Assert.Equal(1, repository.LastQuery!.PageNumber);
        Assert.Equal(100, repository.LastQuery.PageSize);
        Assert.Equal("desc", repository.LastQuery.SortDirection);
    }

    [Fact]
    public async Task GetTenantDetailAsync_WithPermission_ReturnsDetailWithActionFlags()
    {
        var tenantId = Guid.NewGuid();
        var service = CreateService(
            new FakePlatformTenantRepository
            {
                DetailResponse = CreateDetail(tenantId)
            },
            hasViewPermission: true,
            permissions: PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal));

        var result = await service.GetTenantDetailAsync(tenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, result.Value!.Id);
        Assert.True(result.Value.CanUpdate);
    }

    [Fact]
    public async Task GetTenantDetailAsync_WithoutViewPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformTenantRepository { DetailResponse = CreateDetail(Guid.NewGuid()) },
            hasViewPermission: false);

        var result = await service.GetTenantDetailAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetTenantDetailAsync_WhenTenantMissing_ReturnsNotFound()
    {
        var service = CreateService(
            new FakePlatformTenantRepository { DetailResponse = null },
            hasViewPermission: true);

        var result = await service.GetTenantDetailAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.not_found", result.Error.Code);
    }

    private static PlatformTenantService CreateService(
        IPlatformTenantRepository repository,
        bool hasViewPermission,
        IReadOnlySet<string>? permissions = null,
        FakePlatformSubscriptionPlanRepository? subscriptionPlanRepository = null)
    {
        return new PlatformTenantService(
            repository,
            subscriptionPlanRepository ?? new FakePlatformSubscriptionPlanRepository(),
            new FakePlatformPermissionChecker(
                permissions ?? (hasViewPermission
                    ? new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsView }
                    : new HashSet<string>(StringComparer.Ordinal))),
            new FakePlatformPermissionRepository(permissions ?? new HashSet<string>(StringComparer.Ordinal)),
            new FakeDateTimeProvider(),
            new FakePasswordHashService());
    }

    private static PlatformTenantListResponse CreateListResponse()
    {
        return new PlatformTenantListResponse(
            [
                new PlatformTenantListItemDto(
                    Guid.NewGuid(),
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
                    1,
                    1,
                    2,
                    false,
                    false,
                    false,
                    Now,
                    Now)
            ],
            1,
            10,
            1,
            1);
    }

    private static PlatformTenantSummaryResponse CreateSummary()
    {
        return new PlatformTenantSummaryResponse(2, 1, 1, 0, 0, 0, 1, 1);
    }

    private static PlatformTenantDetailResponse CreateDetail(Guid tenantId)
    {
        return new PlatformTenantDetailResponse(
            tenantId,
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
            null,
            1,
            1,
            1,
            false,
            false,
            false,
            Now,
            Now,
            Now,
            false,
            false,
            false,
            false);
    }

    private sealed class FakePlatformTenantRepository : IPlatformTenantRepository
    {
        public PlatformTenantListQuery? LastQuery { get; private set; }

        public PlatformTenantListResponse ListResponse { get; init; } = CreateListResponse();

        public PlatformTenantSummaryResponse SummaryResponse { get; init; } = CreateSummary();

        public PlatformTenantFilterOptionsResponse FilterOptionsResponse { get; init; } =
            new([], [], [], []);

        public PlatformTenantDetailResponse? DetailResponse { get; init; }

        public Task<PlatformTenantListResponse> GetTenantsAsync(
            PlatformTenantListQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(ListResponse);
        }

        public Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SummaryResponse);

        public Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(FilterOptionsResponse);

        public Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DetailResponse);

        public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<Tenant?>(null);

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
            Task.FromResult<TenantSubscription?>(null);

        public Task UpdateTenantSubscriptionAsync(
            TenantSubscription subscription,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReplaceTenantEntitlementsAsync(
            Guid tenantId,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
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
            Task.FromResult(new PlatformTenantCreateOptionsResponse([], [], [], [], [], [], [], [], [], [], []));

        public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Guid>> GetTenantAdminBootstrapPermissionIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
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
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_permissions.Contains(permissionCode));
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
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
