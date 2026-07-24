using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using E_POS.Api.Controllers.V1.ECommerce.CustomerAuth;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CustomerAuth;

public sealed class CustomerAuthControllerTests
{
    [Fact]
    public async Task Login_Success_ForwardsTenantAndRequestMetadata()
    {
        var tenantId = Guid.NewGuid();
        var response = new CustomerLoginResponse(
            "customer-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            new CustomerLoginCustomerDto(
                Guid.NewGuid(), tenantId, "Customer", "customer@example.com", "+94770000000"));
        var service = new FakeService
        {
            LoginResult = ApplicationResult<CustomerAuthTokenResult>.Success(
                CreateTokenResult(response))
        };
        var controller = CreateController(service);
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.20");
        controller.Request.Headers.UserAgent = "customer-app";
        var request = new CustomerLoginRequest
        {
            EmailOrPhone = "customer@example.com",
            Password = "password"
        };

        var result = await controller.Login(
            tenantId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.LoginTenantId);
        Assert.Same(request, service.LoginRequest);
        Assert.Equal(IPAddress.Parse("192.0.2.20"), service.IpAddress);
        Assert.Equal("customer-app", service.UserAgent);
        var cookie = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains("customer_refresh_token=refresh-token", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/ecommerce/storefront/auth", cookie, StringComparison.OrdinalIgnoreCase);
        var responseJson = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("customer-token", responseJson);
        Assert.DoesNotContain("refresh-token", responseJson);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var service = new FakeService
        {
            LoginResult = ApplicationResult<CustomerAuthTokenResult>.Failure(
                new ApplicationError(
                    "customer_auth.invalid_credentials",
                    "Invalid email/phone or password."))
        };
        var controller = CreateController(service);

        var result = await controller.Login(
            Guid.NewGuid(),
            new CustomerLoginRequest
            {
                EmailOrPhone = "customer@example.com",
                Password = "wrong"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_ValidCookie_RotatesCookieAndReturnsAccessToken()
    {
        var tenantId = Guid.NewGuid();
        var response = new CustomerLoginResponse(
            "new-access-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            new CustomerLoginCustomerDto(
                Guid.NewGuid(), tenantId, "Customer", "customer@example.com", null));
        var service = new FakeService
        {
            RefreshResult = ApplicationResult<CustomerAuthTokenResult>.Success(
                CreateTokenResult(response, "replacement-refresh-token"))
        };
        var controller = CreateController(service);
        controller.Request.Headers.Cookie =
            "customer_refresh_token=current-refresh-token";

        var result = await controller.Refresh(tenantId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.RefreshTenantId);
        Assert.Equal("current-refresh-token", service.RefreshToken);
        Assert.Contains(
            "customer_refresh_token=replacement-refresh-token",
            controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Refresh_InvalidCookie_ReturnsUnauthorizedAndClearsCookie()
    {
        var service = new FakeService
        {
            RefreshResult = ApplicationResult<CustomerAuthTokenResult>.Failure(
                new ApplicationError(
                    "customer_auth.invalid_refresh_token",
                    "The refresh token is invalid or expired."))
        };
        var controller = CreateController(service);
        controller.Request.Headers.Cookie = "customer_refresh_token=invalid-token";

        var result = await controller.Refresh(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains(
            "customer_refresh_token=",
            controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Logout_AuthenticatedCustomer_RevokesClaimedSession()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var service = new FakeService { LogoutResult = ApplicationResult.Success() };
        var controller = CreateController(service);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", customerId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("session_id", sessionId.ToString())
        ], "Test"));

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(tenantId, service.LogoutTenantId);
        Assert.Equal(customerId, service.LogoutCustomerId);
        Assert.Equal(sessionId, service.LogoutSessionId);
        Assert.Contains(
            "customer_refresh_token=",
            controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public void Endpoints_ApplyExpectedAuthorizationPolicies()
    {
        var login = typeof(CustomerAuthController).GetMethod(nameof(CustomerAuthController.Login));
        var refresh = typeof(CustomerAuthController).GetMethod(nameof(CustomerAuthController.Refresh));
        var logout = typeof(CustomerAuthController).GetMethod(nameof(CustomerAuthController.Logout));

        Assert.NotNull(login);
        Assert.NotNull(refresh);
        Assert.NotNull(logout);
        Assert.NotNull(login!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(refresh!.GetCustomAttribute<AllowAnonymousAttribute>());
        var authorize = Assert.Single(logout!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("CustomerOnly", authorize.Policy);
    }

    private static CustomerAuthTokenResult CreateTokenResult(
        CustomerLoginResponse response,
        string refreshToken = "refresh-token") =>
        new(response, refreshToken, DateTimeOffset.UtcNow.AddDays(30));

    private static CustomerAuthController CreateController(FakeService service)
    {
        return new CustomerAuthController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class FakeService : ICustomerAuthService
    {
        public ApplicationResult<CustomerAuthTokenResult> LoginResult { get; init; } =
            ApplicationResult<CustomerAuthTokenResult>.Failure(
                new ApplicationError(
                    "customer_auth.invalid_credentials",
                    "Invalid email/phone or password."));
        public ApplicationResult<CustomerAuthTokenResult> RefreshResult { get; init; } =
            ApplicationResult<CustomerAuthTokenResult>.Failure(
                new ApplicationError(
                    "customer_auth.invalid_refresh_token",
                    "The refresh token is invalid or expired."));
        public ApplicationResult LogoutResult { get; init; } = ApplicationResult.Failure(
            new ApplicationError("customer_auth.invalid_session", "Invalid customer session."));
        public Guid? LoginTenantId { get; private set; }
        public CustomerLoginRequest? LoginRequest { get; private set; }
        public IPAddress? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public Guid? RefreshTenantId { get; private set; }
        public string? RefreshToken { get; private set; }
        public Guid? LogoutTenantId { get; private set; }
        public Guid? LogoutCustomerId { get; private set; }
        public Guid? LogoutSessionId { get; private set; }

        public Task<ApplicationResult<CustomerAuthTokenResult>> LoginAsync(
            Guid tenantId,
            CustomerLoginRequest request,
            IPAddress? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            LoginTenantId = tenantId;
            LoginRequest = request;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            return Task.FromResult(LoginResult);
        }

        public Task<ApplicationResult<CustomerAuthTokenResult>> RefreshAsync(
            Guid tenantId,
            string refreshToken,
            CancellationToken cancellationToken)
        {
            RefreshTenantId = tenantId;
            RefreshToken = refreshToken;
            return Task.FromResult(RefreshResult);
        }

        public Task<ApplicationResult> LogoutAsync(
            Guid tenantId,
            Guid customerId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            LogoutTenantId = tenantId;
            LogoutCustomerId = customerId;
            LogoutSessionId = sessionId;
            return Task.FromResult(LogoutResult);
        }

        public Task<ApplicationResult<E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos.CustomerProfileResponse>> GetProfileAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos.CustomerProfileResponse>.Failure(new ApplicationError("not_implemented", "not implemented")));
        }

        public Task<ApplicationResult> UpdateProfileAsync(
            Guid tenantId,
            Guid customerId,
            E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos.CustomerProfileUpdateRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult.Failure(new ApplicationError("not_implemented", "not implemented")));
        }
    }
}
