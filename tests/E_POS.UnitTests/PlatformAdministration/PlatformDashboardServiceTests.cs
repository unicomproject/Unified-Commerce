using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformDashboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetDashboardAsync_WithDashboardPermission_ReturnsSectionedDashboard()
    {
        var service = CreateService(
            new FakePlatformDashboardRepository(CreateSnapshot()),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value!.TenantSummary.Status);
        Assert.Equal(3, result.Value.TotalTenants);
        Assert.Equal(2, result.Value.ActiveTenants);
        Assert.NotNull(result.Value.RevenueSummary);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.RevenueSummary!.Status);
        Assert.Single(result.Value.RevenueSummary.Data!.MrrByCurrency);
        Assert.Equal(1000m, result.Value.RevenueSummary.Data.MrrByCurrency[0].Amount);
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutDashboardPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformDashboardRepository(CreateSnapshot()),
            new FakePlatformPermissionChecker(allGranted: false),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_dashboard.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutSubscriptionPermission_OmitsSubscriptionMetrics()
    {
        var service = CreateService(
            new FakePlatformDashboardRepository(CreateSnapshot()),
            new FakePlatformPermissionChecker(granted:
            [
                PlatformPermissionCodes.DashboardView
            ]),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.PermissionDenied, result.Value!.SubscriptionSummary!.Status);
        Assert.Null(result.Value.TotalSubscriptions);
        Assert.Null(result.Value.TrialTenants);
        Assert.Null(result.Value.ActiveSubscriptions);
        Assert.DoesNotContain(result.Value.AttentionItems!, x => x.Type == "past_due_subscriptions");
    }

    [Fact]
    public async Task GetDashboardAsync_MissingCurrencyMetadata_MarksRevenueUnavailable()
    {
        var snapshot = CreateSnapshot() with
        {
            Currencies = new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>(StringComparer.OrdinalIgnoreCase)
        };
        var service = CreateService(
            new FakePlatformDashboardRepository(snapshot),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.Unavailable, result.Value!.RevenueSummary!.Status);
        Assert.Equal(PlatformDashboardErrorCodes.CurrencyMetadataUnavailable, result.Value.RevenueSummary.ErrorCode);
        Assert.Null(result.Value.RevenueSummary.Data);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.TenantSummary.Status);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.SystemHealth!.Status);
        Assert.NotEqual(default, result.Value.GeneratedAt);
    }

    [Fact]
    public async Task GetDashboardAsync_InvalidTimezone_MarksTrendsUnavailable()
    {
        var snapshot = CreateSnapshot() with { PlatformTimezone = "Not/A/Real/Timezone" };
        var service = CreateService(
            new FakePlatformDashboardRepository(snapshot),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.Unavailable, result.Value!.Trends!.Status);
        Assert.Equal(PlatformDashboardErrorCodes.TimezoneUnavailable, result.Value.Trends.ErrorCode);
        Assert.Null(result.Value.Trends.Data);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.TenantSummary.Status);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.RevenueSummary!.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_RevenueAndTrendsUnavailable_PreservesOtherSections()
    {
        var snapshot = CreateSnapshot() with
        {
            PlatformTimezone = null,
            Currencies = new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>(StringComparer.OrdinalIgnoreCase)
        };
        var service = CreateService(
            new FakePlatformDashboardRepository(snapshot),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.Unavailable, result.Value!.RevenueSummary!.Status);
        Assert.Equal(PlatformDashboardSectionStatuses.Unavailable, result.Value.Trends!.Status);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.TenantSummary.Status);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.SystemHealth!.Status);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.RecentTenants!.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_CriticalHealthDependency_KeepsHealthSectionSuccessWithCriticalOverall()
    {
        var service = CreateService(
            new FakePlatformDashboardRepository(CreateSnapshot()),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe(
                overall: "CRITICAL",
                dependencies:
                [
                    new("core_api", "HEALTHY", true, null),
                    new("payment", "DEGRADED", true, "Payment provider is not configured.")
                ]));

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value!.SystemHealth!.Status);
        Assert.Equal("CRITICAL", result.Value.SystemHealth.Data!.OverallStatus);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.TenantSummary.Status);
        Assert.DoesNotContain("Exception", result.Value.SystemHealth.Data.Dependencies[1].Message ?? string.Empty);
    }

    [Fact]
    public async Task GetDashboardAsync_HealthProbeThrows_MarksHealthUnavailable()
    {
        var service = CreateService(
            new FakePlatformDashboardRepository(CreateSnapshot()),
            new FakePlatformPermissionChecker(allGranted: true),
            new ThrowingHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformDashboardSectionStatuses.Unavailable, result.Value!.SystemHealth!.Status);
        Assert.Equal(PlatformDashboardErrorCodes.HealthProbeFailed, result.Value.SystemHealth.ErrorCode);
        Assert.Null(result.Value.SystemHealth.Data);
        Assert.Equal(PlatformDashboardSectionStatuses.Success, result.Value.TenantSummary.Status);
    }

    [Fact]
    public async Task GetDashboardAsync_SetupPendingIncludesDraftAndPendingActivation()
    {
        var tenants = new List<PlatformDashboardTenantRow>
        {
            new(Guid.NewGuid(), "T1", "Draft", "draft", Now),
            new(Guid.NewGuid(), "T2", "Pending Act", "pending_activation", Now),
            new(Guid.NewGuid(), "T3", "Active", "active", Now)
        };
        var snapshot = CreateSnapshot() with { Tenants = tenants, TenantCreatedEvents = tenants.Select(t => (t.CreatedAt, t.Id)).ToList() };
        var service = CreateService(
            new FakePlatformDashboardRepository(snapshot),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(2, result.Value!.SetupPendingTenants);
        Assert.Equal(2, result.Value.TenantSummary.Data!.SetupPendingTenants);
    }

    [Fact]
    public async Task GetDashboardAsync_TenantLifecycleBucketsAlignWithApprovedContract()
    {
        var tenants = new List<PlatformDashboardTenantRow>
        {
            new(Guid.NewGuid(), "T1", "Draft", "draft", Now),
            new(Guid.NewGuid(), "T2", "Pending Payment", "pending_payment", Now),
            new(Guid.NewGuid(), "T3", "Pending Act", "pending_activation", Now),
            new(Guid.NewGuid(), "T4", "Setup Pending", "setup_pending", Now),
            new(Guid.NewGuid(), "T5", "Active", "active", Now),
            new(Guid.NewGuid(), "T6", "Suspended", "suspended", Now),
            new(Guid.NewGuid(), "T7", "Inactive", "inactive", Now),
            new(Guid.NewGuid(), "T8", "Cancelled", "cancelled", Now)
        };
        var snapshot = CreateSnapshot() with { Tenants = tenants, TenantCreatedEvents = tenants.Select(t => (t.CreatedAt, t.Id)).ToList() };
        var service = CreateService(
            new FakePlatformDashboardRepository(snapshot),
            new FakePlatformPermissionChecker(allGranted: true),
            new FakeHealthProbe());

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var data = result.Value!.TenantSummary.Data!;
        Assert.Equal(4, data.SetupPendingTenants);
        Assert.Equal(1, data.ActiveTenants);
        Assert.Equal(1, data.SuspendedTenants);
        Assert.Equal(1, data.InactiveTenants);
    }

    private static PlatformDashboardService CreateService(
        IPlatformDashboardRepository repository,
        IPlatformPermissionChecker permissionChecker,
        IPlatformDashboardHealthProbe healthProbe)
    {
        return new PlatformDashboardService(
            repository,
            permissionChecker,
            healthProbe,
            new FakeDateTimeProvider());
    }

    private static PlatformDashboardComputationSnapshot CreateSnapshot()
    {
        var tenantOne = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
        var tenantTwo = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
        var tenantThree = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3");
        var activeSubId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");

        var tenants = new List<PlatformDashboardTenantRow>
        {
            new(tenantOne, "TEN-001", "Tenant One", "active", Now.AddDays(-3)),
            new(tenantTwo, "TEN-002", "Tenant Two", "suspended", Now.AddDays(-2)),
            new(tenantThree, "TEN-003", "Tenant Three", "active", Now.AddDays(-1))
        };

        var subscriptions = new List<PlatformDashboardSubscriptionRow>
        {
            new(activeSubId, tenantOne, "ACTIVE", "LKR", 1000m, "monthly", "MONTHLY", null, null, Now, Now),
            new(Guid.NewGuid(), tenantThree, "TRIAL", "LKR", 500m, "monthly", "MONTHLY", null, null, Now, Now),
            new(Guid.NewGuid(), tenantTwo, "PAST_DUE", "LKR", 800m, "monthly", "MONTHLY", null, null, Now, Now)
        };

        return new PlatformDashboardComputationSnapshot(
            Now,
            "Asia/Colombo",
            tenants,
            subscriptions,
            [],
            [],
            new Dictionary<string, PlatformDashboardMrrCalculator.CurrencyMetadata>(StringComparer.OrdinalIgnoreCase)
            {
                ["LKR"] = new("LKR", 2)
            },
            PendingBillingCount: 1,
            TotalOutlets: 4,
            TotalTills: 5,
            TotalTenantUsers: 6,
            TotalPlatformUsers: 2,
            TenantCreatedEvents: tenants.Select(t => (t.CreatedAt, t.Id)).ToList(),
            SubscriptionCreatedEvents: subscriptions.Select(s => (s.CreatedAt, s.Id)).ToList());
    }

    private sealed class FakePlatformDashboardRepository : IPlatformDashboardRepository
    {
        private readonly PlatformDashboardComputationSnapshot _snapshot;

        public FakePlatformDashboardRepository(PlatformDashboardComputationSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<PlatformDashboardComputationSnapshot> GetComputationSnapshotAsync(
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot with { GeneratedAt = generatedAt });
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly HashSet<string> _granted;

        public FakePlatformPermissionChecker(bool allGranted)
        {
            _granted = allGranted
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PlatformPermissionCodes.DashboardView,
                    PlatformPermissionCodes.TenantSubscriptionsView,
                    PlatformPermissionCodes.BillingView,
                    PlatformPermissionCodes.UsersView
                }
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public FakePlatformPermissionChecker(IEnumerable<string> granted)
        {
            _granted = new HashSet<string>(granted, StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(_granted.Contains(permissionCode));
    }

    private sealed class FakeHealthProbe : IPlatformDashboardHealthProbe
    {
        private readonly PlatformDashboardSystemHealthDto _health;

        public FakeHealthProbe(
            string overall = "HEALTHY",
            IReadOnlyList<PlatformDashboardHealthDependencyDto>? dependencies = null)
        {
            _health = new PlatformDashboardSystemHealthDto(
                overall,
                Now,
                dependencies ?? [new PlatformDashboardHealthDependencyDto("core_api", "HEALTHY", true, null)]);
        }

        public Task<PlatformDashboardSystemHealthDto> ProbeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_health);
    }

    private sealed class ThrowingHealthProbe : IPlatformDashboardHealthProbe
    {
        public Task<PlatformDashboardSystemHealthDto> ProbeAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated probe failure with secret=abc");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
