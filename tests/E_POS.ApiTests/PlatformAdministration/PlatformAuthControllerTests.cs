using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common.Cookies;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;


namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAuthControllerTests
{
    [Fact]
    public async Task Login_WithSuccessfulServiceResult_ReturnsOkResponseAndRefreshCookie()
    {
        var response = new PlatformAdminLoginResponse(
            "jwt-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new PlatformAdminUserDto(Guid.NewGuid(), "admin@tmepos.test", "ACTIVE"),
            ["platform.users.manage"]);
        var controller = CreateController(new FakePlatformAuthService(ApplicationResult<PlatformAdminLoginResponse>.Success(response)));

        var result = await controller.Login(new PlatformAdminLoginRequest("admin@tmepos.test", "password"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Contains("platform_refresh_token=refresh-token", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/platform-auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakePlatformAuthService(ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
            "platform_auth.invalid_credentials",
            "Invalid email or password."))));

        var result = await controller.Login(new PlatformAdminLoginRequest("admin@tmepos.test", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithoutPlatformAccess_ReturnsForbidden()
    {
        var controller = CreateController(new FakePlatformAuthService(ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
            "platform_auth.platform_access_denied",
            "Platform access denied."))));

        var result = await controller.Login(new PlatformAdminLoginRequest("admin@tmepos.test", "password"), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Logout_WithAuthenticatedPlatformSession_ReturnsNoContentAndClearsRefreshCookie()
    {
        var platformUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new FakePlatformAuthService(CreateLoginFailure(), ApplicationResult.Success());
        var controller = CreateController(service);
        SetPlatformClaims(controller, platformUserId, sessionId);

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(platformUserId, service.LogoutPlatformUserId);
        Assert.Equal(sessionId, service.LogoutSessionId);
        Assert.Contains("platform_refresh_token=", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/platform-auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_WithoutSessionClaim_ReturnsUnauthorized()
    {
        var service = new FakePlatformAuthService(CreateLoginFailure(), ApplicationResult.Success());
        var controller = CreateController(service);
        var platformUserId = Guid.NewGuid();
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", platformUserId.ToString())
            ],
            "Test"));

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.LogoutCallCount);
    }

    [Fact]
    public void LogoutEndpoint_RequiresPlatformOnlyPolicyWhileLoginAllowsAnonymous()
    {
        var loginMethod = typeof(PlatformAuthController).GetMethod(nameof(PlatformAuthController.Login));
        var logoutMethod = typeof(PlatformAuthController).GetMethod(nameof(PlatformAuthController.Logout));

        Assert.NotNull(loginMethod);
        Assert.NotNull(logoutMethod);
        Assert.NotNull(loginMethod!.GetCustomAttribute<AllowAnonymousAttribute>());
        var authorize = Assert.Single(logoutMethod!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("PlatformOnly", authorize.Policy);
    }

    [Fact]
    public async Task Refresh_WithSuccessfulServiceResult_ReturnsOkResponseAndRefreshCookie()
    {
        var response = new PlatformAdminLoginResponse(
            "jwt-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new PlatformAdminUserDto(Guid.NewGuid(), "admin@tmepos.test", "ACTIVE"),
            [PlatformPermissionCodes.TenantsView]);
        var controller = CreateController(new FakePlatformAuthService(
            ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError("platform_auth.invalid_credentials", "Invalid")),
            refreshResult: ApplicationResult<PlatformAdminLoginResponse>.Success(response)));
        controller.Request.Headers.Cookie = $"{PlatformAuthCookieHelper.RefreshTokenCookieName}=refresh-token";

        var result = await controller.Refresh(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Contains("platform_refresh_token=refresh-token", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakePlatformAuthService(
            CreateLoginFailure(),
            refreshResult: ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
                "platform_auth.invalid_refresh_token",
                "Invalid platform refresh token."))));

        var result = await controller.Refresh(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void RefreshEndpoint_AllowsAnonymousAccess()
    {
        var refreshMethod = typeof(PlatformAuthController).GetMethod(nameof(PlatformAuthController.Refresh));

        Assert.NotNull(refreshMethod);
        Assert.NotNull(refreshMethod!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task LegacyPlatformLogin_WithSuccessfulServiceResult_ReturnsLegacyApiResponseAndRefreshCookie()
    {
        var response = new PlatformAdminLoginResponse(
            "jwt-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new PlatformAdminUserDto(Guid.NewGuid(), "admin@tmepos.test", "ACTIVE"),
            PlatformPermissionCodes.All);
        var controller = CreateLegacyController(new FakePlatformAuthService(ApplicationResult<PlatformAdminLoginResponse>.Success(response)));

        var result = await controller.PlatformLogin(new PlatformAdminLoginRequest("admin@tmepos.test", "password"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<LegacyPlatformLoginResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("jwt-token", payload.Data.AccessToken);
        Assert.Equal(36, payload.Data.User.PlatformPermissions.Count);
        Assert.Contains("platform_refresh_token=refresh-token", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyPlatformLogin_WithInvalidPassword_ReturnsUnauthorized()
    {
        var controller = CreateLegacyController(new FakePlatformAuthService(ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
            "platform_auth.invalid_credentials",
            "Invalid email or password."))));

        var result = await controller.PlatformLogin(new PlatformAdminLoginRequest("admin@tmepos.test", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task LegacyPlatformLogout_WithRefreshCookie_RevokesSessionAndClearsCookie()
    {
        var service = new FakePlatformAuthService(CreateLoginFailure());
        var controller = CreateLegacyController(service);
        controller.Request.Headers.Cookie = $"{PlatformAuthCookieHelper.RefreshTokenCookieName}=refresh-token";

        var result = await controller.PlatformLogout(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<bool>>(ok.Value);
        Assert.True(payload.Success);
        Assert.True(payload.Data);
        Assert.Equal(1, service.LogoutByRefreshTokenCallCount);
        Assert.Equal("refresh-token", service.LogoutByRefreshTokenValue);
        Assert.Contains("platform_refresh_token=", controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("path=/api/v1/auth", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyPlatformLogout_WithAuthenticatedAccessToken_RevokesCurrentSession()
    {
        var platformUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new FakePlatformAuthService(CreateLoginFailure(), ApplicationResult.Success());
        var controller = CreateLegacyController(service);
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", platformUserId.ToString()),
                new Claim("session_id", sessionId.ToString())
            ],
            "Test"));

        var result = await controller.PlatformLogout(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(((LegacyApiResponse<bool>)ok.Value!).Data);
        Assert.Equal(1, service.LogoutCallCount);
        Assert.Equal(platformUserId, service.LogoutPlatformUserId);
        Assert.Equal(sessionId, service.LogoutSessionId);
        Assert.Equal(0, service.LogoutByRefreshTokenCallCount);
    }

    [Fact]
    public async Task LegacyPlatformRefresh_WithSuccessfulServiceResult_ReturnsLegacyApiResponse()
    {
        var response = new PlatformAdminLoginResponse(
            "jwt-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new PlatformAdminUserDto(Guid.NewGuid(), "admin@tmepos.test", "ACTIVE"),
            [PlatformPermissionCodes.TenantsView]);
        var controller = new PlatformAuthLegacyController(new FakePlatformAuthService(
            CreateLoginFailure(),
            refreshResult: ApplicationResult<PlatformAdminLoginResponse>.Success(response)))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Cookie = $"{PlatformAuthCookieHelper.RefreshTokenCookieName}=refresh-token";

        var result = await controller.PlatformRefresh(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<LegacyPlatformLoginResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("jwt-token", payload.Data.AccessToken);
        Assert.Equal([PlatformPermissionCodes.TenantsView], payload.Data.User.PlatformPermissions);
    }

    private static PlatformAuthLegacyController CreateLegacyController(FakePlatformAuthService service)
    {
        return new PlatformAuthLegacyController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static ApplicationResult<PlatformAdminLoginResponse> CreateLoginFailure()
    {
        return ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
            "platform_auth.invalid_credentials",
            "Invalid email or password."));
    }

    private static PlatformAuthController CreateController(FakePlatformAuthService service)
    {
        var controller = new PlatformAuthController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static void SetPlatformClaims(PlatformAuthController controller, Guid platformUserId, Guid sessionId)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", platformUserId.ToString()),
                new Claim("session_id", sessionId.ToString())
            ],
            "Test"));
    }

    private sealed class FakePlatformAuthService : IPlatformAuthService
    {
        private readonly ApplicationResult<PlatformAdminLoginResponse> _loginResult;
        private readonly ApplicationResult _logoutResult;
        private readonly ApplicationResult<PlatformAdminLoginResponse>? _refreshResult;

        public FakePlatformAuthService(
            ApplicationResult<PlatformAdminLoginResponse> loginResult,
            ApplicationResult? logoutResult = null,
            ApplicationResult<PlatformAdminLoginResponse>? refreshResult = null)
        {
            _loginResult = loginResult;
            _logoutResult = logoutResult ?? ApplicationResult.Success();
            _refreshResult = refreshResult;
        }

        public int LogoutCallCount { get; private set; }

        public int LogoutByRefreshTokenCallCount { get; private set; }

        public string? LogoutByRefreshTokenValue { get; private set; }

        public Guid? LogoutPlatformUserId { get; private set; }

        public Guid? LogoutSessionId { get; private set; }

        public Task<ApplicationResult<PlatformAdminLoginResponse>> LoginAsync(
            PlatformAdminLoginRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResult);
        }

        public Task<ApplicationResult> LogoutAsync(
            Guid platformUserId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            LogoutCallCount++;
            LogoutPlatformUserId = platformUserId;
            LogoutSessionId = sessionId;
            return Task.FromResult(_logoutResult);
        }

        public Task LogoutByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LogoutByRefreshTokenCallCount++;
            LogoutByRefreshTokenValue = refreshToken;
            return Task.CompletedTask;
        }

        public Task<ApplicationResult<PlatformAdminLoginResponse>> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_refreshResult ?? CreateLoginFailure());
        }
    }
}

