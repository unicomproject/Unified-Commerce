using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.AccessControl;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.AccessControl;

public sealed class TenantAdminRolesControllerTests
{
    [Fact]
    public async Task Create_WithTenantClaims_ForwardsIdempotencyKey()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var service = new FakeTenantAdminRoleService(CreateRoleDetail(roleId));
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, TenantAdminUserPermissions.RolesCreate);
        controller.Request.Headers["Idempotency-Key"] = "role-create-key";

        var result = await controller.Create(
            new TenantAdminRoleCreateRequest("Manager", "MANAGER", "Manages outlets"),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(tenantId, service.CreateContext?.TenantId);
        Assert.Equal(userId, service.CreateContext?.UserId);
        Assert.Equal("role-create-key", service.CreateIdempotencyKey);
    }

    [Fact]
    public async Task List_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminRoleService(CreateRoleDetail(Guid.NewGuid()));
        var controller = CreateController(service);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ReplacePermissions_WithDelegationFailure_ReturnsForbidden()
    {
        var roleId = Guid.NewGuid();
        var service = new FakeTenantAdminRoleService(CreateRoleDetail(roleId))
        {
            ReplacePermissionsResult = ApplicationResult<TenantRolePermissionsResponse>.Failure(
                new ApplicationError("tenant_roles.delegation_ceiling_exceeded", "Cannot grant permission.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminUserPermissions.RolesPermissionsUpdate);

        var result = await controller.ReplacePermissions(
            roleId,
            new TenantRolePermissionsUpdateRequest(["tenant.users.view"]),
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Delete_WithLastAdminProtection_ReturnsConflict()
    {
        var service = new FakeTenantAdminRoleService(CreateRoleDetail(Guid.NewGuid()))
        {
            DeleteResult = ApplicationResult.Failure(
                new ApplicationError("tenant_roles.last_admin_protected", "At least one admin must remain.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminUserPermissions.RolesDelete);

        var result = await controller.Delete(Guid.NewGuid(), null, CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(TenantAdminRolesController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static TenantAdminRolesController CreateController(FakeTenantAdminRoleService service)
    {
        var controller = new TenantAdminRolesController(
            service,
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        TenantAdminRolesController controller,
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

    private static TenantAdminRoleDetailResponse CreateRoleDetail(Guid roleId) =>
        new(
            roleId,
            "MANAGER",
            "Manager",
            "Manages outlets",
            true,
            false,
            3,
            2,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private sealed class FakeTenantAdminRoleService : ITenantAdminRoleService
    {
        private readonly TenantAdminRoleDetailResponse _detail;

        public FakeTenantAdminRoleService(TenantAdminRoleDetailResponse detail)
        {
            _detail = detail;
        }

        public TenantRequestContext? CreateContext { get; private set; }
        public string? CreateIdempotencyKey { get; private set; }
        public ApplicationResult<TenantRolePermissionsResponse> ReplacePermissionsResult { get; set; } =
            ApplicationResult<TenantRolePermissionsResponse>.Success(new TenantRolePermissionsResponse(
                Guid.NewGuid(), "MANAGER", "Manager", "TENANT", false, [], [], DateTimeOffset.UtcNow));
        public ApplicationResult DeleteResult { get; set; } = ApplicationResult.Success();

        public Task<ApplicationResult<TenantAdminRoleListResponse>> ListAsync(
            TenantRequestContext context,
            string? search,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminRoleListResponse>.Success(
                new TenantAdminRoleListResponse([], page, pageSize, 0, 0)));

        public Task<ApplicationResult<TenantAdminRoleDetailResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid roleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminRoleDetailResponse>.Success(_detail));

        public Task<ApplicationResult<TenantAdminRoleDetailResponse>> CreateAsync(
            TenantRequestContext context,
            TenantAdminRoleCreateRequest request,
            CancellationToken cancellationToken,
            string? idempotencyKey = null)
        {
            CreateContext = context;
            CreateIdempotencyKey = idempotencyKey;
            return Task.FromResult(ApplicationResult<TenantAdminRoleDetailResponse>.Success(_detail));
        }

        public Task<ApplicationResult<TenantAdminRoleDetailResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid roleId,
            TenantAdminRoleUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminRoleDetailResponse>.Success(_detail));

        public Task<ApplicationResult<TenantAdminRoleDetailResponse>> UpdateStatusAsync(
            TenantRequestContext context,
            Guid roleId,
            TenantAdminRoleStatusRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminRoleDetailResponse>.Success(_detail));

        public Task<ApplicationResult> DeleteAsync(
            TenantRequestContext context,
            Guid roleId,
            DateTimeOffset? expectedUpdatedAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeleteResult);

        public Task<ApplicationResult<TenantPermissionCatalogResponse>> GetPermissionCatalogAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantPermissionCatalogResponse>.Success(
                new TenantPermissionCatalogResponse([])));

        public Task<ApplicationResult<TenantRolePermissionsResponse>> GetPermissionsAsync(
            TenantRequestContext context,
            Guid roleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ReplacePermissionsResult);

        public Task<ApplicationResult<TenantRolePermissionsResponse>> ReplacePermissionsAsync(
            TenantRequestContext context,
            Guid roleId,
            TenantRolePermissionsUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ReplacePermissionsResult);

        public Task<ApplicationResult<TenantRoleAssignmentsResponse>> GetAssignmentsAsync(
            TenantRequestContext context,
            Guid roleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantRoleAssignmentsResponse>.Success(
                new TenantRoleAssignmentsResponse(roleId, "MANAGER", "Manager", false, [], DateTimeOffset.UtcNow)));

        public Task<ApplicationResult<TenantRoleAssignmentsResponse>> ReplaceAssignmentsAsync(
            TenantRequestContext context,
            Guid roleId,
            TenantRoleAssignmentsUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantRoleAssignmentsResponse>.Success(
                new TenantRoleAssignmentsResponse(roleId, "MANAGER", "Manager", false, [], DateTimeOffset.UtcNow)));
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
