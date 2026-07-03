using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAdminRolesControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetRoles_WithPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformRoleService(
                ApplicationResult<PlatformRoleListResponse>.Success(CreateListResponse())),
            Guid.NewGuid());

        var result = await controller.GetRoles(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformRoleListResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Single(payload.Data.Roles);
    }

    [Fact]
    public async Task GetRole_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformRoleService(
                ApplicationResult<PlatformRoleDetailResponse>.Failure(new ApplicationError(
                    "platform_roles.access_denied",
                    "Platform role access denied."))),
            Guid.NewGuid());

        var result = await controller.GetRole(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetRole_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(
            new FakePlatformRoleService(
                ApplicationResult<PlatformRoleDetailResponse>.Failure(new ApplicationError(
                    "platform_roles.not_found",
                    "Platform role was not found."))),
            Guid.NewGuid());

        var result = await controller.GetRole(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateRole_WithDuplicateCode_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformRoleService(
                ApplicationResult<PlatformRoleDetailResponse>.Failure(new ApplicationError(
                    "platform_roles.conflict",
                    "A platform role with this code already exists."))),
            Guid.NewGuid());

        var result = await controller.CreateRole(
            new CreatePlatformRoleRequest { RoleCode = "support_operator", Name = "Support Operator" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateRolePermissions_WithInvalidPermission_ReturnsBadRequest()
    {
        var controller = CreateController(
            new FakePlatformRoleService(
                ApplicationResult<PlatformRolePermissionsResponse>.Failure(new ApplicationError(
                    "platform_roles.validation_failed",
                    "Unknown platform permission codes: platform.unknown.permission."))),
            Guid.NewGuid());

        var result = await controller.UpdateRolePermissions(
            Guid.NewGuid(),
            new UpdatePlatformRolePermissionsRequest { PermissionCodes = ["platform.unknown.permission"] },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetRoles_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformRoleService(
                ApplicationResult<PlatformRoleListResponse>.Success(CreateListResponse())),
            platformUserId: null);

        var result = await controller.GetRoles(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void RolesEndpoints_RequirePlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminRolesController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminRolesController CreateController(
        FakePlatformRoleService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminRolesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (platformUserId is not null)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", platformUserId.Value.ToString())],
                "Test"));
        }

        return controller;
    }

    private static PlatformRoleListResponse CreateListResponse()
    {
        return new PlatformRoleListResponse(
        [
            new PlatformRoleListItemDto(
                PlatformAdminSeedConstants.SuperAdministratorRoleId,
                PlatformRoleCodes.SuperAdministrator,
                "Super Administrator",
                "Protected role.",
                PlatformAuthConstants.ActiveStatus,
                36,
                1,
                true,
                true,
                Now,
                Now)
        ]);
    }

    private sealed class FakePlatformRoleService : IPlatformRoleService
    {
        private readonly ApplicationResult<PlatformRoleListResponse> _listResult;
        private readonly ApplicationResult<PlatformRoleDetailResponse> _detailResult;
        private readonly ApplicationResult<PlatformRolePermissionsResponse> _permissionsResult;

        public FakePlatformRoleService(ApplicationResult<PlatformRoleListResponse> listResult)
        {
            _listResult = listResult;
            _detailResult = ApplicationResult<PlatformRoleDetailResponse>.Failure(new ApplicationError(
                "platform_roles.not_found",
                "Platform role was not found."));
            _permissionsResult = ApplicationResult<PlatformRolePermissionsResponse>.Failure(new ApplicationError(
                "platform_roles.not_found",
                "Platform role was not found."));
        }

        public FakePlatformRoleService(ApplicationResult<PlatformRoleDetailResponse> detailResult)
        {
            _listResult = ApplicationResult<PlatformRoleListResponse>.Success(new PlatformRoleListResponse([]));
            _detailResult = detailResult;
            _permissionsResult = ApplicationResult<PlatformRolePermissionsResponse>.Failure(new ApplicationError(
                "platform_roles.not_found",
                "Platform role was not found."));
        }

        public FakePlatformRoleService(ApplicationResult<PlatformRolePermissionsResponse> permissionsResult)
        {
            _listResult = ApplicationResult<PlatformRoleListResponse>.Success(new PlatformRoleListResponse([]));
            _detailResult = ApplicationResult<PlatformRoleDetailResponse>.Failure(new ApplicationError(
                "platform_roles.not_found",
                "Platform role was not found."));
            _permissionsResult = permissionsResult;
        }

        public Task<ApplicationResult<PlatformRoleListResponse>> GetRolesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_listResult);
        }

        public Task<ApplicationResult<PlatformRoleDetailResponse>> GetRoleAsync(
            Guid roleId,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }

        public Task<ApplicationResult<PlatformRoleDetailResponse>> CreateRoleAsync(
            CreatePlatformRoleRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }

        public Task<ApplicationResult<PlatformRoleDetailResponse>> UpdateRoleAsync(
            Guid roleId,
            UpdatePlatformRoleRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }

        public Task<ApplicationResult<PlatformRolePermissionsResponse>> GetRolePermissionsAsync(
            Guid roleId,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_permissionsResult);
        }

        public Task<ApplicationResult<PlatformRolePermissionsResponse>> UpdateRolePermissionsAsync(
            Guid roleId,
            UpdatePlatformRolePermissionsRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_permissionsResult);
        }
    }
}
