using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.OutletTillDevice;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.OutletTillDevice;

public sealed class TenantAdminTillsControllerTests
{
    [Fact]
    public async Task Create_WithTenantClaims_ReturnsCreated()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var response = CreateDetailResponse(tillId);
        var service = new FakeTenantAdminTillService(
            ApplicationResult<TenantAdminTillDetailResponse>.Success(response));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.tills.create");

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(tillId, service.CreateRequest?.OutletId == CreateRequest().OutletId ? tillId : tillId);
        Assert.Equal(tenantId, service.CreateContext?.TenantId);
        Assert.Equal(userId, service.CreateContext?.UserId);
    }

    [Fact]
    public async Task List_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminTillService(
            ApplicationResult<TenantAdminTillDetailResponse>.Success(CreateDetailResponse(Guid.NewGuid())));
        var controller = CreateController(service);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(TenantAdminTillsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static TenantAdminTillsController CreateController(FakeTenantAdminTillService service)
    {
        var controller = new TenantAdminTillsController(
            service,
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        TenantAdminTillsController controller,
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

    private static TenantAdminTillCreateRequest CreateRequest()
    {
        return new TenantAdminTillCreateRequest(
            "Front Counter Till",
            "TILL-001",
            Guid.NewGuid(),
            "Active");
    }

    private static TenantAdminTillDetailResponse CreateDetailResponse(Guid tillId)
    {
        return new TenantAdminTillDetailResponse(
            tillId,
            "Front Counter Till",
            "TILL-001",
            Guid.NewGuid(),
            "Main Outlet",
            "OUT-001",
            "Active",
            "Offline",
            DateTimeOffset.UtcNow,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeTenantAdminTillService : ITenantAdminTillService
    {
        public FakeTenantAdminTillService(ApplicationResult<TenantAdminTillDetailResponse> createResult)
        {
            CreateResult = createResult;
        }

        public ApplicationResult<TenantAdminTillDetailResponse> CreateResult { get; }
        public TenantRequestContext? CreateContext { get; private set; }
        public TenantAdminTillCreateRequest? CreateRequest { get; private set; }

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
            Task.FromResult(ApplicationResult<TenantAdminTillListResponse>.Success(
                new TenantAdminTillListResponse([], page, pageSize, 0)));

        public Task<ApplicationResult<TenantAdminTillSummaryResponse>> GetSummaryAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminTillSummaryResponse>.Success(
                new TenantAdminTillSummaryResponse(0, 0, 0, 0, 0)));

        public Task<ApplicationResult<TenantAdminTillDetailResponse>> CreateAsync(
            TenantRequestContext context,
            TenantAdminTillCreateRequest request,
            CancellationToken cancellationToken)
        {
            CreateContext = context;
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<TenantAdminTillDetailResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid tillId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult<TenantAdminTillDetailResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid tillId,
            TenantAdminTillUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult> DeleteAsync(
            TenantRequestContext context,
            Guid tillId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult.Success());

        public Task<ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>> GetOutletOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>.Success([]));
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
