using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.SubscriptionBilling.Contracts;
using E_POS.Application.Modules.SubscriptionBilling.Dtos;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.SubscriptionBilling;

public sealed class PlatformSubscriptionPlansControllerTests
{
    [Fact]
    public async Task GetPlans_WithPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformSubscriptionPlanService(
                ApplicationResult<SubscriptionPlanListResponse>.Success(
                    new SubscriptionPlanListResponse([], 1, 10, 0, 0, true, true, true, true, true))),
            Guid.NewGuid());

        var result = await controller.GetPlans(new SubscriptionPlanListQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<SubscriptionPlanListResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.True(payload.Data.CanCreate);
    }

    [Fact]
    public async Task GetPlans_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformSubscriptionPlanService(
                ApplicationResult<SubscriptionPlanListResponse>.Failure(new ApplicationError(
                    "platform_subscription_plans.access_denied",
                    "Platform subscription plan access denied."))),
            Guid.NewGuid());

        var result = await controller.GetPlans(new SubscriptionPlanListQuery(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_WithDuplicateCode_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformSubscriptionPlanService(
                ApplicationResult<SubscriptionPlanMutationResponse>.Failure(new ApplicationError(
                    "platform_subscription_plans.conflict",
                    "A subscription plan with this plan code already exists."))),
            Guid.NewGuid());

        var result = await controller.CreatePlan(
            new CreateSubscriptionPlanRequest { PlanCode = "starter", Name = "Starter", BillingCycle = "monthly" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task PublishPlan_WithInvalidTransition_ReturnsConflict()
    {
        var controller = CreateController(
            new FakePlatformSubscriptionPlanService(
                publishResult: ApplicationResult<SubscriptionPlanMutationResponse>.Failure(new ApplicationError(
                    "platform_subscription_plans.invalid_transition",
                    "Only draft subscription plans can be published."))),
            Guid.NewGuid());

        var result = await controller.PublishPlan(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformSubscriptionPlanService(
                ApplicationResult<SubscriptionPlanCatalogResponse>.Success(new SubscriptionPlanCatalogResponse([]))),
            platformUserId: null);

        var result = await controller.GetCatalog(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void SubscriptionPlanEndpoints_RequirePlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformSubscriptionPlansController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformSubscriptionPlansController CreateController(
        FakePlatformSubscriptionPlanService service,
        Guid? platformUserId)
    {
        var controller = new PlatformSubscriptionPlansController(service)
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

    private sealed class FakePlatformSubscriptionPlanService : IPlatformSubscriptionPlanService
    {
        private readonly ApplicationResult<SubscriptionPlanListResponse>? _listResult;
        private readonly ApplicationResult<SubscriptionPlanCatalogResponse>? _catalogResult;
        private readonly ApplicationResult<SubscriptionPlanMutationResponse>? _mutationResult;
        private readonly ApplicationResult<SubscriptionPlanMutationResponse>? _publishResult;

        public FakePlatformSubscriptionPlanService(ApplicationResult<SubscriptionPlanListResponse> listResult)
        {
            _listResult = listResult;
            _catalogResult = ApplicationResult<SubscriptionPlanCatalogResponse>.Success(new SubscriptionPlanCatalogResponse([]));
            _mutationResult = ApplicationResult<SubscriptionPlanMutationResponse>.Failure(new ApplicationError("platform_subscription_plans.not_found", "missing"));
            _publishResult = _mutationResult;
        }

        public FakePlatformSubscriptionPlanService(ApplicationResult<SubscriptionPlanCatalogResponse> catalogResult)
        {
            _catalogResult = catalogResult;
            _listResult = ApplicationResult<SubscriptionPlanListResponse>.Success(new SubscriptionPlanListResponse([], 1, 10, 0, 0, false, false, false, false, false));
            _mutationResult = ApplicationResult<SubscriptionPlanMutationResponse>.Failure(new ApplicationError("platform_subscription_plans.not_found", "missing"));
            _publishResult = _mutationResult;
        }

        public FakePlatformSubscriptionPlanService(ApplicationResult<SubscriptionPlanMutationResponse> mutationResult)
        {
            _mutationResult = mutationResult;
            _publishResult = mutationResult;
            _listResult = ApplicationResult<SubscriptionPlanListResponse>.Success(new SubscriptionPlanListResponse([], 1, 10, 0, 0, false, false, false, false, false));
            _catalogResult = ApplicationResult<SubscriptionPlanCatalogResponse>.Success(new SubscriptionPlanCatalogResponse([]));
        }

        public FakePlatformSubscriptionPlanService(
            ApplicationResult<SubscriptionPlanMutationResponse>? publishResult = null,
            ApplicationResult<SubscriptionPlanListResponse>? listResult = null)
        {
            _publishResult = publishResult;
            _listResult = listResult;
            _catalogResult = ApplicationResult<SubscriptionPlanCatalogResponse>.Success(new SubscriptionPlanCatalogResponse([]));
            _mutationResult = publishResult ?? ApplicationResult<SubscriptionPlanMutationResponse>.Failure(new ApplicationError("platform_subscription_plans.not_found", "missing"));
        }

        public Task<ApplicationResult<SubscriptionPlanListResponse>> GetPlansAsync(
            SubscriptionPlanListQuery query,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_listResult!);
        }

        public Task<ApplicationResult<SubscriptionPlanCatalogResponse>> GetCatalogAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_catalogResult!);
        }

        public Task<ApplicationResult<SubscriptionPlanMutationResponse>> CreateDraftAsync(
            CreateSubscriptionPlanRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_mutationResult!);
        }

        public Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdatePricingAsync(
            Guid planId,
            UpdateSubscriptionPlanPricingRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_mutationResult!);
        }

        public Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdateLimitsAsync(
            Guid planId,
            UpdateSubscriptionPlanLimitsRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_mutationResult!);
        }

        public Task<ApplicationResult<SubscriptionPlanMutationResponse>> UpdateFeaturesAsync(
            Guid planId,
            UpdateSubscriptionPlanFeaturesRequest request,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_mutationResult!);
        }

        public Task<ApplicationResult<SubscriptionPlanMutationResponse>> PublishAsync(
            Guid planId,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_publishResult ?? _mutationResult!);
        }
    }
}
