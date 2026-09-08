using System.Reflection;
using E_POS.Api.Controllers.V1.Tenant.TenantAuth;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace E_POS.ApiTests.AuthSecurity;

public sealed class TenantAdminOnboardingInvitationControllerTests
{
    private const string ValidToken = "valid-setup-token-12345";
    private const string ExpiredToken = "expired-setup-token-67890";
    private const string UsedToken = "used-setup-token-54321";
    private const string InvalidToken = "invalid-unknown-token-00000";
    private const string AdminEmail = "admin@example.com";

    [Fact]
    public async Task ValidateSetupToken_WithValidToken_ReturnsOkWithEmailAndValidTrue()
    {
        var expectedResponse = new ValidateTenantAdminSetupTokenResponse(
            SetupToken: ValidToken,
            Valid: true,
            Expired: false,
            Email: AdminEmail,
            Message: null);

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            ValidateResult = expectedResponse
        };

        var controller = CreateController(fakeService);

        var result = await controller.ValidateSetupToken(ValidToken, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var setupTokenProp = ok.Value.GetType().GetProperty("setupToken")?.GetValue(ok.Value);
        var validProp = ok.Value.GetType().GetProperty("valid")?.GetValue(ok.Value);
        var expiredProp = ok.Value.GetType().GetProperty("expired")?.GetValue(ok.Value);
        var emailProp = ok.Value.GetType().GetProperty("email")?.GetValue(ok.Value);

        Assert.Equal(ValidToken, setupTokenProp);
        Assert.Equal(true, validProp);
        Assert.Equal(false, expiredProp);
        Assert.Equal(AdminEmail, emailProp);
    }

    [Fact]
    public async Task ValidateSetupToken_WithUnknownToken_ReturnsOkWithValidFalse()
    {
        var expectedResponse = new ValidateTenantAdminSetupTokenResponse(
            SetupToken: InvalidToken,
            Valid: false,
            Expired: false,
            Email: null,
            Message: "This invitation link is invalid or no longer available.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            ValidateResult = expectedResponse
        };

        var controller = CreateController(fakeService);

        var result = await controller.ValidateSetupToken(InvalidToken, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var validProp = ok.Value.GetType().GetProperty("valid")?.GetValue(ok.Value);
        var expiredProp = ok.Value.GetType().GetProperty("expired")?.GetValue(ok.Value);
        var emailProp = ok.Value.GetType().GetProperty("email")?.GetValue(ok.Value);

        Assert.Equal(false, validProp);
        Assert.Equal(false, expiredProp);
        Assert.Null(emailProp);
    }

    [Fact]
    public async Task ValidateSetupToken_WithExpiredToken_ReturnsOkWithExpiredTrue()
    {
        var expectedResponse = new ValidateTenantAdminSetupTokenResponse(
            SetupToken: ExpiredToken,
            Valid: false,
            Expired: true,
            Email: null,
            Message: "This invitation link has expired.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            ValidateResult = expectedResponse
        };

        var controller = CreateController(fakeService);

        var result = await controller.ValidateSetupToken(ExpiredToken, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var validProp = ok.Value.GetType().GetProperty("valid")?.GetValue(ok.Value);
        var expiredProp = ok.Value.GetType().GetProperty("expired")?.GetValue(ok.Value);

        Assert.Equal(false, validProp);
        Assert.Equal(true, expiredProp);
    }

    [Fact]
    public async Task ValidateSetupToken_WithUsedToken_ReturnsOkWithValidFalseAndUsedMessage()
    {
        var expectedResponse = new ValidateTenantAdminSetupTokenResponse(
            SetupToken: UsedToken,
            Valid: false,
            Expired: false,
            Email: null,
            Message: "This invitation link has already been used.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            ValidateResult = expectedResponse
        };

        var controller = CreateController(fakeService);

        var result = await controller.ValidateSetupToken(UsedToken, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var validProp = ok.Value.GetType().GetProperty("valid")?.GetValue(ok.Value);
        var messageProp = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value);

        Assert.Equal(false, validProp);
        Assert.Equal("This invitation link has already been used.", messageProp);
    }

    [Fact]
    public async Task ValidateSetupToken_WithWrongTenantOrUserBinding_ReturnsOkWithValidFalse()
    {
        var expectedResponse = new ValidateTenantAdminSetupTokenResponse(
            SetupToken: InvalidToken,
            Valid: false,
            Expired: false,
            Email: null,
            Message: "This tenant is not available for account setup.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            ValidateResult = expectedResponse
        };

        var controller = CreateController(fakeService);

        var result = await controller.ValidateSetupToken(InvalidToken, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var validProp = ok.Value.GetType().GetProperty("valid")?.GetValue(ok.Value);
        var messageProp = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value);

        Assert.Equal(false, validProp);
        Assert.Equal("This tenant is not available for account setup.", messageProp);
    }

    [Fact]
    public async Task SetupPassword_WithValidCredentials_ReturnsOkWithSuccessTrue()
    {
        var expectedResponse = new SetupTenantAdminPasswordResponse(
            Success: true,
            Message: "Account setup completed. You can sign in.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            SetupPasswordResult = ApplicationResult<SetupTenantAdminPasswordResponse>.Success(expectedResponse)
        };

        var controller = CreateController(fakeService);

        var body = new TenantAdminOnboardingInvitationController.SetupTenantAdminPasswordRequestBody
        {
            SetupToken = ValidToken,
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!"
        };

        var result = await controller.SetupPassword(body, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var successProp = ok.Value.GetType().GetProperty("success")?.GetValue(ok.Value);
        var messageProp = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value);

        Assert.Equal(true, successProp);
        Assert.Equal("Account setup completed. You can sign in.", messageProp);
        Assert.Equal(ValidToken, fakeService.LastSetupRequest?.SetupToken);
    }

    [Fact]
    public async Task SetupPassword_WithWeakPassword_ReturnsBadRequestWithPasswordInvalid()
    {
        var policyError = new ApplicationError(
            TenantAdminInvitationAcceptanceService.ErrorPasswordInvalid,
            "Password must include uppercase, lowercase, and numeric characters.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            SetupPasswordResult = ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(policyError)
        };

        var controller = CreateController(fakeService);

        var body = new TenantAdminOnboardingInvitationController.SetupTenantAdminPasswordRequestBody
        {
            SetupToken = ValidToken,
            Password = "weak",
            ConfirmPassword = "weak"
        };

        var result = await controller.SetupPassword(body, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);

        var codeProp = badRequest.Value.GetType().GetProperty("code")?.GetValue(badRequest.Value);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorPasswordInvalid, codeProp);
    }

    [Fact]
    public async Task SetupPassword_WithPasswordMismatch_ReturnsBadRequestWithPasswordMismatch()
    {
        var mismatchError = new ApplicationError(
            TenantAdminInvitationAcceptanceService.ErrorPasswordMismatch,
            "Password and confirmation do not match.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            SetupPasswordResult = ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(mismatchError)
        };

        var controller = CreateController(fakeService);

        var body = new TenantAdminOnboardingInvitationController.SetupTenantAdminPasswordRequestBody
        {
            SetupToken = ValidToken,
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        var result = await controller.SetupPassword(body, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);

        var codeProp = badRequest.Value.GetType().GetProperty("code")?.GetValue(badRequest.Value);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorPasswordMismatch, codeProp);
    }

    [Fact]
    public async Task SetupPassword_WithExpiredToken_ReturnsBadRequestWithInviteExpired()
    {
        var expiredError = new ApplicationError(
            TenantAdminInvitationAcceptanceService.ErrorInviteExpired,
            "This invitation link has expired.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            SetupPasswordResult = ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(expiredError)
        };

        var controller = CreateController(fakeService);

        var body = new TenantAdminOnboardingInvitationController.SetupTenantAdminPasswordRequestBody
        {
            SetupToken = ExpiredToken,
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!"
        };

        var result = await controller.SetupPassword(body, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);

        var codeProp = badRequest.Value.GetType().GetProperty("code")?.GetValue(badRequest.Value);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteExpired, codeProp);
    }

    [Fact]
    public async Task SetupPassword_WithUsedToken_ReturnsBadRequestWithInviteUsed()
    {
        var usedError = new ApplicationError(
            TenantAdminInvitationAcceptanceService.ErrorInviteUsed,
            "This invitation link has already been used.");

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            SetupPasswordResult = ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(usedError)
        };

        var controller = CreateController(fakeService);

        var body = new TenantAdminOnboardingInvitationController.SetupTenantAdminPasswordRequestBody
        {
            SetupToken = UsedToken,
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!"
        };

        var result = await controller.SetupPassword(body, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);

        var codeProp = badRequest.Value.GetType().GetProperty("code")?.GetValue(badRequest.Value);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteUsed, codeProp);
    }

    [Fact]
    public async Task SecretResponseSafety_ResponsesDoNotExposeSensitiveHashesOrSalts()
    {
        var expectedResponse = new ValidateTenantAdminSetupTokenResponse(
            SetupToken: ValidToken,
            Valid: true,
            Expired: false,
            Email: AdminEmail,
            Message: null);

        var fakeService = new FakeTenantAdminInvitationAcceptanceService
        {
            ValidateResult = expectedResponse,
            SetupPasswordResult = ApplicationResult<SetupTenantAdminPasswordResponse>.Success(
                new SetupTenantAdminPasswordResponse(true, "Setup complete."))
        };

        var controller = CreateController(fakeService);

        var validateResult = await controller.ValidateSetupToken(ValidToken, CancellationToken.None);
        var okValidate = Assert.IsType<OkObjectResult>(validateResult);

        var validateProps = okValidate.Value!.GetType().GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();
        Assert.DoesNotContain("tokenhash", validateProps);
        Assert.DoesNotContain("invitetokenhash", validateProps);
        Assert.DoesNotContain("passwordhash", validateProps);
        Assert.DoesNotContain("passwordsalt", validateProps);

        var setupResult = await controller.SetupPassword(
            new TenantAdminOnboardingInvitationController.SetupTenantAdminPasswordRequestBody
            {
                SetupToken = ValidToken,
                Password = "ValidPassword123!",
                ConfirmPassword = "ValidPassword123!"
            },
            CancellationToken.None);

        var okSetup = Assert.IsType<OkObjectResult>(setupResult);
        var setupProps = okSetup.Value!.GetType().GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();
        Assert.DoesNotContain("password", setupProps);
        Assert.DoesNotContain("passwordhash", setupProps);
        Assert.DoesNotContain("salt", setupProps);
    }

    [Fact]
    public void Controller_HasAllowAnonymous_AndRateLimitingAttributes()
    {
        var controllerType = typeof(TenantAdminOnboardingInvitationController);
        Assert.NotNull(controllerType.GetCustomAttribute<AllowAnonymousAttribute>());

        var validateMethod = controllerType.GetMethod(nameof(TenantAdminOnboardingInvitationController.ValidateSetupToken));
        Assert.NotNull(validateMethod);
        var validateRateLimit = validateMethod.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(validateRateLimit);
        Assert.Equal(RateLimitingPolicies.AuthLogin, validateRateLimit.PolicyName);

        var setupPasswordMethod = controllerType.GetMethod(nameof(TenantAdminOnboardingInvitationController.SetupPassword));
        Assert.NotNull(setupPasswordMethod);
        var setupRateLimit = setupPasswordMethod.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(setupRateLimit);
        Assert.Equal(RateLimitingPolicies.AuthLogin, setupRateLimit.PolicyName);
    }

    private static TenantAdminOnboardingInvitationController CreateController(ITenantAdminInvitationAcceptanceService service)
    {
        var controller = new TenantAdminOnboardingInvitationController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private sealed class FakeTenantAdminInvitationAcceptanceService : ITenantAdminInvitationAcceptanceService
    {
        public ValidateTenantAdminSetupTokenResponse? ValidateResult { get; set; }
        public ApplicationResult<SetupTenantAdminPasswordResponse>? SetupPasswordResult { get; set; }
        public SetupTenantAdminPasswordRequest? LastSetupRequest { get; private set; }

        public Task<ValidateTenantAdminSetupTokenResponse> ValidateSetupTokenAsync(
            string rawToken,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ValidateResult ?? new ValidateTenantAdminSetupTokenResponse(
                rawToken, false, false, null, "Invalid"));
        }

        public Task<ApplicationResult<SetupTenantAdminPasswordResponse>> SetupPasswordAsync(
            SetupTenantAdminPasswordRequest request,
            CancellationToken cancellationToken)
        {
            LastSetupRequest = request;
            return Task.FromResult(SetupPasswordResult ??
                ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(
                    new ApplicationError("INVITE_INVALID", "Invalid")));
        }
    }
}
