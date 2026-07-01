using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Application.Modules.AuthSecurity.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.AuthSecurity;

public sealed class TenantAuthControllerTests
{
    [Fact]
    public async Task Login_WithSuccessfulServiceResult_ReturnsOkResponseAndRefreshCookie()
    {
        var response = new TenantLoginResponse(
            "jwt-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new TenantLoginUserDto(Guid.NewGuid(), Guid.NewGuid(), "USER@TENANT.TEST", "ACTIVE", "active"),
            ["tenant.dashboard.view"]);
        var controller = CreateController(ApplicationResult<TenantLoginResponse>.Success(response));

        var result = await controller.Login(new TenantLoginRequest("user@tenant.test", "password"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Contains("tenant_refresh_token=refresh-token", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/tenant-auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var controller = CreateController(ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.invalid_credentials",
            "Invalid email or password.")));

        var result = await controller.Login(new TenantLoginRequest("user@tenant.test", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithTenantAccessDenied_ReturnsForbidden()
    {
        var controller = CreateController(ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.tenant_access_denied",
            "Tenant access denied.")));

        var result = await controller.Login(new TenantLoginRequest("user@tenant.test", "password"), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidationFailure_ReturnsBadRequest()
    {
        var controller = CreateController(ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.validation_failed",
            "Email and password are required.")));

        var result = await controller.Login(new TenantLoginRequest("", ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static TenantAuthController CreateController(ApplicationResult<TenantLoginResponse> result)
    {
        var controller = new TenantAuthController(new FakeTenantAuthService(result));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private sealed class FakeTenantAuthService : ITenantAuthService
    {
        private readonly ApplicationResult<TenantLoginResponse> _result;

        public FakeTenantAuthService(ApplicationResult<TenantLoginResponse> result)
        {
            _result = result;
        }

        public Task<ApplicationResult<TenantLoginResponse>> LoginAsync(
            TenantLoginRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}