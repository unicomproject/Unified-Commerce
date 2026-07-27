using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformPasswordResetControllerTests
{
    [Fact]
    public async Task Validate_AlwaysReturns200WithSafePayload()
    {
        var controller = new PlatformPasswordResetController(
            new FakeResetService(ApplicationResult<ValidatePlatformPasswordResetTokenResponse>.Success(
                new ValidatePlatformPasswordResetTokenResponse(false, "INVALID", null))));

        var result = await controller.Validate(
            new ValidatePlatformPasswordResetTokenRequest("bad"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ValidatePlatformPasswordResetTokenResponse>(ok.Value);
        Assert.False(payload.IsValid);
        Assert.Equal("INVALID", payload.Status);
    }

    [Fact]
    public async Task Complete_OnFailure_ReturnsBadRequest()
    {
        var controller = new PlatformPasswordResetController(
            new FakeResetService(ApplicationResult<CompletePlatformPasswordResetResponse>.Failure(
                new ApplicationError("platform_password_reset.invalid_token", "Password reset token is invalid."))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Complete(
            new CompletePlatformPasswordResetRequest("token", "NewPass123", "NewPass123"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task InitiatePasswordReset_WithoutPermission_ReturnsForbidden()
    {
        var controller = new PlatformAdminUsersController(
            new NoOpUserService(),
            new FakeResetService(ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(
                new ApplicationError("platform_users.access_denied", "Platform user access denied."))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString())],
                "Test"));

        var result = await controller.InitiatePasswordReset(Guid.NewGuid(), CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private sealed class NoOpUserService : IPlatformUserService
    {
        public Task<ApplicationResult<PlatformUserListResponse>> GetUsersAsync(Guid platformUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<PlatformUserDetailResponse>> GetUserAsync(Guid userId, Guid platformUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<PlatformUserDetailResponse>> CreateUserAsync(CreatePlatformUserRequest request, Guid platformUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<PlatformUserDetailResponse>> UpdateUserAsync(Guid userId, UpdatePlatformUserRequest request, Guid platformUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<PlatformUserDetailResponse>> AssignRolesAsync(Guid userId, AssignPlatformUserRolesRequest request, Guid platformUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class FakeResetService : IPlatformPasswordResetService
    {
        private readonly ApplicationResult<ValidatePlatformPasswordResetTokenResponse>? _validate;
        private readonly ApplicationResult<CompletePlatformPasswordResetResponse>? _complete;
        private readonly ApplicationResult<InitiatePlatformPasswordResetResponse>? _initiate;

        public FakeResetService(ApplicationResult<ValidatePlatformPasswordResetTokenResponse> validate)
        {
            _validate = validate;
        }

        public FakeResetService(ApplicationResult<CompletePlatformPasswordResetResponse> complete)
        {
            _complete = complete;
        }

        public FakeResetService(ApplicationResult<InitiatePlatformPasswordResetResponse> initiate)
        {
            _initiate = initiate;
        }

        public Task<ApplicationResult<PlatformPasswordResetTokenIssueResult>> CreatePendingResetTokenAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<PlatformPasswordResetTokenValidationResult>> ValidateResetTokenAsync(
            string rawToken,
            CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult> MarkTokenUsedAsync(string rawToken, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<int>> RevokeActivePendingTokensAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<InitiatePlatformPasswordResetResponse>> InitiateAdminPasswordResetAsync(
            Guid targetUserId,
            Guid actorPlatformUserId,
            PlatformAuthClientContext? clientContext,
            CancellationToken cancellationToken)
            => Task.FromResult(_initiate ?? ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(
                new ApplicationError("not_configured", "not configured")));

        public Task<ApplicationResult<ValidatePlatformPasswordResetTokenResponse>> ValidatePublicTokenAsync(
            string rawToken,
            CancellationToken cancellationToken)
            => Task.FromResult(_validate ?? ApplicationResult<ValidatePlatformPasswordResetTokenResponse>.Success(
                new ValidatePlatformPasswordResetTokenResponse(false, "INVALID", null)));

        public Task<ApplicationResult<CompletePlatformPasswordResetResponse>> CompletePasswordResetAsync(
            CompletePlatformPasswordResetRequest request,
            PlatformAuthClientContext? clientContext,
            CancellationToken cancellationToken)
            => Task.FromResult(_complete ?? ApplicationResult<CompletePlatformPasswordResetResponse>.Success(
                new CompletePlatformPasswordResetResponse(true, "ok")));
    }
}
