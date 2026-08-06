using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.OutletTillDevice;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.OutletTillDevice;

public sealed class TenantAdminOutletsOverviewControllerTests
{
    [Fact]
    public async Task GetOverview_WithValidContext_ReturnsOkWithOverviewData()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var overviewResponse = new TenantAdminOutletOverviewResponse(
            Outlet: new OutletOverviewInfoResponse(outletId, "Main Store", "S001", "STORE", "ACTIVE", null, "123 Main St", "Colombo"),
            Manager: null,
            Tills: new OutletOverviewTillSummaryResponse(2, 2, 2, 0),
            Sales: new OutletOverviewSalesSummaryResponse(1500m, 10.5m, "LKR"),
            Inventory: new OutletOverviewInventorySummaryResponse(50000m, "LKR"),
            Orders: new OutletOverviewOrderSummaryResponse(3),
            Health: new OutletOverviewHealthResponse("HEALTHY", DateTimeOffset.UtcNow, null),
            Alerts: Array.Empty<OutletOverviewAlertResponse>(),
            TotalActiveAlertCount: 0,
            Access: new OutletOverviewSectionAccessResponse(true, true, true, true, true));

        var service = new FakeTenantAdminOutletService { OverviewResult = ApplicationResult<TenantAdminOutletOverviewResponse>.Success(overviewResponse) };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.outlets.view");

        var result = await controller.GetOverview(outletId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetOverview_WithoutTenantContext_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminOutletService();
        var controller = CreateController(service);
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await controller.GetOverview(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetOverview_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var service = new FakeTenantAdminOutletService
        {
            OverviewResult = ApplicationResult<TenantAdminOutletOverviewResponse>.Failure(new ApplicationError("outlet.not_found", "Outlet was not found."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.outlets.view");

        var result = await controller.GetOverview(outletId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetOverview_WhenServiceReturnsPermissionDenied_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var service = new FakeTenantAdminOutletService
        {
            OverviewResult = ApplicationResult<TenantAdminOutletOverviewResponse>.Failure(new ApplicationError("outlet.permission_denied", "Permission denied for outlet management."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId);

        var result = await controller.GetOverview(outletId, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task SetManager_ValidRequest_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var service = new FakeTenantAdminOutletService { CommandResult = ApplicationResult.Success() };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.outlets.manage");

        var result = await controller.SetManager(outletId, new TenantAdminOutletManagerUpdateRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SetImage_ValidRequest_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var service = new FakeTenantAdminOutletService { CommandResult = ApplicationResult.Success() };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.outlets.update");

        var result = await controller.SetImage(outletId, new TenantAdminOutletImageUpdateRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task List_ValidTenantContext_ReturnsOk()
    {
        var controller = CreateController(new FakeTenantAdminOutletService());
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.outlets.view");

        var result = await controller.List(1, 20, "main", "STORE", "ACTIVE", "NEEDS_ATTENTION", "name", "asc", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ValidRequest_ReturnsOk()
    {
        var controller = CreateController(new FakeTenantAdminOutletService { CommandResult = ApplicationResult.Success() });
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.outlets.manage");

        var result = await controller.UpdateStatus(Guid.NewGuid(), new TenantAdminOutletStatusUpdateRequest("INACTIVE"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    private static TenantAdminOutletsController CreateController(ITenantAdminOutletService service)
    {
        var controller = new TenantAdminOutletsController(
            service,
            new FakeTenantAdminTillService(),
            new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(ControllerBase controller, Guid tenantId, Guid userId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("tenant_id", tenantId.ToString())
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    private sealed class FakeTenantAdminOutletService : ITenantAdminOutletService
    {
        public ApplicationResult<TenantAdminOutletOverviewResponse> OverviewResult { get; set; } = ApplicationResult<TenantAdminOutletOverviewResponse>.Failure(new ApplicationError("outlet.not_found", "Not found"));
        public ApplicationResult CommandResult { get; set; } = ApplicationResult.Success();

        public Task<ApplicationResult<TenantAdminOutletListResponse>> ListAsync(TenantRequestContext context, TenantAdminOutletListQuery query, CancellationToken cancellationToken)
            => Task.FromResult(ApplicationResult<TenantAdminOutletListResponse>.Success(new TenantAdminOutletListResponse([], query.PageNumber, query.PageSize, 0)));

        public Task<ApplicationResult<TenantAdminOutletDetailResponse>> GetDetailAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminOutletRevenueSummaryResponse>> GetRevenueSummaryAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminOutletUsersResponse>> GetUsersAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminOutletTillsResponse>> GetTillsAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminOutletOverviewResponse>> GetOverviewAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(OverviewResult);

        public Task<ApplicationResult> SetManagerAsync(TenantRequestContext context, Guid outletId, TenantAdminOutletManagerUpdateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CommandResult);

        public Task<ApplicationResult> RemoveManagerAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(CommandResult);

        public Task<ApplicationResult> SetImageAsync(TenantRequestContext context, Guid outletId, TenantAdminOutletImageUpdateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CommandResult);

        public Task<ApplicationResult> RemoveImageAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
            => Task.FromResult(CommandResult);

        public Task<ApplicationResult> UpdateStatusAsync(TenantRequestContext context, Guid outletId, TenantAdminOutletStatusUpdateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CommandResult);
    }

    private sealed class FakeTenantAdminTillService : ITenantAdminTillService
    {
        public Task<ApplicationResult<TenantAdminTillListResponse>> ListAsync(
            TenantRequestContext context,
            string? search,
            string? status,
            Guid? outletId,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDirection,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminTillSummaryResponse>> GetSummaryAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminTillDetailResponse>> CreateAsync(
            TenantRequestContext context,
            TenantAdminTillCreateRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminTillDetailResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid tillId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminTillDetailResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid tillId,
            TenantAdminTillUpdateRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult> DeleteAsync(
            TenantRequestContext context,
            Guid tillId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult.Success());

        public Task<ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>> GetOutletOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>.Success([]));

        public Task<ApplicationResult<TenantAdminTillCreateOptionsResponse>> GetCreateOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<TenantAdminTillHardwareReadinessResponse>> GetHardwareReadinessAsync(
            TenantRequestContext context,
            Guid tillId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
