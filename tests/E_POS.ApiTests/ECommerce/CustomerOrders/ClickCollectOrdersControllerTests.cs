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
    public async Task List_WithTenantClaims_ForwardsCanonicalQueryAndReturnsQueueEnvelope()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var detailService = new FakePosOnlineOrderDetailService();
        var controller = CreateController(new FakeClickCollectOrderStatusService(), detailService);
        SetTenantClaims(controller, tenantId, userId,
            "commerce.online_order.orders.access commerce.online_order.orders.view");

        var result = await controller.List(
            outletId, "EC-1001", "NEW", "collectionTime", "asc", 1, 4,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, detailService.ListContext?.TenantId);
        Assert.Equal(userId, detailService.ListContext?.UserId);
        Assert.Equal(outletId, detailService.Query?.OutletId);
        Assert.Equal(4, detailService.Query?.PageSize);
    }

    [Fact]
    public async Task List_WithoutTenantClaims_ReturnsUnauthorizedWithoutCallingService()
    {
        var detailService = new FakePosOnlineOrderDetailService();
        var controller = CreateController(new FakeClickCollectOrderStatusService(), detailService);

        var result = await controller.List(
            Guid.NewGuid(), null, null, null, null, 1, 4, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(detailService.ListContext);
    }

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
    public async Task GetDetail_WithTenantClaims_ForwardsCanonicalRouteArguments()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var detailService = new FakePosOnlineOrderDetailService();
        var controller = CreateController(new FakeClickCollectOrderStatusService(), detailService);
        SetTenantClaims(controller, tenantId, userId, "commerce.online_order.orders.access commerce.online_order.orders.view");

        var result = await controller.GetDetail(orderId, outletId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, detailService.Context?.TenantId);
        Assert.Equal(userId, detailService.Context?.UserId);
        Assert.Equal(outletId, detailService.OutletId);
        Assert.Equal(orderId, detailService.OrderId);
    }

    [Fact]
    public async Task GetDetail_OutletDenied_ReturnsForbidden()
    {
        var detailService = new FakePosOnlineOrderDetailService
        {
            Result = ApplicationResult<PosOnlineOrderDetailResponse>.Failure(
                new ApplicationError("online_orders.outlet_access_denied", "Denied."))
        };
        var controller = CreateController(new FakeClickCollectOrderStatusService(), detailService);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "commerce.online_order.orders.access commerce.online_order.orders.view");

        var result = await controller.GetDetail(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task StartFulfillment_ForwardsExpectedVersionAndCanonicalArguments()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var startService = new FakePosOnlineOrderStartFulfillmentService();
        var controller = CreateController(new FakeClickCollectOrderStatusService(), startService: startService);
        SetTenantClaims(controller, tenantId, userId, "commerce.online_order.fulfilment.start");
        var request = new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = 5 };

        var result = await controller.StartFulfillment(orderId, outletId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, startService.Context?.TenantId);
        Assert.Equal(userId, startService.Context?.UserId);
        Assert.Equal(outletId, startService.OutletId);
        Assert.Equal(orderId, startService.OrderId);
        Assert.Same(request, startService.Request);
    }

    [Fact]
    public async Task StartFulfillment_ConcurrencyConflict_Returns409()
    {
        var startService = new FakePosOnlineOrderStartFulfillmentService
        {
            Result = ApplicationResult<PosOnlineOrderStartFulfillmentResponse>.Failure(
                new ApplicationError("online_orders.concurrency_conflict", "Conflict."))
        };
        var controller = CreateController(new FakeClickCollectOrderStatusService(), startService: startService);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "commerce.online_order.fulfilment.start");

        var result = await controller.StartFulfillment(
            Guid.NewGuid(), Guid.NewGuid(),
            new PosOnlineOrderStartFulfillmentRequest { ExpectedVersion = 5 }, CancellationToken.None);

        var objectResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
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
        Assert.Null(Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.List))!
            .GetCustomAttributes<HttpGetAttribute>()).Template);
        Assert.Equal(
            "{orderId:guid}",
            Assert.Single(typeof(ClickCollectOrdersController)
                .GetMethod(nameof(ClickCollectOrdersController.GetDetail))!
                .GetCustomAttributes<HttpGetAttribute>()).Template);
        Assert.Equal(
            "{orderId:guid}/fulfilment/start",
            Assert.Single(typeof(ClickCollectOrdersController)
                .GetMethod(nameof(ClickCollectOrdersController.StartFulfillment))!
                .GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    private static ClickCollectOrdersController CreateController(
        FakeClickCollectOrderStatusService service,
        FakePosOnlineOrderDetailService? detailService = null,
        FakePosOnlineOrderStartFulfillmentService? startService = null) =>
        new(service, detailService ?? new FakePosOnlineOrderDetailService(),
            startService ?? new FakePosOnlineOrderStartFulfillmentService(), new TenantRequestContextFactory())
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

    private sealed class FakePosOnlineOrderDetailService : IPosOnlineOrderDetailService
    {
        public TenantRequestContext? ListContext { get; private set; }
        public PosOnlineOrderListQuery? Query { get; private set; }

        public Task<ApplicationResult<PosOnlineOrderListResponse>> ListAsync(
            TenantRequestContext context,
            PosOnlineOrderListQuery query,
            CancellationToken cancellationToken)
        {
            ListContext = context;
            Query = query;
            return Task.FromResult(ApplicationResult<PosOnlineOrderListResponse>.Success(
                new PosOnlineOrderListResponse(
                    [], new PosOnlineOrderSummaryResponse(0, 0, 0, 0, 0, 0),
                    query.Page, query.PageSize, 0, 0, DateTimeOffset.UtcNow)));
        }

        public ApplicationResult<PosOnlineOrderDetailResponse> Result { get; init; } =
            ApplicationResult<PosOnlineOrderDetailResponse>.Success(new PosOnlineOrderDetailResponse
            {
                Id = Guid.NewGuid(),
                OrderNumber = "EC-1001",
                CustomerName = "Customer",
                OutletId = Guid.NewGuid(),
                OutletName = "Main Store",
                ServerTime = DateTimeOffset.UtcNow
            });
        public TenantRequestContext? Context { get; private set; }
        public Guid? OutletId { get; private set; }
        public Guid? OrderId { get; private set; }

        public Task<ApplicationResult<PosOnlineOrderDetailResponse>> GetAsync(
            TenantRequestContext context,
            Guid outletId,
            Guid orderId,
            CancellationToken cancellationToken)
        {
            Context = context;
            OutletId = outletId;
            OrderId = orderId;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakePosOnlineOrderStartFulfillmentService : IPosOnlineOrderStartFulfillmentService
    {
        public ApplicationResult<PosOnlineOrderStartFulfillmentResponse> Result { get; init; } =
            ApplicationResult<PosOnlineOrderStartFulfillmentResponse>.Success(
                new PosOnlineOrderStartFulfillmentResponse());
        public TenantRequestContext? Context { get; private set; }
        public Guid? OutletId { get; private set; }
        public Guid? OrderId { get; private set; }
        public PosOnlineOrderStartFulfillmentRequest? Request { get; private set; }

        public Task<ApplicationResult<PosOnlineOrderStartFulfillmentResponse>> StartAsync(
            TenantRequestContext context,
            Guid outletId,
            Guid orderId,
            PosOnlineOrderStartFulfillmentRequest request,
            CancellationToken cancellationToken)
        {
            Context = context;
            OutletId = outletId;
            OrderId = orderId;
            Request = request;
            return Task.FromResult(Result);
        }
    }
}
