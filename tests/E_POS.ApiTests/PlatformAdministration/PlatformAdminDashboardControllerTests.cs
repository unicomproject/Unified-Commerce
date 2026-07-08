using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAdminDashboardControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetDashboard_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var dashboard = CreateDashboard();
        var controller = CreateController(
            new FakePlatformDashboardService(ApplicationResult<PlatformDashboardResponse>.Success(dashboard)),
            Guid.NewGuid());

        var result = await controller.GetDashboard(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformDashboardResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(2, payload.Data.TotalTenants);
    }

    [Fact]
    public async Task GetDashboard_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformDashboardService(ApplicationResult<PlatformDashboardResponse>.Failure(new ApplicationError(
                "platform_dashboard.access_denied",
                "Platform dashboard access denied."))),
            Guid.NewGuid());

        var result = await controller.GetDashboard(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformDashboardService(ApplicationResult<PlatformDashboardResponse>.Success(CreateDashboard())),
            platformUserId: null);

        var result = await controller.GetDashboard(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void GetDashboardEndpoint_RequiresPlatformOnlyPolicy()
    {
        var method = typeof(PlatformAdminDashboardController).GetMethod(nameof(PlatformAdminDashboardController.GetDashboard));
        var controllerAuthorize = typeof(PlatformAdminDashboardController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(method);
        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminDashboardController CreateController(
        FakePlatformDashboardService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminDashboardController(service)
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

    private static PlatformDashboardResponse CreateDashboard()
    {
        return new PlatformDashboardResponse(
            TotalTenants: 2,
            ActiveTenants: 1,
            SuspendedTenants: 1,
            TrialTenants: 0,
            TotalSubscriptions: 1,
            ActiveSubscriptions: 1,
            PendingBillingCount: 0,
            TotalOutlets: 0,
            TotalTills: 0,
            TotalUsers: 0,
            RecentTenants: [],
            AttentionItems: [],
            GeneratedAt: Now);
    }

    private sealed class FakePlatformDashboardService : IPlatformDashboardService
    {
        private readonly ApplicationResult<PlatformDashboardResponse> _result;

        public FakePlatformDashboardService(ApplicationResult<PlatformDashboardResponse> result)
        {
            _result = result;
        }

        public Task<ApplicationResult<PlatformDashboardResponse>> GetDashboardAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}

