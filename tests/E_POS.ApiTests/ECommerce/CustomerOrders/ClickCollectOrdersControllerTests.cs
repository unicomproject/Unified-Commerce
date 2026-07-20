using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.ECommerce;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CustomerOrders;

public sealed class ClickCollectOrdersControllerTests
{
    [Fact]
    public async Task UpdateStatus_WithTenantClaims_ForwardsContextOrderAndRequest()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var service = new FakeClickCollectOrderStatusService();
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "fulfillment.orders.manage");
        var request = new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" };

        var result = await controller.UpdateStatus(orderId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Contains("fulfillment.orders.manage", service.Context!.Permissions);
        Assert.Equal(orderId, service.OrderId);
        Assert.Same(request, service.Request);
    }

    [Fact]
    public async Task UpdateStatus_WithoutTenantClaims_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new FakeClickCollectOrderStatusService();
        var controller = CreateController(service);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.Context);
    }

    [Fact]
    public async Task UpdateStatus_PermissionDenied_ReturnsForbidden()
    {
        var service = new FakeClickCollectOrderStatusService
        {
            Result = ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Failure(
                new ApplicationError("click_collect_orders.permission_denied", "Permission denied."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.dashboard.view");

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsConflict()
    {
        var service = new FakeClickCollectOrderStatusService
        {
            Result = ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Failure(
                new ApplicationError("click_collect_orders.invalid_transition", "Invalid transition."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "fulfillment.orders.manage");

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new ClickCollectOrderStatusUpdateRequest { Status = "READY_FOR_COLLECTION" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_NotFound_ReturnsNotFound()
    {
        var service = new FakeClickCollectOrderStatusService
        {
            Result = ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Failure(
                new ApplicationError("click_collect_orders.not_found", "Order was not found."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "fulfillment.orders.manage");

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" },
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicyAndExpectedRoute()
    {
        var authorize = Assert.Single(
            typeof(ClickCollectOrdersController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);

        var route = Assert.Single(
            typeof(ClickCollectOrdersController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/tenant/ecommerce/click-collect/orders", route.Template);
        Assert.Equal(
            "{orderId:guid}/status",
            Assert.Single(typeof(ClickCollectOrdersController)
                .GetMethod(nameof(ClickCollectOrdersController.UpdateStatus))!
                .GetCustomAttributes<HttpPatchAttribute>()).Template);
    }

    private static ClickCollectOrdersController CreateController(FakeClickCollectOrderStatusService service) =>
        new(service, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static void SetTenantClaims(
        ClickCollectOrdersController controller,
        Guid tenantId,
        Guid userId,
        string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("permissions", permission),
            new Claim("identity_type", "tenant_user")
        ], "Test"));
    }

    private sealed class FakeClickCollectOrderStatusService : IClickCollectOrderStatusService
    {
        public ApplicationResult<ClickCollectOrderStatusUpdateResponse> Result { get; init; } =
            ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Success(
                new ClickCollectOrderStatusUpdateResponse
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "SO-WEB-1",
                    Status = "ACCEPTED",
                    StatusLabel = "Accepted",
                    FulfillmentStatus = "ACCEPTED",
                    UpdatedAt = DateTimeOffset.UtcNow,
                    CollectionQrAvailable = true
                });
        public TenantRequestContext? Context { get; private set; }
        public Guid? OrderId { get; private set; }
        public ClickCollectOrderStatusUpdateRequest? Request { get; private set; }

        public Task<ApplicationResult<ClickCollectOrderStatusUpdateResponse>> UpdateStatusAsync(
            TenantRequestContext context,
            Guid orderId,
            ClickCollectOrderStatusUpdateRequest request,
            CancellationToken cancellationToken)
        {
            Context = context;
            OrderId = orderId;
            Request = request;
            return Task.FromResult(Result);
        }
    }
}
