using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Application.Modules.AuthSecurity.Dtos;
using Microsoft.AspNetCore.Authorization;
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
        var controller = CreateController(new FakeTenantAuthService(ApplicationResult<TenantLoginResponse>.Success(response)));

        var result = await controller.Login(new TenantLoginRequest("user@tenant.test", "password"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Contains("tenant_refresh_token=refresh-token", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/tenant-auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeTenantAuthService(ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.invalid_credentials",
            "Invalid email or password."))));

        var result = await controller.Login(new TenantLoginRequest("user@tenant.test", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithTenantAccessDenied_ReturnsForbidden()
    {
        var controller = CreateController(new FakeTenantAuthService(ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.tenant_access_denied",
            "Tenant access denied."))));

        var result = await controller.Login(new TenantLoginRequest("user@tenant.test", "password"), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidationFailure_ReturnsBadRequest()
    {
        var controller = CreateController(new FakeTenantAuthService(ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.validation_failed",
            "Email and password are required."))));

        var result = await controller.Login(new TenantLoginRequest("", ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Logout_WithAuthenticatedTenantSession_ReturnsNoContentAndClearsRefreshCookie()
    {
        var tenantUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new FakeTenantAuthService(CreateLoginFailure(), ApplicationResult.Success());
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantUserId, tenantId, sessionId);

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(tenantUserId, service.LogoutTenantUserId);
        Assert.Equal(tenantId, service.LogoutTenantId);
        Assert.Equal(sessionId, service.LogoutSessionId);
        Assert.Contains("tenant_refresh_token=", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/tenant-auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_WithoutSessionClaim_ReturnsUnauthorized()
    {
        var service = new FakeTenantAuthService(CreateLoginFailure(), ApplicationResult.Success());
        var controller = CreateController(service);
        var tenantUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", tenantUserId.ToString()),
                new Claim("tenant_id", tenantId.ToString())
            ],
            "Test"));

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.LogoutCallCount);
    }

    [Fact]
    public void LogoutEndpoint_RequiresTenantOnlyPolicyWhileLoginAllowsAnonymous()
    {
        var loginMethod = typeof(TenantAuthController).GetMethod(nameof(TenantAuthController.Login));
        var logoutMethod = typeof(TenantAuthController).GetMethod(nameof(TenantAuthController.Logout));

        Assert.NotNull(loginMethod);
        Assert.NotNull(logoutMethod);
        Assert.NotNull(loginMethod!.GetCustomAttribute<AllowAnonymousAttribute>());
        var authorize = Assert.Single(logoutMethod!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static ApplicationResult<TenantLoginResponse> CreateLoginFailure()
    {
        return ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
            "tenant_auth.invalid_credentials",
            "Invalid email or password."));
    }

    private static TenantAuthController CreateController(FakeTenantAuthService service)
    {
        var controller = new TenantAuthController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static void SetTenantClaims(
        TenantAuthController controller,
        Guid tenantUserId,
        Guid tenantId,
        Guid sessionId)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", tenantUserId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("session_id", sessionId.ToString())
            ],
            "Test"));
    }

    private sealed class FakeTenantAuthService : ITenantAuthService
    {
        private readonly ApplicationResult<TenantLoginResponse> _loginResult;
        private readonly ApplicationResult _logoutResult;

        public FakeTenantAuthService(
            ApplicationResult<TenantLoginResponse> loginResult,
            ApplicationResult? logoutResult = null)
        {
            _loginResult = loginResult;
            _logoutResult = logoutResult ?? ApplicationResult.Success();
        }

        public int LogoutCallCount { get; private set; }

        public Guid? LogoutTenantUserId { get; private set; }

        public Guid? LogoutTenantId { get; private set; }

        public Guid? LogoutSessionId { get; private set; }

        public Task<ApplicationResult<TenantLoginResponse>> LoginAsync(
            TenantLoginRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResult);
        }

        public Task<ApplicationResult> LogoutAsync(
            Guid tenantUserId,
            Guid tenantId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            LogoutCallCount++;
            LogoutTenantUserId = tenantUserId;
            LogoutTenantId = tenantId;
            LogoutSessionId = sessionId;
            return Task.FromResult(_logoutResult);
        }
    }
}