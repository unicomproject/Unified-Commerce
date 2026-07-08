using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.AccessControl;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.AccessControl;

public sealed class TenantAdminUsersControllerTests
{
    [Fact]
    public async Task Create_WithTenantClaims_ReturnsCreated()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var response = CreateDetailResponse(newUserId);
        var service = new FakeTenantAdminUserService(
            ApplicationResult<TenantAdminUserDetailResponse>.Success(response));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "tenant.users.create");

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(tenantId, service.CreateContext?.TenantId);
        Assert.Equal(userId, service.CreateContext?.UserId);
    }

    [Fact]
    public async Task List_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminUserService(
            ApplicationResult<TenantAdminUserDetailResponse>.Success(CreateDetailResponse(Guid.NewGuid())));
        var controller = CreateController(service);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithPermissionDeniedResult_ReturnsForbidden()
    {
        var service = new FakeTenantAdminUserService(
            ApplicationResult<TenantAdminUserDetailResponse>.Success(CreateDetailResponse(Guid.NewGuid())))
        {
            DeleteResult = ApplicationResult.Failure(new ApplicationError(
                "user.permission_denied",
                "Permission denied for user management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.users.view");

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(TenantAdminUsersController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static TenantAdminUsersController CreateController(FakeTenantAdminUserService service)
    {
        var controller = new TenantAdminUsersController(
            service,
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        TenantAdminUsersController controller,
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

    private static TenantAdminUserCreateRequest CreateRequest()
    {
        return new TenantAdminUserCreateRequest(
            "Jane Doe",
            "jane.doe@example.com",
            "+1234567890",
            Guid.NewGuid(),
            [],
            false,
            [],
            false);
    }

    private static TenantAdminUserDetailResponse CreateDetailResponse(Guid userId)
    {
        return new TenantAdminUserDetailResponse(
            userId,
            "Jane Doe",
            "JANE.DOE@EXAMPLE.COM",
            "+1234567890",
            Guid.NewGuid(),
            "Store Manager",
            [],
            "Active",
            false,
            [],
            null,
            DateTimeOffset.UtcNow,
            null);
    }

    private sealed class FakeTenantAdminUserService : ITenantAdminUserService
    {
        public FakeTenantAdminUserService(ApplicationResult<TenantAdminUserDetailResponse> createResult)
        {
            CreateResult = createResult;
        }

        public ApplicationResult<TenantAdminUserDetailResponse> CreateResult { get; }
        public ApplicationResult DeleteResult { get; set; } = ApplicationResult.Success();
        public TenantRequestContext? CreateContext { get; private set; }
        public TenantAdminUserCreateRequest? CreateRequest { get; private set; }

        public Task<ApplicationResult<TenantAdminUserListResponse>> ListAsync(
            TenantRequestContext context,
            string? search,
            string? status,
            Guid? roleId,
            Guid? outletId,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDirection,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminUserListResponse>.Success(
                new TenantAdminUserListResponse([], page, pageSize, 0)));

        public Task<ApplicationResult<TenantAdminUserCreateOptionsResponse>> GetCreateOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminUserCreateOptionsResponse>.Success(
                new TenantAdminUserCreateOptionsResponse([], [], [])));

        public Task<ApplicationResult<TenantAdminUserDetailResponse>> CreateAsync(
            TenantRequestContext context,
            TenantAdminUserCreateRequest request,
            CancellationToken cancellationToken)
        {
            CreateContext = context;
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<TenantAdminUserDetailResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult<TenantAdminUserDetailResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid userId,
            TenantAdminUserUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult> DeleteAsync(
            TenantRequestContext context,
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeleteResult);
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
