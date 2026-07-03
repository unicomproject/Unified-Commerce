using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Mappers;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAdminPermissionCatalogControllerTests
{
    [Fact]
    public async Task GetCatalog_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var catalog = CreateCatalog();
        var controller = CreateController(
            new FakePlatformPermissionCatalogService(
                ApplicationResult<PlatformPermissionCatalogResponse>.Success(catalog),
                ApplicationResult<PlatformPermissionFlatResponse>.Success(CreateFlatCatalog())),
            Guid.NewGuid());

        var result = await controller.GetCatalog(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformPermissionCatalogResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(13, payload.Data.Modules.Count);
    }

    [Fact]
    public async Task GetFlatCatalog_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var flatCatalog = CreateFlatCatalog();
        var controller = CreateController(
            new FakePlatformPermissionCatalogService(
                ApplicationResult<PlatformPermissionCatalogResponse>.Success(CreateCatalog()),
                ApplicationResult<PlatformPermissionFlatResponse>.Success(flatCatalog)),
            Guid.NewGuid());

        var result = await controller.GetFlatCatalog(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformPermissionFlatResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(36, payload.Data.TotalCount);
    }

    [Fact]
    public async Task GetCatalog_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformPermissionCatalogService(
                ApplicationResult<PlatformPermissionCatalogResponse>.Failure(new ApplicationError(
                    "platform_permission_catalog.access_denied",
                    "Platform permission catalog access denied.")),
                ApplicationResult<PlatformPermissionFlatResponse>.Success(CreateFlatCatalog())),
            Guid.NewGuid());

        var result = await controller.GetCatalog(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetFlatCatalog_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformPermissionCatalogService(
                ApplicationResult<PlatformPermissionCatalogResponse>.Success(CreateCatalog()),
                ApplicationResult<PlatformPermissionFlatResponse>.Failure(new ApplicationError(
                    "platform_permission_catalog.access_denied",
                    "Platform permission catalog access denied."))),
            Guid.NewGuid());

        var result = await controller.GetFlatCatalog(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformPermissionCatalogService(
                ApplicationResult<PlatformPermissionCatalogResponse>.Success(CreateCatalog()),
                ApplicationResult<PlatformPermissionFlatResponse>.Success(CreateFlatCatalog())),
            platformUserId: null);

        var result = await controller.GetCatalog(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetFlatCatalog_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformPermissionCatalogService(
                ApplicationResult<PlatformPermissionCatalogResponse>.Success(CreateCatalog()),
                ApplicationResult<PlatformPermissionFlatResponse>.Success(CreateFlatCatalog())),
            platformUserId: null);

        var result = await controller.GetFlatCatalog(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void PermissionCatalogEndpoints_RequirePlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminPermissionCatalogController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminPermissionCatalogController CreateController(
        FakePlatformPermissionCatalogService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminPermissionCatalogController(service)
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

    private static PlatformPermissionCatalogResponse CreateCatalog()
    {
        var permissions = PlatformAdminPermissionsSeedData.Definitions
            .Select(definition => new PlatformPermissionCatalogItem(
                definition.Id,
                definition.PermissionCode,
                definition.Name,
                definition.Description,
                "ACTIVE"))
            .ToList();

        return PlatformPermissionCatalogMapper.BuildCatalog(permissions);
    }

    private static PlatformPermissionFlatResponse CreateFlatCatalog()
    {
        var permissions = PlatformAdminPermissionsSeedData.Definitions
            .Select(definition => new PlatformPermissionDto(
                definition.Id,
                definition.PermissionCode,
                definition.Name,
                definition.Description,
                "ACTIVE",
                IsSystem: false,
                IsBootstrap: false))
            .ToList();

        return new PlatformPermissionFlatResponse(permissions, permissions.Count);
    }

    private sealed class FakePlatformPermissionCatalogService : IPlatformPermissionCatalogService
    {
        private readonly ApplicationResult<PlatformPermissionCatalogResponse> _catalogResult;
        private readonly ApplicationResult<PlatformPermissionFlatResponse> _flatResult;

        public FakePlatformPermissionCatalogService(
            ApplicationResult<PlatformPermissionCatalogResponse> catalogResult,
            ApplicationResult<PlatformPermissionFlatResponse> flatResult)
        {
            _catalogResult = catalogResult;
            _flatResult = flatResult;
        }

        public Task<ApplicationResult<PlatformPermissionCatalogResponse>> GetCatalogAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_catalogResult);
        }

        public Task<ApplicationResult<PlatformPermissionFlatResponse>> GetFlatCatalogAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_flatResult);
        }
    }
}
