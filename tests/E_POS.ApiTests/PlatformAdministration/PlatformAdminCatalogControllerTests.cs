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

public sealed class PlatformAdminCatalogControllerTests
{
    private static readonly Guid ModuleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FeatureId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task GetModules_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformModulesCatalogService(
                ApplicationResult<PlatformModulesCatalogResponse>.Success(CreateCatalog(includeFeatures: true))),
            Guid.NewGuid());

        var result = await controller.GetModules(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformModulesCatalogResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Single(payload.Data.Modules);
        Assert.Single(payload.Data.Modules[0].Features);
    }

    [Fact]
    public async Task GetModules_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformModulesCatalogService(
                ApplicationResult<PlatformModulesCatalogResponse>.Failure(new ApplicationError(
                    "platform_modules_catalog.access_denied",
                    "Platform modules catalog access denied."))),
            Guid.NewGuid());

        var result = await controller.GetModules(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetModules_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformModulesCatalogService(
                ApplicationResult<PlatformModulesCatalogResponse>.Success(CreateCatalog(includeFeatures: false))),
            platformUserId: null);

        var result = await controller.GetModules(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void ModulesCatalogEndpoint_RequiresPlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminCatalogController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminCatalogController CreateController(
        FakePlatformModulesCatalogService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminCatalogController(service)
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

    private static PlatformModulesCatalogResponse CreateCatalog(bool includeFeatures)
    {
        return new PlatformModulesCatalogResponse(
        [
            new PlatformModulesCatalogModuleDto(
                ModuleId,
                "core_pos",
                "Core POS",
                "Core module",
                10,
                "ACTIVE",
                includeFeatures
                    ?
                    [
                        new PlatformModulesCatalogFeatureDto(
                            FeatureId,
                            "pos.sales",
                            "POS Sales",
                            "Start sale",
                            1,
                            "ACTIVE")
                    ]
                    : [])
        ]);
    }

    private sealed class FakePlatformModulesCatalogService : IPlatformModulesCatalogService
    {
        private readonly ApplicationResult<PlatformModulesCatalogResponse> _result;

        public FakePlatformModulesCatalogService(ApplicationResult<PlatformModulesCatalogResponse> result)
        {
            _result = result;
        }

        public Task<ApplicationResult<PlatformModulesCatalogResponse>> GetModulesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}
