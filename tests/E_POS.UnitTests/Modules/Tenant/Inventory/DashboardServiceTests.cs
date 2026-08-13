using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Services;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.Modules.Tenant.Inventory;

public sealed class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _mockRepository;
    private readonly DashboardService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        _mockRepository = new Mock<IDashboardRepository>();
        _service = new DashboardService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_WithoutPermission_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, ["some_other_permission"]);

        var result = await _service.GetDashboardMetricsAsync(context, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_WithOutletWithoutAccess_ReturnsFailure()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.DashboardView]);
        var outletId = Guid.NewGuid();
        _mockRepository.Setup(r => r.UserHasOutletAccessAsync(_tenantId, _userId, outletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.GetDashboardMetricsAsync(context, outletId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inventory.permission_denied", result.Error?.Code);
        Assert.Equal("You do not have access to this outlet.", result.Error?.Message);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_WithPermissionAndAccess_ReturnsMetrics()
    {
        var context = new TenantRequestContext(_tenantId, _userId, [StockPermissions.DashboardView]);
        var expectedMetrics = new DashboardMetricsResponse(10, 5, 2, 5000);
        
        _mockRepository.Setup(r => r.GetDashboardMetricsAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        var result = await _service.GetDashboardMetricsAsync(context, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedMetrics, result.Value);
    }
}
