using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAuthControllerTests
{
    [Fact]
    public async Task Login_WithSuccessfulServiceResult_ReturnsOkResponse()
    {
        var response = new PlatformAdminLoginResponse(
            "jwt-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new PlatformAdminUserDto(Guid.NewGuid(), "admin@tmepos.test", "ACTIVE"),
            ["platform.users.manage"]);
        var controller = CreateController(ApplicationResult<PlatformAdminLoginResponse>.Success(response));

        var result = await controller.Login(new PlatformAdminLoginRequest("admin@tmepos.test", "password"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var controller = CreateController(ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
            "platform_auth.invalid_credentials",
            "Invalid email or password.")));

        var result = await controller.Login(new PlatformAdminLoginRequest("admin@tmepos.test", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithoutPlatformAccess_ReturnsForbidden()
    {
        var controller = CreateController(ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
            "platform_auth.platform_access_denied",
            "Platform access denied.")));

        var result = await controller.Login(new PlatformAdminLoginRequest("admin@tmepos.test", "password"), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private static PlatformAuthController CreateController(ApplicationResult<PlatformAdminLoginResponse> result)
    {
        var controller = new PlatformAuthController(new FakePlatformAuthService(result));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private sealed class FakePlatformAuthService : IPlatformAuthService
    {
        private readonly ApplicationResult<PlatformAdminLoginResponse> _result;

        public FakePlatformAuthService(ApplicationResult<PlatformAdminLoginResponse> result)
        {
            _result = result;
        }

        public Task<ApplicationResult<PlatformAdminLoginResponse>> LoginAsync(
            PlatformAdminLoginRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}

