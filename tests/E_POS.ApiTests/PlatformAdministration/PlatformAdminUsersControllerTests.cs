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

public sealed class PlatformAdminUsersControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetUsers_WithPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserListResponse>.Success(CreateListResponse())),
            Guid.NewGuid());

        var result = await controller.GetUsers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformUserListResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Single(payload.Data.Users);
    }

    [Fact]
    public async Task GetUsers_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserListResponse>.Failure(new ApplicationError(
                    "platform_users.access_denied",
                    "Platform user access denied."))),
            Guid.NewGuid());

        var result = await controller.GetUsers(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserDetailResponse>.Failure(new ApplicationError(
                    "platform_users.not_found",
                    "Platform user was not found."))),
            Guid.NewGuid());

        var result = await controller.GetUser(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserDetailResponse>.Failure(new ApplicationError(
                    "platform_users.conflict",
                    "A platform user with this email already exists."))),
            Guid.NewGuid());

        var result = await controller.CreateUser(
            new CreatePlatformUserRequest
            {
                Email = "duplicate@example.com",
                RoleCodes = ["support_operator"]
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidRole_ReturnsBadRequest()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserDetailResponse>.Failure(new ApplicationError(
                    "platform_users.validation_failed",
                    "Unknown platform roles (roleCodes: unknown_role)."))),
            Guid.NewGuid());

        var result = await controller.CreateUser(
            new CreatePlatformUserRequest
            {
                Email = "support.user@example.com",
                RoleCodes = ["unknown_role"]
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserDetailResponse>.Failure(new ApplicationError(
                    "platform_users.access_denied",
                    "Platform user access denied."))),
            Guid.NewGuid());

        var result = await controller.UpdateUser(
            Guid.NewGuid(),
            new UpdatePlatformUserRequest { Status = PlatformAuthConstants.InactiveStatus },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task AssignRoles_WithProtectedLockout_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserDetailResponse>.Failure(new ApplicationError(
                    "platform_users.super_admin_lockout",
                    "Cannot remove the last active super administrator or deactivate the only active super administrator."))),
            Guid.NewGuid());

        var result = await controller.AssignRoles(
            PlatformAdminSeedConstants.DevelopmentPlatformUserId,
            new AssignPlatformUserRolesRequest { RoleCodes = ["support_operator"] },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformUserService(
                ApplicationResult<PlatformUserListResponse>.Success(CreateListResponse())),
            platformUserId: null);

        var result = await controller.GetUsers(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void UsersEndpoints_RequirePlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminUsersController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminUsersController CreateController(
        FakePlatformUserService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminUsersController(service)
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

    private static PlatformUserListResponse CreateListResponse()
    {
        return new PlatformUserListResponse(
        [
            new PlatformUserListItemDto(
                PlatformAdminSeedConstants.DevelopmentPlatformUserId,
                "admin@example.com",
                "admin",
                PlatformAuthConstants.ActiveStatus,
                [PlatformRoleCodes.SuperAdministrator],
                ["Super Administrator"],
                31,
                Now,
                Now,
                Now)
        ]);
    }

    private sealed class FakePlatformUserService : IPlatformUserService
    {
        private readonly ApplicationResult<PlatformUserListResponse> _listResult;
        private readonly ApplicationResult<PlatformUserDetailResponse> _detailResult;

        public FakePlatformUserService(ApplicationResult<PlatformUserListResponse> listResult)
        {
            _listResult = listResult;
            _detailResult = ApplicationResult<PlatformUserDetailResponse>.Failure(new ApplicationError(
                "platform_users.not_found",
                "Platform user was not found."));
        }

        public FakePlatformUserService(ApplicationResult<PlatformUserDetailResponse> detailResult)
        {
            _listResult = ApplicationResult<PlatformUserListResponse>.Success(new PlatformUserListResponse([]));
            _detailResult = detailResult;
        }

        public Task<ApplicationResult<PlatformUserListResponse>> GetUsersAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_listResult);
        }

        public Task<ApplicationResult<PlatformUserDetailResponse>> GetUserAsync(
            Guid userId,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }

        public Task<ApplicationResult<PlatformUserDetailResponse>> CreateUserAsync(
            CreatePlatformUserRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }

        public Task<ApplicationResult<PlatformUserDetailResponse>> UpdateUserAsync(
            Guid userId,
            UpdatePlatformUserRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }

        public Task<ApplicationResult<PlatformUserDetailResponse>> AssignRolesAsync(
            Guid userId,
            AssignPlatformUserRolesRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_detailResult);
        }
    }
}
