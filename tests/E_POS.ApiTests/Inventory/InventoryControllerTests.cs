using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.Inventory;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Contracts.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Contracts.Dashboard;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.Dashboard;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.Inventory;

public sealed class InventoryControllerTests
{
    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(InventoryController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task GetDashboardMetrics_WithPermission_ReturnsOk()
    {
        var metrics = new DashboardMetricsResponse(5, 2, 3, 100);
        var service = new FakeDashboardService
        {
            MetricsResult = ApplicationResult<DashboardMetricsResponse>.Success(metrics)
        };
        var controller = CreateController(dashboardService: service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.stock.view");

        var result = await controller.GetDashboardMetrics(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetDashboardMetrics_WithoutClaims_ReturnsUnauthorized()
    {
        var service = new FakeDashboardService();
        var controller = CreateController(dashboardService: service);

        var result = await controller.GetDashboardMetrics(null, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetDashboardMetrics_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeDashboardService
        {
            MetricsResult = ApplicationResult<DashboardMetricsResponse>.Failure(
                new ApplicationError("inventory.permission_denied", "Permission denied."))
        };
        var controller = CreateController(dashboardService: service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "some_other_permission");

        var result = await controller.GetDashboardMetrics(null, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private static InventoryController CreateController(FakeDashboardService? dashboardService = null, FakeCurrentStockService? currentStockService = null)
    {
        var controller = new InventoryController(
            dashboardService ?? new FakeDashboardService(),
            currentStockService ?? new FakeCurrentStockService(),
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        InventoryController controller,
        Guid tenantId,
        Guid userId,
        string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission),
            ],
            "Test"));
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public ApplicationResult<DashboardMetricsResponse> MetricsResult { get; init; } =
            ApplicationResult<DashboardMetricsResponse>.Success(new DashboardMetricsResponse(0, 0, 0, 0));

        public Task<ApplicationResult<DashboardMetricsResponse>> GetDashboardMetricsAsync(TenantRequestContext context, Guid? outletId, CancellationToken cancellationToken)
        {
            return Task.FromResult(MetricsResult);
        }

        public Task<ApplicationResult<DashboardAlertsResponse>> GetDashboardAlertsAsync(TenantRequestContext context, Guid? outletId, int page, int pageSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<DashboardActivitiesResponse>> GetDashboardActivitiesAsync(TenantRequestContext context, Guid? outletId, int page, int pageSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeCurrentStockService : ICurrentStockService
    {
        public Task<ApplicationResult<CurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(TenantRequestContext context, Guid? outletId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<CurrentStockListResponse>> GetCurrentStockAsync(TenantRequestContext context, CurrentStockQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<byte[]>> ExportCurrentStockAsync(TenantRequestContext context, CurrentStockQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<Application.Modules.Tenant.Inventory.Dtos.StockIn.StockInResponse>> StockInAsync(TenantRequestContext context, Application.Modules.Tenant.Inventory.Dtos.StockIn.StockInRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<ProductStockDetailResponse>> GetProductStockDetailAsync(TenantRequestContext context, Guid productVariantId, Guid? outletId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<StockMovementHistoryListResponse>> GetStockMovementHistoryAsync(TenantRequestContext context, StockMovementHistoryQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeTenantRequestContextFactory : ITenantRequestContextFactory
    {
        public bool TryCreate(ClaimsPrincipal user, out TenantRequestContext context)
        {
            var tenantUserIdValue = user.FindFirstValue("sub");
            var tenantIdValue = user.FindFirstValue("tenant_id");
            var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out var tenantUserId);
            var hasTenantId = Guid.TryParse(tenantIdValue, out var tenantId);

            if (!hasTenantUserId || !hasTenantId)
            {
                context = new TenantRequestContext(Guid.Empty, Guid.Empty, []);
                return false;
            }

            var permissions = user.FindAll("permissions")
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            context = new TenantRequestContext(tenantId, tenantUserId, permissions);
            return true;
        }
    }
}
