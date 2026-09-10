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
    public async Task PickLine_ForwardsExpectedVersionAndCanonicalArguments()
    {
        var picking = new FakePosOnlineOrderPickingService();
        var controller = CreateController(new FakeClickCollectOrderStatusService(), pickingService: picking);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(),
            "commerce.online_order.orders.access commerce.online_order.picking.pick commerce.online_order.picking.scan");
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var request = new PosOnlineOrderPickLineRequest
        {
            Quantity = 1,
            Barcode = "SKU-1",
            InputMethod = "SCAN",
            ExpectedVersion = 4
        };

        var result = await controller.PickLine(orderId, lineId, outletId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(orderId, picking.OrderId);
        Assert.Equal(lineId, picking.LineId);
        Assert.Equal(outletId, picking.OutletId);
        Assert.Equal(4, picking.PickRequest?.ExpectedVersion);
    }

    [Fact]
    public async Task PickLine_ConcurrencyConflict_Returns409()
    {
        var picking = new FakePosOnlineOrderPickingService
        {
            CommandResult = ApplicationResult<PosOnlineOrderPickingCommandResponse>.Failure(
                new ApplicationError("online_orders.concurrency_conflict", "Conflict."))
        };
        var controller = CreateController(new FakeClickCollectOrderStatusService(), pickingService: picking);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(),
            "commerce.online_order.orders.access commerce.online_order.picking.pick commerce.online_order.picking.manual_entry");

        var result = await controller.PickLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new PosOnlineOrderPickLineRequest { Quantity = 1, InputMethod = "MANUAL", ExpectedVersion = 2 },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AddPickingNote_ForwardsCanonicalArgumentsAndExpectedVersion()
    {
        var picking = new FakePosOnlineOrderPickingService();
        var controller = CreateController(new FakeClickCollectOrderStatusService(), pickingService: picking);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(),
            "commerce.online_order.orders.access commerce.online_order.picking.note");
        var orderId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var request = new PosOnlineOrderPickingNoteRequest { Note = "Shelf checked", ExpectedVersion = 9 };

        var result = await controller.AddPickingNote(
            orderId, outletId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(orderId, picking.OrderId);
        Assert.Equal(outletId, picking.OutletId);
        Assert.Equal("Shelf checked", picking.NoteRequest?.Note);
        Assert.Equal(9, picking.NoteRequest?.ExpectedVersion);
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
        Assert.Equal("{orderId:guid}/picking", Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.GetPicking))!
            .GetCustomAttributes<HttpGetAttribute>()).Template);
        Assert.Equal("{orderId:guid}/picking/lines/{lineId:guid}/pick", Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.PickLine))!
            .GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Equal("{orderId:guid}/picking/lines/{lineId:guid}/issues", Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.ReportPickingIssue))!
            .GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Equal("{orderId:guid}/picking/notes", Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.AddPickingNote))!
            .GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Equal("{orderId:guid}/pack", Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.Pack))!
            .GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Equal("{orderId:guid}/ready", Assert.Single(typeof(ClickCollectOrdersController)
            .GetMethod(nameof(ClickCollectOrdersController.MarkReady))!
            .GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    private static ClickCollectOrdersController CreateController(
        FakeClickCollectOrderStatusService service,
        FakePosOnlineOrderDetailService? detailService = null,
        FakePosOnlineOrderStartFulfillmentService? startService = null,
        FakePosOnlineOrderPickingService? pickingService = null,
        FakePosOnlineOrderPackingService? packingService = null,
        FakeReadyService? readyService = null) =>
        new(service, detailService ?? new FakePosOnlineOrderDetailService(),
            startService ?? new FakePosOnlineOrderStartFulfillmentService(),
            pickingService ?? new FakePosOnlineOrderPickingService(),
            packingService ?? new FakePosOnlineOrderPackingService(),
            new TenantRequestContextFactory(), readyService ?? new FakeReadyService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Theory]
    [InlineData("online_orders.permission_denied", 403)]
    [InlineData("online_orders.outlet_access_denied", 403)]
    [InlineData("online_orders.not_found", 404)]
    [InlineData("online_orders.invalid_state", 409)]
    [InlineData("online_orders.concurrency_conflict", 409)]
    [InlineData("online_orders.notification_recipient_unavailable", 400)]
    [InlineData("online_orders.notification_failed", 503)]
    public async Task NotifyReady_UsesCanonicalErrorEnvelope(string code, int status)
    {
        var controller = CreateController(new FakeClickCollectOrderStatusService(),
            readyService: new FakeReadyService(code));
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "commerce.online_order.collection.notify_customer");
        var result = Assert.IsAssignableFrom<ObjectResult>(
            await controller.NotifyReady(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(status, result.StatusCode);
        Assert.Contains(code, System.Text.Json.JsonSerializer.Serialize(result.Value));
        Assert.Contains("traceId", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public async Task NotifyReady_WithoutContext_IsUnauthorized()
    {
        var controller = CreateController(new FakeClickCollectOrderStatusService());
        Assert.IsType<UnauthorizedObjectResult>(await controller.NotifyReady(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task NotifyReady_Success_ReturnsPersistedResult()
    {
        var controller = CreateController(new FakeClickCollectOrderStatusService());
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "commerce.online_order.collection.notify_customer");
        Assert.IsType<OkObjectResult>(await controller.NotifyReady(Guid.NewGuid(), Guid.NewGuid()));
    }

    private sealed class FakeReadyService(string? code = null) : IPosOnlineOrderReadyService
    {
        public Task<ApplicationResult<E_POS.Application.Modules.Shared.Notification.Dtos.NotificationCreateResult>> NotifyAsync(
            TenantRequestContext context, Guid outletId, Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(code is null
                ? ApplicationResult<E_POS.Application.Modules.Shared.Notification.Dtos.NotificationCreateResult>.Success(new())
                : ApplicationResult<E_POS.Application.Modules.Shared.Notification.Dtos.NotificationCreateResult>.Failure(new(code, "Safe error")));
    }

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

    private sealed class FakePosOnlineOrderPickingService : IPosOnlineOrderPickingService
    {
        public ApplicationResult<PosOnlineOrderPickingCommandResponse> CommandResult { get; init; } =
            ApplicationResult<PosOnlineOrderPickingCommandResponse>.Success(
                new PosOnlineOrderPickingCommandResponse());
        public Guid? OutletId { get; private set; }
        public Guid? OrderId { get; private set; }
        public Guid? LineId { get; private set; }
        public PosOnlineOrderPickLineRequest? PickRequest { get; private set; }
        public PosOnlineOrderPickingNoteRequest? NoteRequest { get; private set; }
        public ApplicationResult<PosOnlineOrderPickingNoteCommandResponse> NoteResult { get; init; } =
            ApplicationResult<PosOnlineOrderPickingNoteCommandResponse>.Success(
                new PosOnlineOrderPickingNoteCommandResponse());

        public Task<ApplicationResult<PosOnlineOrderPickingResponse>> GetAsync(
            TenantRequestContext context, Guid outletId, Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PosOnlineOrderPickingResponse>.Success(
                new PosOnlineOrderPickingResponse()));

        public Task<ApplicationResult<PosOnlineOrderPickingCommandResponse>> PickLineAsync(
            TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId,
            PosOnlineOrderPickLineRequest request, CancellationToken cancellationToken)
        {
            OutletId = outletId;
            OrderId = orderId;
            LineId = lineId;
            PickRequest = request;
            return Task.FromResult(CommandResult);
        }

        public Task<ApplicationResult<PosOnlineOrderPickingCommandResponse>> ReportIssueAsync(
            TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId,
            PosOnlineOrderPickingIssueRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(CommandResult);

        public Task<ApplicationResult<PosOnlineOrderPickingNoteCommandResponse>> AddNoteAsync(
            TenantRequestContext context, Guid outletId, Guid orderId,
            PosOnlineOrderPickingNoteRequest request, CancellationToken cancellationToken)
        {
            OutletId = outletId;
            OrderId = orderId;
            NoteRequest = request;
            return Task.FromResult(NoteResult);
        }
    }

    private sealed class FakePosOnlineOrderPackingService : IPosOnlineOrderPackingService
    {
        public ApplicationResult<PosOnlineOrderPackReadyCommandResponse> Result { get; init; } =
            ApplicationResult<PosOnlineOrderPackReadyCommandResponse>.Success(
                new PosOnlineOrderPackReadyCommandResponse());

        public Task<ApplicationResult<PosOnlineOrderPackReadyCommandResponse>> PackAsync(
            TenantRequestContext context, Guid outletId, Guid orderId,
            PosOnlineOrderPackRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Result);

        public Task<ApplicationResult<PosOnlineOrderPackReadyCommandResponse>> MarkReadyAsync(
            TenantRequestContext context, Guid outletId, Guid orderId,
            PosOnlineOrderReadyRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }
}
