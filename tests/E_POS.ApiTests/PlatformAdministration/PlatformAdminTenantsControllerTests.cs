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

public sealed class PlatformAdminTenantsControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTenants_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformTenantService(ApplicationResult<PlatformTenantListResponse>.Success(CreateListResponse())),
            Guid.NewGuid());

        var result = await controller.GetTenants(new PlatformTenantListQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformTenantListResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Single(payload.Data.Items);
    }

    [Fact]
    public async Task GetTenants_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformTenantService(ApplicationResult<PlatformTenantListResponse>.Failure(new ApplicationError(
                "platform_tenants.access_denied",
                "Platform tenant access denied."))),
            Guid.NewGuid());

        var result = await controller.GetTenants(new PlatformTenantListQuery(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetTenants_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformTenantService(ApplicationResult<PlatformTenantListResponse>.Success(CreateListResponse())),
            platformUserId: null);

        var result = await controller.GetTenants(new PlatformTenantListQuery(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetSummary_WithPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                listResult: ApplicationResult<PlatformTenantListResponse>.Success(CreateListResponse()),
                summaryResult: ApplicationResult<PlatformTenantSummaryResponse>.Success(new PlatformTenantSummaryResponse(
                    1, 1, 0, 0, 0, 0, 0, 0))),
            Guid.NewGuid());

        var result = await controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LegacyApiResponse<PlatformTenantSummaryResponse>>(ok.Value);
    }

    [Fact]
    public async Task GetSummary_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                summaryResult: ApplicationResult<PlatformTenantSummaryResponse>.Failure(new ApplicationError(
                    "platform_tenants.access_denied",
                    "Platform tenant access denied."))),
            Guid.NewGuid());

        var result = await controller.GetSummary(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetFilterOptions_WithPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                filterOptionsResult: ApplicationResult<PlatformTenantFilterOptionsResponse>.Success(
                    new PlatformTenantFilterOptionsResponse([], [], [], []))),
            Guid.NewGuid());

        var result = await controller.GetFilterOptions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LegacyApiResponse<PlatformTenantFilterOptionsResponse>>(ok.Value);
    }

    [Fact]
    public void TenantEndpoints_RequirePlatformOnlyPolicy()
    {
        var authorize = typeof(PlatformAdminTenantsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("PlatformOnly", authorize!.Policy);
    }

    [Fact]
    public async Task GetTenantById_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var tenantId = Guid.NewGuid();
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Success(CreateDetail(tenantId))),
            tenantId);

        var result = await controller.GetTenantById(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformTenantDetailResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(tenantId, payload.Data.Id);
    }

    [Fact]
    public async Task GetTenantById_WithoutPermission_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                    "platform_tenants.access_denied",
                    "Platform tenant access denied."))),
            tenantId);

        var result = await controller.GetTenantById(tenantId, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetTenantById_WhenTenantMissing_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                    "platform_tenants.not_found",
                    "Platform tenant not found."))),
            tenantId);

        var result = await controller.GetTenantById(tenantId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetTenantById_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Success(CreateDetail(Guid.NewGuid()))),
            platformUserId: null);

        var result = await controller.GetTenantById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetEntitlementOptions_WithPermission_ReturnsLegacyApiResponse()
    {
        var tenantId = Guid.NewGuid();
        var options = CreateEntitlementOptions(tenantId);
        var controller = CreateController(
            new FakePlatformTenantService(
                entitlementOptionsResult: ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Success(options)),
            tenantId);

        var result = await controller.GetEntitlementOptions(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformTenantEntitlementOptionsResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(tenantId, payload.Data.TenantId);
        Assert.Single(payload.Data.EnabledFeatureIds);
        Assert.Single(payload.Data.Plans);
        Assert.NotEmpty(payload.Data.CatalogModules);
    }

    [Fact]
    public async Task GetEntitlementOptions_WithoutPermission_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var controller = CreateController(
            new FakePlatformTenantService(
                entitlementOptionsResult: ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(new ApplicationError(
                    "platform_tenants.access_denied",
                    "Platform tenant access denied."))),
            tenantId);

        var result = await controller.GetEntitlementOptions(tenantId, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetEntitlementOptions_WhenTenantMissing_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var controller = CreateController(
            new FakePlatformTenantService(
                entitlementOptionsResult: ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(new ApplicationError(
                    "platform_tenants.not_found",
                    "Platform tenant not found."))),
            tenantId);

        var result = await controller.GetEntitlementOptions(tenantId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateTenant_WithDuplicateCode_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                    "platform_tenants.conflict",
                    "A tenant with this code already exists."))),
            Guid.NewGuid());

        var result = await controller.CreateTenant(
            new CreatePlatformTenantRequest { Code = "TEN-DUP", Name = "Duplicate" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateTenant_WithInvalidPlan_ReturnsBadRequest()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                    "platform_tenants.validation_failed",
                    "Subscription plan was not found or is not active."))),
            Guid.NewGuid());

        var result = await controller.CreateTenant(
            new CreatePlatformTenantRequest { Code = "TEN-001", Name = "Tenant" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ActivateTenant_WithInvalidTransition_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                    "platform_tenants.invalid_transition",
                    "Tenant cannot be activated from its current status."))),
            Guid.NewGuid());

        var result = await controller.ActivateTenant(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateTenant_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformTenantService(
                detailResult: ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                    "platform_tenants.access_denied",
                    "Platform tenant access denied."))),
            Guid.NewGuid());

        var result = await controller.UpdateTenant(
            Guid.NewGuid(),
            new UpdatePlatformTenantRequest { Name = "Updated" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void TenantMutationEndpoints_RequirePlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminTenantsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminTenantsController CreateController(
        FakePlatformTenantService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminTenantsController(service)
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

    private static PlatformTenantListResponse CreateListResponse()
    {
        return new PlatformTenantListResponse(
            [
                new PlatformTenantListItemDto(
                    Guid.NewGuid(),
                    "TEN-001",
                    "Tenant One",
                    "active",
                    "paid",
                    "unified_epos",
                    "LKR",
                    "Asia/Colombo",
                    "en-LK",
                    "Retail",
                    null,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    Now,
                    Now)
            ],
            1,
            10,
            1,
            1);
    }

    private static PlatformTenantDetailResponse CreateDetail(Guid tenantId)
    {
        return new PlatformTenantDetailResponse(
            tenantId,
            "TEN-001",
            "Tenant One",
            "active",
            "paid",
            "unified_epos",
            "LKR",
            "Asia/Colombo",
            "en-LK",
            "Retail",
            null,
            null,
            null,
            0,
            0,
            0,
            false,
            false,
            false,
            [],
            [],
            Now,
            Now,
            Now,
            true,
            false,
            true,
            true);
    }

    private static PlatformTenantEntitlementOptionsResponse CreateEntitlementOptions(Guid tenantId)
    {
        var featureId = Guid.Parse("88888888-8888-4888-8888-888888888801");
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777711");

        return new PlatformTenantEntitlementOptionsResponse(
            tenantId,
            planId,
            "STARTER",
            "Starter Plan",
            [featureId],
            ["online_store"],
            [
                new PlatformTenantEntitlementPlanOptionDto(
                    planId,
                    "STARTER",
                    "Starter Plan",
                    "active",
                    [featureId],
                    ["online_store"])
            ],
            [
                new PlatformTenantEntitlementCatalogModuleDto(
                    Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                    "commerce",
                    "Commerce",
                    [
                        new PlatformTenantEntitlementCatalogFeatureDto(
                            featureId,
                            "online_store",
                            "Online Store",
                            "Online store entitlement")
                    ])
            ]);
    }

    private sealed class FakePlatformTenantService : IPlatformTenantService
    {
        private readonly ApplicationResult<PlatformTenantListResponse> _listResult;
        private readonly ApplicationResult<PlatformTenantSummaryResponse> _summaryResult;
        private readonly ApplicationResult<PlatformTenantFilterOptionsResponse> _filterOptionsResult;
        private readonly ApplicationResult<PlatformTenantDetailResponse> _detailResult;
        private readonly ApplicationResult<PlatformTenantEntitlementOptionsResponse> _entitlementOptionsResult;

        public FakePlatformTenantService(ApplicationResult<PlatformTenantListResponse> listResult)
            : this(listResult, null, null, null, null)
        {
        }

        public FakePlatformTenantService(
            ApplicationResult<PlatformTenantListResponse>? listResult = null,
            ApplicationResult<PlatformTenantSummaryResponse>? summaryResult = null,
            ApplicationResult<PlatformTenantFilterOptionsResponse>? filterOptionsResult = null,
            ApplicationResult<PlatformTenantDetailResponse>? detailResult = null,
            ApplicationResult<PlatformTenantEntitlementOptionsResponse>? entitlementOptionsResult = null)
        {
            _listResult = listResult ?? ApplicationResult<PlatformTenantListResponse>.Success(CreateListResponse());
            _summaryResult = summaryResult ?? ApplicationResult<PlatformTenantSummaryResponse>.Success(
                new PlatformTenantSummaryResponse(0, 0, 0, 0, 0, 0, 0, 0));
            _filterOptionsResult = filterOptionsResult ?? ApplicationResult<PlatformTenantFilterOptionsResponse>.Success(
                new PlatformTenantFilterOptionsResponse([], [], [], []));
            _detailResult = detailResult ?? ApplicationResult<PlatformTenantDetailResponse>.Failure(new ApplicationError(
                "platform_tenants.not_found",
                "Platform tenant not found."));
            _entitlementOptionsResult = entitlementOptionsResult ?? ApplicationResult<PlatformTenantEntitlementOptionsResponse>.Failure(new ApplicationError(
                "platform_tenants.not_found",
                "Platform tenant not found."));
        }

        public Task<ApplicationResult<PlatformTenantListResponse>> GetTenantsAsync(
            PlatformTenantListQuery query,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_listResult);

        public Task<ApplicationResult<PlatformTenantSummaryResponse>> GetSummaryAsync(
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_summaryResult);

        public Task<ApplicationResult<PlatformTenantFilterOptionsResponse>> GetFilterOptionsAsync(
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_filterOptionsResult);

        public Task<ApplicationResult<PlatformTenantDetailResponse>> GetTenantDetailAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApplicationResult<PlatformTenantDetailResponse>> CreateTenantAsync(
            CreatePlatformTenantRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApplicationResult<PlatformTenantDetailResponse>> UpdateTenantAsync(
            Guid tenantId,
            UpdatePlatformTenantRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApplicationResult<PlatformTenantDetailResponse>> ActivateTenantAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApplicationResult<PlatformTenantDetailResponse>> SuspendTenantAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApplicationResult<PlatformTenantDetailResponse>> UpdateEntitlementsAsync(
            Guid tenantId,
            UpdatePlatformTenantEntitlementsRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApplicationResult<PlatformTenantEntitlementOptionsResponse>> GetEntitlementOptionsAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_entitlementOptionsResult);

        public Task<ApplicationResult<PlatformTenantCreateOptionsResponse>> GetCreateOptionsAsync(
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantCreateOptionsResponse>.Success(
                new PlatformTenantCreateOptionsResponse([], [], [], [], [], [], [], [], [], [], [], [], [])));
    }
}

