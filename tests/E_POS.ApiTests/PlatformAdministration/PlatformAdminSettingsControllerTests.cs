using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAdminSettingsControllerTests
{
    private static readonly Guid PlatformUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSettings_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformSettingsService(
                ApplicationResult<PlatformSettingsResponse>.Success(CreateResponse())),
            PlatformUserId);

        var result = await controller.GetSettings(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformSettingsResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("SCS-TIX", payload.Data.PlatformDisplayName);
    }

    [Fact]
    public async Task GetSettings_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformSettingsService(
                ApplicationResult<PlatformSettingsResponse>.Failure(new ApplicationError(
                    "platform_settings.access_denied",
                    "Platform settings access denied."))),
            PlatformUserId);

        var result = await controller.GetSettings(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetSettings_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformSettingsService(
                ApplicationResult<PlatformSettingsResponse>.Success(CreateResponse())),
            platformUserId: null);

        var result = await controller.GetSettings(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSettings_WithValidationFailure_ReturnsBadRequest()
    {
        var controller = CreateController(
            new FakePlatformSettingsService(
                ApplicationResult<PlatformSettingsResponse>.Failure(new ApplicationError(
                    "platform_settings.validation_failed",
                    "One or more platform settings fields are invalid."))),
            PlatformUserId);

        var result = await controller.UpdateSettings(
            new UpdatePlatformSettingsRequest { PlatformDisplayName = "SCS-TIX" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_WithPermission_ReturnsUpdatedSettings()
    {
        var controller = CreateController(
            new FakePlatformSettingsService(
                ApplicationResult<PlatformSettingsResponse>.Success(CreateResponse() with
                {
                    PlatformDisplayName = "TM-EPOS"
                })),
            PlatformUserId);

        var result = await controller.UpdateSettings(
            new UpdatePlatformSettingsRequest { PlatformDisplayName = "TM-EPOS" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformSettingsResponse>>(ok.Value);
        Assert.Equal("TM-EPOS", payload.Data.PlatformDisplayName);
    }

    [Fact]
    public void SettingsEndpoint_RequiresPlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminSettingsController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminSettingsController CreateController(
        FakePlatformSettingsService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminSettingsController(service)
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

    private static PlatformSettingsResponse CreateResponse()
    {
        return new PlatformSettingsResponse
        {
            PlatformDisplayName = "SCS-TIX",
            DefaultCountryCode = "LK",
            DefaultCurrencyCode = "LKR",
            DefaultTimezone = "Asia/Colombo",
            DefaultLocale = "en-LK",
            UpdatedAt = Now,
            UpdatedByPlatformUserId = PlatformUserId
        };
    }

    private sealed class FakePlatformSettingsService : IPlatformSettingsService
    {
        private readonly ApplicationResult<PlatformSettingsResponse> _getResult;
        private readonly ApplicationResult<PlatformSettingsResponse>? _updateResult;

        public FakePlatformSettingsService(ApplicationResult<PlatformSettingsResponse> result)
        {
            _getResult = result;
            _updateResult = result;
        }

        public Task<ApplicationResult<PlatformSettingsResponse>> GetSettingsAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_getResult);
        }

        public Task<ApplicationResult<PlatformSettingsResponse>> UpdateSettingsAsync(
            UpdatePlatformSettingsRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_updateResult!);
        }
    }
}
