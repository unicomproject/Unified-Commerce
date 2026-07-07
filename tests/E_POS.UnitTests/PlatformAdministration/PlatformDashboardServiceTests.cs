using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
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
    public async Task GetDashboardAsync_WithDashboardPermission_ReturnsDashboard()
    {
        var dashboard = CreateDashboard();
        var service = CreateService(
            new FakePlatformDashboardRepository(dashboard),
            new FakePlatformPermissionChecker(hasPermission: true));

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalTenants);
        Assert.Equal(2, result.Value.ActiveTenants);
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutDashboardPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformDashboardRepository(CreateDashboard()),
            new FakePlatformPermissionChecker(hasPermission: false));

        var result = await service.GetDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_dashboard.access_denied", result.Error.Code);
    }

    private static PlatformDashboardService CreateService(
        IPlatformDashboardRepository repository,
        IPlatformPermissionChecker permissionChecker)
    {
        return new PlatformDashboardService(repository, permissionChecker, new FakeDateTimeProvider());
    }

    private static PlatformDashboardResponse CreateDashboard()
    {
        return new PlatformDashboardResponse(
            TotalTenants: 3,
            ActiveTenants: 2,
            SuspendedTenants: 1,
            TrialTenants: 1,
            TotalSubscriptions: 2,
            ActiveSubscriptions: 1,
            PendingBillingCount: 1,
            TotalOutlets: 4,
            TotalTills: 5,
            TotalUsers: 6,
            RecentTenants:
            [
                new PlatformDashboardRecentTenantDto(
                    Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                    "TEN-001",
                    "Tenant One",
                    "active",
                    Now.AddDays(-1))
            ],
            AttentionItems:
            [
                new PlatformDashboardAttentionItemDto(
                    "suspended_tenants",
                    "Suspended Tenants",
                    "Tenants currently suspended.",
                    1,
                    "critical")
            ],
            GeneratedAt: Now);
    }

    private sealed class FakePlatformDashboardRepository : IPlatformDashboardRepository
    {
        private readonly PlatformDashboardResponse _dashboard;

        public FakePlatformDashboardRepository(PlatformDashboardResponse dashboard)
        {
            _dashboard = dashboard;
        }

        public Task<PlatformDashboardResponse> GetDashboardAsync(
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_dashboard with { GeneratedAt = generatedAt });
        }
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasPermission;

        public FakePlatformPermissionChecker(bool hasPermission)
        {
            _hasPermission = hasPermission;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _hasPermission &&
                string.Equals(permissionCode, PlatformPermissionCodes.DashboardView, StringComparison.Ordinal));
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}


